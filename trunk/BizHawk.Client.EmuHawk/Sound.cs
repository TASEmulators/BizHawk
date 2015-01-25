using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#if WINDOWS
using SlimDX.DirectSound;
using SlimDX.Multimedia;
#endif

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
#if WINDOWS
	public static class SoundEnumeration
	{
		public static DirectSound Create()
		{
			var dc = DirectSound.GetDevices();
			foreach (var dev in dc)
			{
				if (dev.Description == Global.Config.SoundDevice)
					return new DirectSound(dev.DriverGuid);
			}
			return new DirectSound();
		}

		public static IEnumerable<string> DeviceNames()
		{
			var ret = new List<string>();
			var dc = DirectSound.GetDevices();
			foreach (var dev in dc)
				ret.Add(dev.Description);
			return ret;
		}
	}

	public class Sound : IDisposable
	{
		private const int SampleRate = 44100;
		private const int BytesPerSample = 2;
		private const int ChannelCount = 2;
		private const int BlockAlign = BytesPerSample * ChannelCount;

		private bool _muted;
		private bool _disposed;
		private DirectSound _device;
		private SecondarySoundBuffer _deviceBuffer;
		private readonly BufferedAsync _semiSync = new BufferedAsync();
		private SoundOutputProvider _outputProvider;
		private ISoundProvider _asyncSoundProvider;
		private ISyncSoundProvider _syncSoundProvider;
		private int _actualWriteOffsetBytes = -1;
		private int _filledBufferSizeBytes;
		private long _lastWriteTime;
		private int _lastWriteCursor;

		public Sound(IntPtr handle, DirectSound device)
		{
			if (device == null) return;

			device.SetCooperativeLevel(handle, CooperativeLevel.Priority);
			_device = device;
		}

		private int BufferSizeSamples { get; set; }

		private int BufferSizeBytes
		{
			get { return BufferSizeSamples * BlockAlign; }
		}

		private void CreateDeviceBuffer()
		{
			BufferSizeSamples = MillisecondsToSamples(Global.Config.SoundBufferSizeMs);

			var format = new WaveFormat
				{
					SamplesPerSecond = SampleRate,
					BitsPerSample = BytesPerSample * 8,
					Channels = ChannelCount,
					FormatTag = WaveFormatTag.Pcm,
					BlockAlignment = BlockAlign,
					AverageBytesPerSecond = SampleRate * BlockAlign
				};

			var desc = new SoundBufferDescription
				{
					Format = format,
					Flags = BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume,
					SizeInBytes = BufferSizeBytes
				};

			_deviceBuffer = new SecondarySoundBuffer(_device, desc);
		}

		public void ApplyVolumeSettings()
		{
			if (_deviceBuffer == null) return;

			_deviceBuffer.Volume = Global.Config.SoundVolume == 0 ? -10000 : ((100 - Global.Config.SoundVolume) * -45);
		}

		public void StartSound()
		{
			if (_disposed) throw new ObjectDisposedException("Sound");
			if (!Global.Config.SoundEnabled) return;
			if (_deviceBuffer != null) return;

			CreateDeviceBuffer();
			ApplyVolumeSettings();

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.CurrentPlayPosition = 0;
			_deviceBuffer.Play(0, PlayFlags.Looping);

			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;

			int minBufferFullnessSamples = MillisecondsToSamples(
				Global.Config.SoundBufferSizeMs < 80 ? 35 :
				Global.Config.SoundBufferSizeMs < 100 ? 45 :
				55);
			_outputProvider = new SoundOutputProvider();
			_outputProvider.MaxSamplesDeficit = BufferSizeSamples - minBufferFullnessSamples;
			_outputProvider.BaseSoundProvider = _syncSoundProvider;

			//LogUnderruns = true;
			//_outputProvider.LogDebug = true;
		}

		public void StopSound()
		{
			if (_deviceBuffer == null) return;

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.Stop();
			_deviceBuffer.Dispose();
			_deviceBuffer = null;

			_outputProvider = null;

			BufferSizeSamples = 0;
		}

		public void Dispose()
		{
			if (_disposed) return;
			StopSound();
			_disposed = true;
		}

		public void SetSyncInputPin(ISyncSoundProvider source)
		{
			if (_asyncSoundProvider != null)
			{
				_asyncSoundProvider.DiscardSamples();
				_asyncSoundProvider = null;
			}
			_semiSync.DiscardSamples();
			_semiSync.BaseSoundProvider = null;
			_syncSoundProvider = source;
			if (_outputProvider != null)
			{
				_outputProvider.BaseSoundProvider = source;
			}
		}

		public void SetAsyncInputPin(ISoundProvider source)
		{
			if (_syncSoundProvider != null)
			{
				_syncSoundProvider.DiscardSamples();
				_syncSoundProvider = null;
			}
			if (_outputProvider != null)
			{
				_outputProvider.DiscardSamples();
				_outputProvider.BaseSoundProvider = null;
			}
			_asyncSoundProvider = source;
			_semiSync.BaseSoundProvider = source;
			_semiSync.RecalculateMagic(Global.CoreComm.VsyncRate);
		}

		private bool InitializeBufferWithSilence
		{
			get { return true; }
		}

		private bool RecoverFromUnderrunsWithSilence
		{
			get { return true; }
		}

		private int SilenceLeaveRoomForFrameCount
		{
			get { return Global.Config.SoundThrottle ? 1 : 2; } // Why 2? I don't know, but it seems to work well with the clock throttle's behavior.
		}

		public bool LogUnderruns { get; set; }

		private int CalculateSamplesNeeded()
		{
			long currentWriteTime = Stopwatch.GetTimestamp();
			int playCursor = _deviceBuffer.CurrentPlayPosition;
			int writeCursor = _deviceBuffer.CurrentWritePosition;
			bool detectedUnderrun = false;
			if (_actualWriteOffsetBytes != -1)
			{
				double elapsedSeconds = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency;
				double bufferSizeSeconds = (double)BufferSizeSamples / SampleRate;
				int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSizeBytes);
				cursorDelta += BufferSizeBytes * (int)Math.Round((elapsedSeconds - (cursorDelta / (double)(SampleRate * BlockAlign))) / bufferSizeSeconds);
				_filledBufferSizeBytes -= cursorDelta;
				if (_filledBufferSizeBytes < 0)
				{
					if (LogUnderruns) Console.WriteLine("DirectSound underrun detected!");
					detectedUnderrun = true;
				}
			}
			bool isInitializing = _actualWriteOffsetBytes == -1;
			if (isInitializing || detectedUnderrun)
			{
				_actualWriteOffsetBytes = writeCursor;
				_filledBufferSizeBytes = 0;
			}
			int samplesNeeded = CircularDistance(_actualWriteOffsetBytes, playCursor, BufferSizeBytes) / BlockAlign;
			if ((isInitializing && InitializeBufferWithSilence) || (detectedUnderrun && RecoverFromUnderrunsWithSilence))
			{
				int samplesPerFrame = (int)Math.Round(SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - (SilenceLeaveRoomForFrameCount * samplesPerFrame), 0);
				WriteSamples(new short[silenceSamples * 2], silenceSamples);
				samplesNeeded -= silenceSamples;
			}
			_lastWriteTime = currentWriteTime;
			_lastWriteCursor = writeCursor;
			return samplesNeeded;
		}

		private int CircularDistance(int start, int end, int size)
		{
			return (end - start + size) % size;
		}

		private void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			_deviceBuffer.Write(samples, 0, sampleCount * ChannelCount, _actualWriteOffsetBytes, LockFlags.None);
			AdvanceWriteOffset(sampleCount);
		}

		private void AdvanceWriteOffset(int sampleCount)
		{
			_actualWriteOffsetBytes = (_actualWriteOffsetBytes + (sampleCount * BlockAlign)) % BufferSizeBytes;
			_filledBufferSizeBytes += sampleCount * BlockAlign;
		}

		public void UpdateSilence()
		{
			_muted = true;
			UpdateSound();
			_muted = false;
		}

		public void UpdateSound()
		{
			if (!Global.Config.SoundEnabled || _deviceBuffer == null || _disposed)
			{
				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
				return;
			}

			short[] samples;
			int samplesNeeded = CalculateSamplesNeeded();
			int samplesProvided;

			if (_muted)
			{
				samples = new short[samplesNeeded * ChannelCount];
				samplesProvided = samplesNeeded;

				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
			}
			else if (_syncSoundProvider != null)
			{
				if (Global.Config.SoundThrottle)
				{
					_syncSoundProvider.GetSamples(out samples, out samplesProvided);

					while (samplesNeeded < samplesProvided && !Global.DisableSecondaryThrottling)
					{
						Thread.Sleep((samplesProvided - samplesNeeded) / (SampleRate / 1000)); // let audio clock control sleep time
						samplesNeeded = CalculateSamplesNeeded();
					}
				}
				else
				{
					samples = new short[samplesNeeded * ChannelCount];

					samplesProvided = _outputProvider.GetSamples(samples, samplesNeeded);
				}
			}
			else if (_asyncSoundProvider != null)
			{
				samples = new short[samplesNeeded * ChannelCount];

				_semiSync.GetSamples(samples);

				samplesProvided = samplesNeeded;
			}
			else
			{
				return;
			}

			WriteSamples(samples, samplesProvided);
		}

		private static int MillisecondsToSamples(int milliseconds)
		{
			return milliseconds * SampleRate / 1000;
		}
	}
#else
	// Dummy implementation for non-Windows platforms for now.
	public class Sound
	{
		public bool Muted = false;
		public bool needDiscard;

		public Sound()
		{
		}

		public void StartSound()
		{
		}

		public bool IsPlaying = false;

		public void StopSound()
		{
		}

		public void Dispose()
		{
		}

		int CalculateSamplesNeeded()
		{
			return 0;
		}

		public void UpdateSound(ISoundProvider soundProvider)
		{
			soundProvider.DiscardSamples();
		}

		/// <summary>
		/// Range: 0-100
		/// </summary>
		/// <param name="vol"></param>
		public void ChangeVolume(int vol)
		{
			Global.Config.SoundVolume = vol;
			UpdateSoundSettings();
		}

		/// <summary>
		/// Uses Global.Config.SoundEnabled, this just notifies the object to read it
		/// </summary>
		public void UpdateSoundSettings()
		{
			if (Global.Emulator is NES)
			{
				NES n = Global.Emulator as NES;
				if (Global.Config.SoundEnabled == false)
					n.SoundOn = false;
				else
					n.SoundOn = true;
			}
		}
	}
#endif
}
