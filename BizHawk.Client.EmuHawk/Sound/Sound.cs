using System;
using System.Threading;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Sound : IDisposable
	{
		public const int SampleRate = 44100;
		public const int BytesPerSample = 2;
		public const int ChannelCount = 2;
		public const int BlockAlign = BytesPerSample * ChannelCount;

		private bool _disposed;
		private ISoundOutput _soundOutput;

		private ISoundProvider _sourceProvider;
		private SyncSoundMode _syncMode;

		private SoundOutputProvider _outputProvider = new SoundOutputProvider();
		private readonly BufferedAsync _semiSync = new BufferedAsync();

		public Sound(IntPtr mainWindowHandle)
		{
#if WINDOWS
			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound)
				_soundOutput = new DirectSoundSoundOutput(this, mainWindowHandle);

			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.XAudio2)
				_soundOutput = new XAudio2SoundOutput(this);
#endif

			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.OpenAL)
				_soundOutput = new OpenALSoundOutput(this);

			if (_soundOutput == null)
				_soundOutput = new DummySoundOutput(this);
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

			_outputProvider.MaxSamplesDeficit = _soundOutput.MaxSamplesDeficit;

			Global.SoundMaxBufferDeficitMs = (int)Math.Ceiling(SamplesToMilliseconds(_soundOutput.MaxSamplesDeficit));

			IsStarted = true;
		}

		public void StopSound()
		{
			if (!IsStarted) return;

			_soundOutput.StopSound();

			_outputProvider.DiscardSamples();
			_semiSync.DiscardSamples();

			Global.SoundMaxBufferDeficitMs = 0;

			IsStarted = false;
		}

		/// <summary>
		/// Attaches a new input pin which will run either in sync or async mode depending
		/// on its SyncMode property. Once attached, the sync mode must not change unless
		/// the pin is re-attached.
		/// </summary>
		public void SetInputPin(ISoundProvider source)
		{
			_outputProvider.DiscardSamples();
			_outputProvider.BaseSoundProvider = null;

			_semiSync.DiscardSamples();
			_semiSync.BaseSoundProvider = null;

			_sourceProvider = source;
			if (_sourceProvider == null)
			{
				return;
			}

			_syncMode = _sourceProvider.SyncMode;
			if (_syncMode == SyncSoundMode.Sync)
			{
				_outputProvider.BaseSoundProvider = _sourceProvider;
			}
			else
			{
				_semiSync.BaseSoundProvider = _sourceProvider;
				_semiSync.RecalculateMagic(Global.Emulator.CoreComm.VsyncRate);
			}
		}

		public bool LogUnderruns { get; set; }

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

		internal void HandleInitializationOrUnderrun(bool isUnderrun, ref int samplesNeeded)
		{
			if ((!isUnderrun && InitializeBufferWithSilence) || (isUnderrun && RecoverFromUnderrunsWithSilence))
			{
				int samplesPerFrame = (int)Math.Round(SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - (SilenceLeaveRoomForFrameCount * samplesPerFrame), 0);
				_soundOutput.WriteSamples(new short[silenceSamples * 2], silenceSamples);
				samplesNeeded -= silenceSamples;
			}
		}

		internal void OnUnderrun()
		{
			if (!IsStarted) return;

			if (LogUnderruns) Console.WriteLine("Sound underrun detected!");
			_outputProvider.OnVolatility();
		}

		public void UpdateSound(float atten)
		{
			if (!Global.Config.SoundEnabled || !IsStarted || _sourceProvider == null || _disposed)
			{
				if (_sourceProvider != null) _sourceProvider.DiscardSamples();
				_outputProvider.DiscardSamples();
				return;
			}

			if (_sourceProvider.SyncMode != _syncMode)
			{
				throw new Exception("Sync mode changed unexpectedly.");
			}

			if (atten < 0) atten = 0;
			if (atten > 1) atten = 1;
			_soundOutput.ApplyVolumeSettings(atten);

			short[] samples;
			int samplesNeeded = _soundOutput.CalculateSamplesNeeded();
			int samplesProvided;

			if (atten == 0)
			{
				samples = new short[samplesNeeded * ChannelCount];
				samplesProvided = samplesNeeded;

				_sourceProvider.DiscardSamples();
				_outputProvider.DiscardSamples();
			}
			else if (_syncMode == SyncSoundMode.Sync)
			{
				if (Global.Config.SoundThrottle)
				{
					_sourceProvider.GetSamplesSync(out samples, out samplesProvided);

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
			else if (_syncMode == SyncSoundMode.Async)
			{
				samples = new short[samplesNeeded * ChannelCount];

				_semiSync.GetSamplesAsync(samples);

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

	public interface ISoundOutput : IDisposable
	{
		void StartSound();
		void StopSound();
		void ApplyVolumeSettings(double volume);
		int MaxSamplesDeficit { get; }
		int CalculateSamplesNeeded();
		void WriteSamples(short[] samples, int sampleCount);
	}
}
