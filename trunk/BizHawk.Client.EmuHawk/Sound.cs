using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

#if WINDOWS
using SlimDX;
using SlimDX.DirectSound;
using SlimDX.Multimedia;
using SlimDX.XAudio2;
#endif

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface ISoundOutput : IDisposable
	{
		void StartSound();
		void StopSound();
		void ApplyVolumeSettings(double volume);
		int MaxSamplesDeficit { get; }
		int CalculateSamplesNeeded();
		void WriteSamples(short[] samples, int sampleCount);
	}

#if WINDOWS
	public class Sound : IDisposable
	{
		public const int SampleRate = 44100;
		public const int BytesPerSample = 2;
		public const int ChannelCount = 2;
		public const int BlockAlign = BytesPerSample * ChannelCount;

		private bool _disposed;
		private ISoundOutput _soundOutput;
		private ISyncSoundProvider _syncSoundProvider;
		private ISoundProvider _asyncSoundProvider;
		private SoundOutputProvider _outputProvider;
		private readonly BufferedAsync _semiSync = new BufferedAsync();

		public Sound(IntPtr mainWindowHandle)
		{
			_soundOutput = new DirectSoundSoundOutput(this, mainWindowHandle);
			//_soundOutput = new XAudio2SoundOutput(this);
		}

		public void Dispose()
		{
			if (_disposed) return;

			StopSound();

			_soundOutput.Dispose();
			_soundOutput = null;

			_disposed = true;
		}

		public bool IsStarted { get; private set; }

		public void StartSound()
		{
			if (_disposed) return;
			if (!Global.Config.SoundEnabled) return;
			if (IsStarted) return;

			_soundOutput.StartSound();

			_outputProvider = new SoundOutputProvider();
			_outputProvider.MaxSamplesDeficit = _soundOutput.MaxSamplesDeficit;
			_outputProvider.BaseSoundProvider = _syncSoundProvider;

			Global.SoundMaxBufferDeficitMs = (int)Math.Ceiling(SamplesToMilliseconds(_soundOutput.MaxSamplesDeficit));

			IsStarted = true;

			ApplyVolumeSettings();

			//LogUnderruns = true;
			//_outputProvider.LogDebug = true;
		}

		public void StopSound()
		{
			if (!IsStarted) return;

			_soundOutput.StopSound();

			_outputProvider = null;

			Global.SoundMaxBufferDeficitMs = 0;

			IsStarted = false;
		}

		public void ApplyVolumeSettings()
		{
			if (!IsStarted) return;

			double volume = Global.Config.SoundVolume / 100.0;
			if (volume < 0.0) volume = 0.0;
			if (volume > 1.0) volume = 1.0;
			_soundOutput.ApplyVolumeSettings(volume);
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

		public bool InitializeBufferWithSilence
		{
			get { return true; }
		}

		public bool RecoverFromUnderrunsWithSilence
		{
			get { return true; }
		}

		public int SilenceLeaveRoomForFrameCount
		{
			get { return Global.Config.SoundThrottle ? 1 : 2; } // Why 2? I don't know, but it seems to work well with the clock throttle's behavior.
		}

		public bool LogUnderruns { get; set; }

		public void OnUnderrun()
		{
			if (!IsStarted) return;

			if (LogUnderruns) Console.WriteLine("Sound underrun detected!");
			_outputProvider.OnVolatility();
		}

		public void UpdateSound(bool outputSilence)
		{
			if (!Global.Config.SoundEnabled || !IsStarted || _disposed)
			{
				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
				return;
			}

			short[] samples;
			int samplesNeeded = _soundOutput.CalculateSamplesNeeded();
			int samplesProvided;

			if (outputSilence)
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
						Thread.Sleep((samplesProvided - samplesNeeded) / (SampleRate / 1000)); // Let the audio clock control sleep time
						samplesNeeded = _soundOutput.CalculateSamplesNeeded();
					}
				}
				else
				{
					if (Global.DisableSecondaryThrottling) // This indicates rewind or fast-forward
					{
						_outputProvider.OnVolatility();
					}
					_outputProvider.GetSamples(samplesNeeded, out samples, out samplesProvided);
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

			_soundOutput.WriteSamples(samples, samplesProvided);
		}

		public static int MillisecondsToSamples(int milliseconds)
		{
			return milliseconds * SampleRate / 1000;
		}

		public static double SamplesToMilliseconds(int samples)
		{
			return samples * 1000.0 / SampleRate;
		}
	}

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
			_deviceBuffer.Volume = (int)(Math.Pow(volume, 0.15) * range) + (int)Volume.Minimum;
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
						SlimDX.DirectSound.BufferFlags.GlobalFocus |
						SlimDX.DirectSound.BufferFlags.Software |
						SlimDX.DirectSound.BufferFlags.GetCurrentPosition2 |
						SlimDX.DirectSound.BufferFlags.ControlVolume,
					SizeInBytes = BufferSizeBytes
				};

			_deviceBuffer = new SecondarySoundBuffer(_device, desc);

			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;

			_deviceBuffer.Play(0, SlimDX.DirectSound.PlayFlags.Looping);
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
			long currentWriteTime = Stopwatch.GetTimestamp();
			int playCursor = _deviceBuffer.CurrentPlayPosition;
			int writeCursor = _deviceBuffer.CurrentWritePosition;
			bool detectedUnderrun = false;
			if (_actualWriteOffsetBytes != -1)
			{
				double elapsedSeconds = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency;
				double bufferSizeSeconds = (double)BufferSizeSamples / Sound.SampleRate;
				int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSizeBytes);
				cursorDelta += BufferSizeBytes * (int)Math.Round((elapsedSeconds - (cursorDelta / (double)(Sound.SampleRate * Sound.BlockAlign))) / bufferSizeSeconds);
				_filledBufferSizeBytes -= cursorDelta;
				if (_filledBufferSizeBytes < 0)
				{
					_sound.OnUnderrun();
					detectedUnderrun = true;
				}
			}
			bool isInitializing = _actualWriteOffsetBytes == -1;
			if (isInitializing || detectedUnderrun)
			{
				_actualWriteOffsetBytes = writeCursor;
				_filledBufferSizeBytes = 0;
			}
			int samplesNeeded = CircularDistance(_actualWriteOffsetBytes, playCursor, BufferSizeBytes) / Sound.BlockAlign;
			if ((isInitializing && _sound.InitializeBufferWithSilence) || (detectedUnderrun && _sound.RecoverFromUnderrunsWithSilence))
			{
				int samplesPerFrame = (int)Math.Round(Sound.SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - (_sound.SilenceLeaveRoomForFrameCount * samplesPerFrame), 0);
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

		public void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			_deviceBuffer.Write(samples, 0, sampleCount * Sound.ChannelCount, _actualWriteOffsetBytes, LockFlags.None);
			_actualWriteOffsetBytes = (_actualWriteOffsetBytes + (sampleCount * Sound.BlockAlign)) % BufferSizeBytes;
			_filledBufferSizeBytes += sampleCount * Sound.BlockAlign;
		}
	}

	public class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private Sound _sound;
		private XAudio2 _device;
		private MasteringVoice _masteringVoice;
		private SourceVoice _sourceVoice;
		private long _runningSamplesQueued;

		public XAudio2SoundOutput(Sound sound)
		{
			_sound = sound;
			_device = new XAudio2();
			int? deviceIndex = Enumerable.Range(0, _device.DeviceCount)
				.Select(n => (int?)n)
				.FirstOrDefault(n => _device.GetDeviceDetails(n.Value).DisplayName == Global.Config.SoundDevice);
			_masteringVoice = deviceIndex == null ?
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate) :
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate, deviceIndex.Value);
		}

		public void Dispose()
		{
			if (_disposed) return;

			_masteringVoice.Dispose();
			_masteringVoice = null;

			_device.Dispose();
			_device = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			using (XAudio2 device = new XAudio2())
			{
				return Enumerable.Range(0, device.DeviceCount).Select(n => device.GetDeviceDetails(n).DisplayName);
			}
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_sourceVoice.Volume = (float)volume;
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			var format = new WaveFormat
				{
					SamplesPerSecond = Sound.SampleRate,
					BitsPerSample = Sound.BytesPerSample * 8,
					Channels = Sound.ChannelCount,
					FormatTag = WaveFormatTag.Pcm,
					BlockAlignment = Sound.BlockAlign,
					AverageBytesPerSecond = Sound.SampleRate * Sound.BlockAlign
				};

			_sourceVoice = new SourceVoice(_device, format);

			_runningSamplesQueued = 0;

			_sourceVoice.Start();
		}

		public void StopSound()
		{
			_sourceVoice.Stop();
			_sourceVoice.Dispose();
			_sourceVoice = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			long samplesAwaitingPlayback = _runningSamplesQueued - _sourceVoice.State.SamplesPlayed;
			bool isInitializing = _runningSamplesQueued == 0;
			bool detectedUnderrun = !isInitializing && _sourceVoice.State.BuffersQueued == 0;
			if (detectedUnderrun)
			{
				_sound.OnUnderrun();
			}
			int samplesNeeded = (int)Math.Max(BufferSizeSamples - samplesAwaitingPlayback, 0);
			if ((isInitializing && _sound.InitializeBufferWithSilence) || (detectedUnderrun && _sound.RecoverFromUnderrunsWithSilence))
			{
				int samplesPerFrame = (int)Math.Round(Sound.SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - (_sound.SilenceLeaveRoomForFrameCount * samplesPerFrame), 0);
				WriteSamples(new short[silenceSamples * 2], silenceSamples);
				samplesNeeded -= silenceSamples;
			}
			return samplesNeeded;
		}

		public void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			// TODO: Re-use these buffers
			byte[] bytes = new byte[sampleCount * Sound.BlockAlign];
			Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
			_sourceVoice.SubmitSourceBuffer(new AudioBuffer {
				AudioBytes = bytes.Length,
				AudioData = new DataStream(bytes, true, false)
			});
			_runningSamplesQueued += sampleCount;
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
