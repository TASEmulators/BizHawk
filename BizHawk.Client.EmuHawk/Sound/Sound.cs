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
		private readonly ISoundOutput _outputDevice;
		private readonly SoundOutputProvider _outputProvider = new SoundOutputProvider(); // Buffer for Sync sources
		private readonly BufferedAsync _bufferedAsync = new BufferedAsync(); // Buffer for Async sources
		private IBufferedSoundProvider _bufferedProvider; // One of the preceding buffers, or null if no source is set

		public Sound(IntPtr mainWindowHandle)
		{
#if WINDOWS
			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound)
				_outputDevice = new DirectSoundSoundOutput(this, mainWindowHandle);

			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.XAudio2)
				_outputDevice = new XAudio2SoundOutput(this);
#endif

			if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.OpenAL)
				_outputDevice = new OpenALSoundOutput(this);

			if (_outputDevice == null)
				_outputDevice = new DummySoundOutput(this);
		}

		public void Dispose()
		{
			if (_disposed) return;

			StopSound();

			_outputDevice.Dispose();

			_disposed = true;
		}

		public bool IsStarted { get; private set; }

		public void StartSound()
		{
			if (_disposed) return;
			if (!Global.Config.SoundEnabled) return;
			if (IsStarted) return;

			_outputDevice.StartSound();

			_outputProvider.MaxSamplesDeficit = _outputDevice.MaxSamplesDeficit;

			Global.SoundMaxBufferDeficitMs = (int)Math.Ceiling(SamplesToMilliseconds(_outputDevice.MaxSamplesDeficit));

			IsStarted = true;
		}

		public void StopSound()
		{
			if (!IsStarted) return;

			_outputDevice.StopSound();

			if (_bufferedProvider != null) _bufferedProvider.DiscardSamples();

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
			if (_bufferedProvider != null)
			{
				_bufferedProvider.BaseSoundProvider = null;
				_bufferedProvider.DiscardSamples();
				_bufferedProvider = null;
			}

			if (source == null) return;

			if (source.SyncMode == SyncSoundMode.Sync)
			{
				_bufferedProvider = _outputProvider;
			}
			else if (source.SyncMode == SyncSoundMode.Async)
			{
				_bufferedAsync.RecalculateMagic(Global.Emulator.CoreComm.VsyncRate);
				_bufferedProvider = _bufferedAsync;
			}
			else throw new InvalidOperationException("Unsupported sync mode.");

			_bufferedProvider.BaseSoundProvider = source;
		}

		public bool LogUnderruns { get; set; }

		internal void HandleInitializationOrUnderrun(bool isUnderrun, ref int samplesNeeded)
		{
			// Fill device buffer with silence but leave enough room for one frame
			int samplesPerFrame = (int)Math.Round(SampleRate / Global.Emulator.CoreComm.VsyncRate);
			int silenceSamples = Math.Max(samplesNeeded - samplesPerFrame, 0);
			_outputDevice.WriteSamples(new short[silenceSamples * 2], silenceSamples);
			samplesNeeded -= silenceSamples;

			if (isUnderrun)
			{
				if (LogUnderruns) Console.WriteLine("Sound underrun detected!");
				_outputProvider.OnVolatility();
			}
		}

		public void UpdateSound(float atten)
		{
			if (!Global.Config.SoundEnabled || !IsStarted || _bufferedProvider == null || _disposed)
			{
				if (_bufferedProvider != null) _bufferedProvider.DiscardSamples();
				return;
			}

			if (atten < 0) atten = 0;
			if (atten > 1) atten = 1;
			_outputDevice.ApplyVolumeSettings(atten);

			short[] samples;
			int samplesNeeded = _outputDevice.CalculateSamplesNeeded();
			int samplesProvided;

			if (atten == 0)
			{
				samples = new short[samplesNeeded * ChannelCount];
				samplesProvided = samplesNeeded;

				_bufferedProvider.DiscardSamples();
			}
			else if (_bufferedProvider == _outputProvider)
			{
				if (Global.Config.SoundThrottle)
				{
					_outputProvider.BaseSoundProvider.GetSamplesSync(out samples, out samplesProvided);

					if (Global.DisableSecondaryThrottling && samplesProvided > samplesNeeded)
					{
						return;
					}

					while (samplesProvided > samplesNeeded)
					{
						Thread.Sleep((samplesProvided - samplesNeeded) / (SampleRate / 1000)); // Let the audio clock control sleep time
						samplesNeeded = _outputDevice.CalculateSamplesNeeded();
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
			else if (_bufferedProvider == _bufferedAsync)
			{
				samples = new short[samplesNeeded * ChannelCount];

				_bufferedAsync.GetSamplesAsync(samples);

				samplesProvided = samplesNeeded;
			}
			else
			{
				return;
			}

			_outputDevice.WriteSamples(samples, samplesProvided);
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
}
