using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SlimDX.DirectSound;
using SlimDX.Multimedia;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class DirectSoundSoundOutput : ISoundOutput
	{
		private bool _disposed;
		private Sound _sound;
		private DirectSound _device;
		private SecondarySoundBuffer _deviceBuffer;
		private int _actualWriteOffsetBytes = -1;
		private int _filledBufferSizeBytes;
		private long _lastWriteTime;
		private int _lastWriteCursor;

		public DirectSoundSoundOutput(Sound sound, IntPtr mainWindowHandle)
		{
			_sound = sound;

			var deviceInfo = DirectSound.GetDevices().FirstOrDefault(d => d.Description == Global.Config.SoundDevice);
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

		private int BufferSizeBytes
		{
			get { return BufferSizeSamples * Sound.BlockAlign; }
		}

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			// I'm not sure if this is "technically" correct but it works okay
			int range = (int)Volume.Maximum - (int)Volume.Minimum;
			_deviceBuffer.Volume = (int)(Math.Pow(volume, 0.1) * range) + (int)Volume.Minimum;
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);

			// 35 to 65 milliseconds depending on how big the buffer is. This is a trade-off
			// between more frequent but less severe glitches (i.e. catching underruns before
			// they happen and filling the buffer with silence) or less frequent but more
			// severe glitches. At least on my Windows 8 machines, the distance between the
			// play and write cursors can be up to 30 milliseconds, so that would be the
			// absolute minimum we could use here.
			int minBufferFullnessMs = Math.Min(35 + ((Global.Config.SoundBufferSizeMs - 60) / 2), 65);
			MaxSamplesDeficit = BufferSizeSamples - Sound.MillisecondsToSamples(minBufferFullnessMs);

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

			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;

			_deviceBuffer.Play(0, PlayFlags.Looping);
		}

		public void StopSound()
		{
			_deviceBuffer.Stop();
			_deviceBuffer.Dispose();
			_deviceBuffer = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			if (_deviceBuffer.Status == BufferStatus.BufferLost) return 0;

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
			int samplesNeeded = CircularDistance(_actualWriteOffsetBytes, playCursor, BufferSizeBytes) / Sound.BlockAlign;
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			_lastWriteTime = currentWriteTime;
			_lastWriteCursor = writeCursor;
			return samplesNeeded;
		}

		private int CircularDistance(int start, int end, int size)
		{
			return (end - start + size) % size;
		}

		public void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			int total = sampleCount * Sound.ChannelCount;
			if (total > samples.Length) { total = samples.Length; }
			_deviceBuffer.Write(samples, 0, total, _actualWriteOffsetBytes, LockFlags.None);
			_actualWriteOffsetBytes = (_actualWriteOffsetBytes + (sampleCount * Sound.BlockAlign)) % BufferSizeBytes;
			_filledBufferSizeBytes += sampleCount * Sound.BlockAlign;
		}
	}
}
