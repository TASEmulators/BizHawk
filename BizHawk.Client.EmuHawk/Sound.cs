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
		private const int BufferSizeMilliseconds = 100;
		private const int BufferSizeSamples = SampleRate * BufferSizeMilliseconds / 1000;
		private const int BufferSizeBytes = BufferSizeSamples * BlockAlign;
		private const double BufferSizeSeconds = (double)(BufferSizeBytes / BlockAlign) / SampleRate;
		private const int MinBufferFullnessMilliseconds = 55;
		private const int MinBufferFullnessSamples = SampleRate * MinBufferFullnessMilliseconds / 1000;

		private bool _muted;
		private bool _disposed;
		private SecondarySoundBuffer _deviceBuffer;
		private readonly BufferedAsync _semiSync = new BufferedAsync();
		private readonly SoundOutputProvider _outputProvider = new SoundOutputProvider();
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
					Flags =
						BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume,
					SizeInBytes = BufferSizeBytes
				};

			_deviceBuffer = new SecondarySoundBuffer(device, desc);

			ChangeVolume(Global.Config.SoundVolume);

			//LogUnderruns = true;
			//_outputProvider.LogDebug = true;
		}

		public void StartSound()
		{
			if (_disposed) throw new ObjectDisposedException("Sound");
			if (Global.Config.SoundEnabled == false) return;
			if (_deviceBuffer == null) return;
			if (IsPlaying) return;

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.CurrentPlayPosition = 0;
			_deviceBuffer.Play(0, PlayFlags.Looping);

			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;
		}

		bool IsPlaying
		{
			get
			{
				if (_deviceBuffer == null) return false;
				if ((_deviceBuffer.Status & BufferStatus.Playing) != 0) return true;
				return false;
			}
		}

		public void StopSound()
		{
			if (!IsPlaying) return;

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.Stop();
		}

		public void Dispose()
		{
			if (_disposed) return;
			if (_deviceBuffer != null && _deviceBuffer.Disposed == false)
			{
				_deviceBuffer.Dispose();
				_deviceBuffer = null;
			}
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
			_outputProvider.BaseSoundProvider = source;
		}

		public void SetAsyncInputPin(ISoundProvider source)
		{
			if (_syncSoundProvider != null)
			{
				_syncSoundProvider.DiscardSamples();
				_syncSoundProvider = null;
			}
			_outputProvider.DiscardSamples();
			_outputProvider.BaseSoundProvider = null;
			_asyncSoundProvider = source;
			_semiSync.BaseSoundProvider = source;
			_semiSync.RecalculateMagic(Global.CoreComm.VsyncRate);
		}

		public bool InitializeBufferWithSilence
		{
			get { return Global.Config.SoundThrottle; }
		}

		public bool RecoverFromUnderrunsWithSilence
		{
			get { return Global.Config.SoundThrottle; }
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
				int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSizeBytes);
				cursorDelta += BufferSizeBytes * (int)Math.Round((elapsedSeconds - (cursorDelta / (double)(SampleRate * BlockAlign))) / BufferSizeSeconds);
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
				// Fill the buffer with silence but leave enough empty for one frame's audio
				int samplesPerFrame = (int)Math.Round(SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - samplesPerFrame, 0);
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
			if (Global.Config.SoundEnabled == false || _disposed)
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

					samplesProvided = _outputProvider.GetSamples(samples, samplesNeeded, samplesNeeded - (BufferSizeSamples - MinBufferFullnessSamples));
				}
			}
			else if (_asyncSoundProvider != null)
			{
				samples = new short[samplesNeeded * ChannelCount];

				_semiSync.GetSamples(samples);

				samplesProvided = samplesNeeded;
			}
			else
				return;

			WriteSamples(samples, samplesProvided);
		}

		/// <summary>
		/// Range: 0-100
		/// </summary>
		/// <param name="vol"></param>
		public void ChangeVolume(int vol)
		{
			if (vol > 100)
				vol = 100;
			if (vol < 0)
				vol = 0;
			Global.Config.SoundVolume = vol;
			UpdateSoundSettings();
		}

		/// <summary>
		/// Uses Global.Config.SoundEnabled, this just notifies the object to read it
		/// </summary>
		public void UpdateSoundSettings()
		{
			if (!Global.Config.SoundEnabled || Global.Config.SoundVolume == 0)
				_deviceBuffer.Volume = -5000;
			else
				_deviceBuffer.Volume = 0 - ((100 - Global.Config.SoundVolume) * 45);
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
