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

		private ISoundProvider _soundProvider;

		private SoundOutputProvider _outputProvider;
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

			_outputProvider = new SoundOutputProvider();
			_outputProvider.MaxSamplesDeficit = _soundOutput.MaxSamplesDeficit;
			_outputProvider.BaseSoundProvider = _soundProvider;

			Global.SoundMaxBufferDeficitMs = (int)Math.Ceiling(SamplesToMilliseconds(_soundOutput.MaxSamplesDeficit));

			IsStarted = true;
		}

		public void StopSound()
		{
			if (!IsStarted) return;

			_soundOutput.StopSound();

			_outputProvider = null;

			Global.SoundMaxBufferDeficitMs = 0;

			IsStarted = false;
		}

		// Sound refactor TODO: combine these methods, and check the SyncMode for behavior
		/// <summary>
		/// attach a new input pin, of the `sync` variety
		/// </summary>
		public void SetSyncInputPin(ISoundProvider source)
		{
			if (source.SyncMode != SyncSoundMode.Sync)
				throw new InvalidOperationException("SetSyncInputPin() must be called with a sync input");

			_semiSync.DiscardSamples();
			_semiSync.ClearSoundProvider();
			_soundProvider = source;
			if (_outputProvider != null)
			{
				_outputProvider.BaseSoundProvider = source;
			}
		}

		/// <summary>
		/// attach a new input pin, of the `async` variety
		/// </summary>
		public void SetAsyncInputPin(ISoundProvider source)
		{
			if (source.SyncMode != SyncSoundMode.Async)
				throw new InvalidOperationException("SetAsyncInputPin() must be called with a async input");

			if (_outputProvider != null)
			{
				_outputProvider.DiscardSamples();
				_outputProvider.BaseSoundProvider = null;
			}

			_soundProvider = source;
			_semiSync.SetAsyncSoundProvider(source);
			_semiSync.RecalculateMagic(Global.Emulator.CoreComm.VsyncRate);
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
				int samplesPerFrame = (int)Math.Round(Sound.SampleRate / Global.Emulator.CoreComm.VsyncRate);
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
			if (!Global.Config.SoundEnabled || !IsStarted || _disposed)
			{
				if (_soundProvider != null) _soundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
				return;
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

				if (_soundProvider != null) _soundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
			}
			else if (_soundProvider != null && _soundProvider.SyncMode == SyncSoundMode.Sync)
			{
				if (Global.Config.SoundThrottle)
				{
					_soundProvider.GetSamplesSync(out samples, out samplesProvided);

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
			else if (_soundProvider != null && _soundProvider.SyncMode == SyncSoundMode.Async)
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
