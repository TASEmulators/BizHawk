using System;
using System.Diagnostics;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class DummySoundOutput : ISoundOutput
	{
		private Sound _sound;
		private int _remainingSamples;
		private long _lastWriteTime;

		public DummySoundOutput(Sound sound)
		{
			_sound = sound;
		}

		public void Dispose()
		{
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			_lastWriteTime = 0;
		}

		public void StopSound()
		{
			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			long currentWriteTime = Stopwatch.GetTimestamp();
			bool isInitializing = _lastWriteTime == 0;
			bool detectedUnderrun = false;
			if (!isInitializing)
			{
				double elapsedSeconds = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency;
				// Due to rounding errors this doesn't work well in audio throttle mode unless enough time has passed
				if (elapsedSeconds >= 0.001)
				{
					_remainingSamples -= (int)Math.Round(elapsedSeconds * Sound.SampleRate);
					if (_remainingSamples < 0)
					{
						_remainingSamples = 0;
						detectedUnderrun = true;
					}
					_lastWriteTime = currentWriteTime;
				}
			}
			else
			{
				_lastWriteTime = currentWriteTime;
			}
			int samplesNeeded = BufferSizeSamples - _remainingSamples;
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			return samplesNeeded;
		}

		public void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			_remainingSamples += sampleCount;
		}
	}
}
