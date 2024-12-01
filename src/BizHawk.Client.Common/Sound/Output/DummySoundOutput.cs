using System.Diagnostics;
using System.IO;

namespace BizHawk.Client.Common
{
	public class DummySoundOutput : ISoundOutput
	{
		private readonly IHostAudioManager _sound;
		private int _remainingSamples;
		private long _lastWriteTime;

		public DummySoundOutput(IHostAudioManager sound)
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
			BufferSizeSamples = _sound.MillisecondsToSamples(_sound.ConfigBufferSizeMs);
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
					_remainingSamples -= (int) Math.Round(elapsedSeconds * _sound.SampleRate);
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

		public void WriteSamples(short[] samples, int sampleOffset, int sampleCount)
		{
			if (sampleCount == 0) return;
			_remainingSamples += sampleCount;
		}

		public void PlayWavFile(Stream wavFile, double volume)
		{
		}
	}
}
