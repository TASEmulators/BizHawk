using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SlimDX.DirectSound;
using SlimDX.Multimedia;

namespace BizHawk.Client.EmuHawk
{
	public class DirectSoundSoundOutput : ISoundOutput
	{
		private readonly Sound _sound;
		private bool _disposed;
		private DirectSound _device;
		private SecondarySoundBuffer _deviceBuffer;
		private int _actualWriteOffsetBytes = -1;
		private int _filledBufferSizeBytes;
		private long _lastWriteTime;
		private int _lastWriteCursor;
		private int _retryCounter;

		public DirectSoundSoundOutput(Sound sound, IntPtr mainWindowHandle, string soundDevice)
		{
			_sound = sound;
			_retryCounter = 5;

			var deviceInfo = DirectSound.GetDevices().FirstOrDefault(d => d.Description == soundDevice);
			_device = deviceInfo != null ? new DirectSound(deviceInfo.DriverGuid) : new DirectSound();
			_device.SetCooperativeLevel(mainWindowHandle, CooperativeLevel.Priority);
		}

		public void Dispose()
		{
			if (_disposed) return;

			_device.Dispose();
			_device = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			return DirectSound.GetDevices().Select(d => d.Description);
		}

		private int BufferSizeSamples { get; set; }

		private int BufferSizeBytes => BufferSizeSamples * Sound.BlockAlign;

		public int MaxSamplesDeficit { get; private set; }

		private bool IsPlaying => _deviceBuffer != null && (_deviceBuffer.Status & BufferStatus.BufferLost) == 0 && (_deviceBuffer.Status & BufferStatus.Playing) == BufferStatus.Playing;

		private void StartPlaying()
		{
			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;
			int attempts = _retryCounter;
			while (!IsPlaying && attempts > 0)
			{
				attempts--;
				try
				{
					if (_deviceBuffer == null)
					{
						var format = new WaveFormat
						{
							SamplesPerSecond = Sound.SampleRate,
							BitsPerSample = Sound.BytesPerSample * 8,
							Channels = Sound.ChannelCount,
							FormatTag = WaveFormatTag.Pcm,
							BlockAlignment = Sound.BlockAlign,
							AverageBytesPerSecond = Sound.SampleRate * Sound.BlockAlign
						};

						var desc = new SoundBufferDescription
						{
							Format = format,
							Flags =
									BufferFlags.GlobalFocus |
									BufferFlags.Software |
									BufferFlags.GetCurrentPosition2 |
									BufferFlags.ControlVolume,
							SizeInBytes = BufferSizeBytes
						};

						_deviceBuffer = new SecondarySoundBuffer(_device, desc);
					}

					_deviceBuffer.Play(0, PlayFlags.Looping);
				}
				catch (DirectSoundException)
				{
					if (_deviceBuffer != null)
					{
						_deviceBuffer.Restore();
					}
					if (attempts > 0)
					{
						System.Threading.Thread.Sleep(10);
					}
				}
			}

			if (IsPlaying)
			{
				_retryCounter = 5;
			}
			else if (_retryCounter > 1)
			{
				_retryCounter--;
			}
		}

		public void ApplyVolumeSettings(double volume)
		{
			if (IsPlaying)
			{
				try
				{
					// I'm not sure if this is "technically" correct but it works okay
					int range = (int)Volume.Maximum - (int)Volume.Minimum;
					_deviceBuffer.Volume = (int)(Math.Pow(volume, 0.1) * range) + (int)Volume.Minimum;
				}
				catch (DirectSoundException)
				{
				}
			}
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(GlobalWin.Config.SoundBufferSizeMs);

			// 35 to 65 milliseconds depending on how big the buffer is. This is a trade-off
			// between more frequent but less severe glitches (i.e. catching underruns before
			// they happen and filling the buffer with silence) or less frequent but more
			// severe glitches. At least on my Windows 8 machines, the distance between the
			// play and write cursors can be up to 30 milliseconds, so that would be the
			// absolute minimum we could use here.
			int minBufferFullnessMs = Math.Min(35 + ((GlobalWin.Config.SoundBufferSizeMs - 60) / 2), 65);
			MaxSamplesDeficit = BufferSizeSamples - Sound.MillisecondsToSamples(minBufferFullnessMs);

			StartPlaying();
		}

		public void StopSound()
		{
			if (IsPlaying)
			{
				try
				{
					_deviceBuffer.Stop();
				}
				catch (DirectSoundException)
				{
				}
			}
			_deviceBuffer.Dispose();
			_deviceBuffer = null;
			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			int samplesNeeded = 0;
			if (IsPlaying)
			{
				try
				{
					long currentWriteTime = Stopwatch.GetTimestamp();
					int playCursor = _deviceBuffer.CurrentPlayPosition;
					int writeCursor = _deviceBuffer.CurrentWritePosition;
					bool isInitializing = _actualWriteOffsetBytes == -1;
					bool detectedUnderrun = false;
					if (!isInitializing)
					{
						double elapsedSeconds = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency;
						double bufferSizeSeconds = (double)BufferSizeSamples / Sound.SampleRate;
						int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSizeBytes);
						cursorDelta += BufferSizeBytes * (int)Math.Round((elapsedSeconds - (cursorDelta / (double)(Sound.SampleRate * Sound.BlockAlign))) / bufferSizeSeconds);
						_filledBufferSizeBytes -= cursorDelta;
						detectedUnderrun = _filledBufferSizeBytes < 0;
					}
					if (isInitializing || detectedUnderrun)
					{
						_actualWriteOffsetBytes = writeCursor;
						_filledBufferSizeBytes = 0;
					}
					samplesNeeded = CircularDistance(_actualWriteOffsetBytes, playCursor, BufferSizeBytes) / Sound.BlockAlign;
					if (isInitializing || detectedUnderrun)
					{
						_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
					}
					_lastWriteTime = currentWriteTime;
					_lastWriteCursor = writeCursor;
				}
				catch (DirectSoundException)
				{
					samplesNeeded = 0;
				}
			}
			return samplesNeeded;
		}

		private int CircularDistance(int start, int end, int size)
		{
			return (end - start + size) % size;
		}

		public void WriteSamples(short[] samples, int sampleOffset, int sampleCount)
		{
			// For lack of a better place, this function will be the one that attempts to restart playing
			// after a sound buffer is lost.
			if (IsPlaying)
			{
				if (sampleCount == 0) return;
				try
				{
					_deviceBuffer.Write(samples, sampleOffset * Sound.ChannelCount, sampleCount * Sound.ChannelCount, _actualWriteOffsetBytes, LockFlags.None);
					_actualWriteOffsetBytes = (_actualWriteOffsetBytes + (sampleCount * Sound.BlockAlign)) % BufferSizeBytes;
					_filledBufferSizeBytes += sampleCount * Sound.BlockAlign;
				}
				catch (DirectSoundException)
				{
					_deviceBuffer.Restore();
					StartPlaying();
				}
			}
			else
			{
				if (_deviceBuffer != null)
				{
					_deviceBuffer.Restore();
				}
				StartPlaying();
			}
		}
	}
}
