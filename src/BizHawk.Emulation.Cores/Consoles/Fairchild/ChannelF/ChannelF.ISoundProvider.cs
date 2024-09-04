using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Sound related functions
	/// </summary>
	public partial class ChannelF : ISoundProvider
	{				
		private int tone = 0;
		private short[] sampleBuffer;
		private int[] toneBuffer;
		private readonly double sampleRate = 44100;
		private readonly double decay = 0.998;
		private readonly int rampUpTime = 10;
		private int samplesPerFrame;
		private double cyclesPerSample;
		private double amplitude = 0;
		private int rampCounter = 0;

		private void SetupAudio()
		{
			samplesPerFrame = (int)(sampleRate / refreshRate);
			cyclesPerSample = ClockPerFrame / (double)samplesPerFrame;
			sampleBuffer = new short[samplesPerFrame];
			toneBuffer = new int[samplesPerFrame];
		}

		private void AudioChange()
		{
			var currSample = (int)(FrameClock / cyclesPerSample);

			while (currSample < samplesPerFrame)
			{
				toneBuffer[currSample++] = tone;
			}
		}

		private int GetWaveSample(double time, int tone)
		{
			int t = 0;

			switch (tone)
			{
				case 0:
					// silence
					t = 0;
					break;
				case 1:
					// 1000Hz tone
					t = GetSquareWaveSample(time, 1000);
					break;
				case 2:
					// 500 Hz tone
					t = GetSquareWaveSample(time, 500);
					break;
				case 3:
					// 120 Hz tone
					t = GetSquareWaveSample(time, 120);
					t += GetSquareWaveSample(time, 240);
					break;
			}

			return t;
		}
		
		private int GetSineWaveSample(double time, double freq)
		{
			double sTime = time / sampleRate;
			double sAngle = 2.0 * Math.PI * sTime * freq;
			return (int)(Math.Sin(sAngle) * 0x8000);
		}		

		private int GetSquareWaveSample(double time, double freq)
		{
			double sTime = time / sampleRate;
			double period = 1.0 / freq;
			double halfPeriod = period / 2.0;
			double currentTimeInPeriod = sTime % period;
			return currentTimeInPeriod < halfPeriod ? 32766 : -32766;
		}

		private void ApplyLowPassFilter(short[] samples, double cutoffFrequency)
		{
			if (samples == null || samples.Length == 0)
				return;

			double rc = 1.0 / (cutoffFrequency * 2 * Math.PI);
			double dt = 1.0 / sampleRate;
			double alpha = dt / (rc + dt);

			short previousSample = samples[0];
			for (int i = 1; i < samples.Length; i++)
			{
				samples[i] = (short)(previousSample + alpha * (samples[i] - previousSample));
				previousSample = samples[i];
			}
		}

		private void ApplyBassBoostFilter(short[] samples, double boostAmount, double cutoffFrequency)
		{
			if (samples == null || samples.Length == 0)
				return;

			double A = Math.Pow(10, boostAmount / 40);
			double omega = 2 * Math.PI * cutoffFrequency / sampleRate;
			double sn = Math.Sin(omega);
			double cs = Math.Cos(omega);
			double alpha = sn / 2 * Math.Sqrt((A + 1 / A) * (1 / 0.707 - 1) + 2);
			double beta = 2 * Math.Sqrt(A) * alpha;

			double b0 = A * ((A + 1) - (A - 1) * cs + beta);
			double b1 = 2 * A * ((A - 1) - (A + 1) * cs);
			double b2 = A * ((A + 1) - (A - 1) * cs - beta);
			double a0 = (A + 1) + (A - 1) * cs + beta;
			double a1 = -2 * ((A - 1) + (A + 1) * cs);
			double a2 = (A + 1) + (A - 1) * cs - beta;

			double[] filteredSamples = new double[samples.Length];
			filteredSamples[0] = samples[0];
			filteredSamples[1] = samples[1];

			for (int i = 2; i < samples.Length; i++)
			{
				filteredSamples[i] = (b0 / a0) * samples[i] + (b1 / a0) * samples[i - 1] + (b2 / a0) * samples[i - 2]
									 - (a1 / a0) * filteredSamples[i - 1] - (a2 / a0) * filteredSamples[i - 2];
			}

			for (int i = 0; i < samples.Length; i++)
			{
				samples[i] = Clamp((short)filteredSamples[i], short.MinValue, short.MaxValue);
			}

			short Clamp(short value, short min, short max)
			{
				if (value < min) return min;
				if (value > max) return max;
				return value;
			}
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new InvalidOperationException("Only Sync mode is supported.");
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			sampleBuffer = new short[samplesPerFrame];

			// pre-populate the tonebuffer with the last active tone from the frame (will be overwritten if and when a tone change happens)
			int lastTone = toneBuffer[^1];
			for (int i = 0; i < toneBuffer.Length; i++)
				toneBuffer[i] = lastTone;
		}

		int currTone = 0;
		int samplePosition = 0;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			// process tone buffer			
			for (int t = 0; t < toneBuffer.Length; t++)
			{
				var tValue = toneBuffer[t];

				if (currTone != tValue)
				{
					// tone is changing
					if (tValue == 0)
					{
						// immediate silence
						currTone = tValue;
						sampleBuffer[t] = 0;
					}
					else
					{
						// tone change
						amplitude = 1;
						samplePosition = 0;
						rampCounter = rampUpTime;
						currTone = tValue;

						if (rampCounter <= 0)
							sampleBuffer[t] = (short)((GetWaveSample(samplePosition++, currTone) * amplitude) / 30);						
					}
				}
				else if (currTone > 0)
				{
					// tone is continuing
					if (rampCounter <= 0)
					{
						amplitude *= decay;
						samplePosition %= samplesPerFrame;
						sampleBuffer[t] = (short)((GetWaveSample(samplePosition++, currTone) * amplitude) / 30);
					}
					else
					{
						rampCounter--;
					}
				}
				else
				{
					// explicit silence
					sampleBuffer[t] = 0;
				}
			}

			ApplyBassBoostFilter(sampleBuffer, 4.0, 600);
			ApplyLowPassFilter(sampleBuffer, 10000);

			nsamp = sampleBuffer.Length;
			samples = new short[nsamp * 2];

			for (int i = 0, s = 0; i < nsamp; i++, s += 2)
			{
				samples[s] = (short)(sampleBuffer[i] * 10);
				samples[s + 1] = (short)(sampleBuffer[i] * 10);
			}

			DiscardSamples();
		}
	}
}
