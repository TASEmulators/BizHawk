using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Sound related functions
	/// </summary>
	public partial class ChannelF : ISoundProvider
	{
		private const double SAMPLE_RATE = 44100;
		private const double DECAY = 0.998;
		private const int RAMP_UP_TIME = 1;

		private int _tone;
		private short[] _sampleBuffer;
		private double[] _filteredSampleBuffer;
		private int[] _toneBuffer;
		private int _samplesPerFrame;
		private double _cyclesPerSample;
		private double _amplitude;
		private int _rampCounter;

		private int _currTone;
		private int _samplePosition;

		private short[] _finalSampleBuffer = [ ];

		private void SetupAudio()
		{
			_samplesPerFrame = (int)(SAMPLE_RATE * VsyncDenominator / VsyncNumerator);
			// TODO: more precise audio clocking
			_cyclesPerSample = _cpuClocksPerFrame / (double)_samplesPerFrame;
			_sampleBuffer = new short[_samplesPerFrame];
			_filteredSampleBuffer = new double[_samplesPerFrame];
			_toneBuffer = new int[_samplesPerFrame];
		}

		private void AudioChange()
		{
			var currSample = (int)(_frameClock / _cyclesPerSample);
			while (currSample < _samplesPerFrame)
			{
				_toneBuffer[currSample++] = _tone;
			}
		}

		private static int GetWaveSample(double time, int tone)
		{
			var t = 0;
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
#if false
		private static int GetSineWaveSample(double time, double freq)
		{
			var sTime = time / SAMPLE_RATE;
			var sAngle = 2.0 * Math.PI * sTime * freq;
			return (int)(Math.Sin(sAngle) * 0x8000);
		}
#endif
		private static int GetSquareWaveSample(double time, double freq)
		{
			var sTime = time / SAMPLE_RATE;
			var period = 1.0 / freq;
			var halfPeriod = period / 2.0;
			var currentTimeInPeriod = sTime % period;
			return currentTimeInPeriod < halfPeriod ? 32766 : -32766;
		}

		private static void ApplyLowPassFilter(short[] samples, double cutoffFrequency)
		{
			if (samples == null || samples.Length == 0)
				return;

			var rc = 1.0 / (cutoffFrequency * 2 * Math.PI);
			const double dt = 1.0 / SAMPLE_RATE;
			var alpha = dt / (rc + dt);

			var previousSample = samples[0];
			for (var i = 1; i < samples.Length; i++)
			{
				samples[i] = (short)(previousSample + alpha * (samples[i] - previousSample));
				previousSample = samples[i];
			}
		}

		private static void ApplyBassBoostFilter(short[] samples, double[] filteredSamples, double boostAmount, double cutoffFrequency)
		{
			if (samples == null || samples.Length == 0)
				return;

			var A = Math.Pow(10, boostAmount / 40);
			var omega = 2 * Math.PI * cutoffFrequency / SAMPLE_RATE;
			var sn = Math.Sin(omega);
			var cs = Math.Cos(omega);
			var alpha = sn / 2 * Math.Sqrt((A + 1 / A) * (1 / 0.707 - 1) + 2);
			var beta = 2 * Math.Sqrt(A) * alpha;

			var b0 = A * ((A + 1) - (A - 1) * cs + beta);
			var b1 = 2 * A * ((A - 1) - (A + 1) * cs);
			var b2 = A * ((A + 1) - (A - 1) * cs - beta);
			var a0 = (A + 1) + (A - 1) * cs + beta;
			var a1 = -2 * ((A - 1) + (A + 1) * cs);
			var a2 = (A + 1) + (A - 1) * cs - beta;

			filteredSamples[0] = samples[0];
			filteredSamples[1] = samples[1];

			for (var i = 2; i < samples.Length; i++)
			{
				filteredSamples[i] = (b0 / a0) * samples[i] + (b1 / a0) * samples[i - 1] + (b2 / a0) * samples[i - 2]
					- (a1 / a0) * filteredSamples[i - 1] - (a2 / a0) * filteredSamples[i - 2];
			}

			for (var i = 0; i < samples.Length; i++)
			{
				samples[i] = (short)Math.Min(Math.Max(filteredSamples[i], short.MinValue), short.MaxValue);
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
			=> throw new NotSupportedException("Async is not available");

		public void DiscardSamples()
		{
			Array.Clear(_sampleBuffer, 0, _sampleBuffer.Length);

			// pre-populate the tonebuffer with the last active tone from the frame (will be overwritten if and when a tone change happens)
			var lastTone = _toneBuffer[^1];
			for (var i = 0; i < _toneBuffer.Length; i++)
				_toneBuffer[i] = lastTone;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			// process tone buffer
			for (var t = 0; t < _toneBuffer.Length; t++)
			{
				var tValue = _toneBuffer[t];

				if (_currTone != tValue)
				{
					// tone is changing
					if (tValue == 0)
					{
						// immediate silence
						_currTone = tValue;
						_sampleBuffer[t] = 0;
					}
					else
					{
						// tone change
						_amplitude = 1;
						_samplePosition = 0;
						_rampCounter = RAMP_UP_TIME;
						_currTone = tValue;

						if (_rampCounter <= 0)
							_sampleBuffer[t] = (short)((GetWaveSample(_samplePosition++, _currTone) * _amplitude) / 30);
					}
				}
				else if (_currTone > 0)
				{
					// tone is continuing
					if (_rampCounter <= 0)
					{
						_amplitude *= DECAY;
						_samplePosition %= _samplesPerFrame;
						_sampleBuffer[t] = (short)((GetWaveSample(_samplePosition++, _currTone) * _amplitude) / 30);
					}
					else
					{
						_rampCounter--;
					}
				}
				else
				{
					// explicit silence
					_sampleBuffer[t] = 0;
				}
			}

			ApplyBassBoostFilter(_sampleBuffer, _filteredSampleBuffer, 4.0, 600);
			ApplyLowPassFilter(_sampleBuffer, 10000);

			nsamp = _sampleBuffer.Length;
			if (_finalSampleBuffer.Length < nsamp * 2)
			{
				_finalSampleBuffer = new short[nsamp * 2];
			}
			samples = _finalSampleBuffer;

			for (int i = 0, s = 0; i < nsamp; i++, s += 2)
			{
				samples[s] = (short)(_sampleBuffer[i] * 10);
				samples[s + 1] = (short)(_sampleBuffer[i] * 10);
			}

			DiscardSamples();
		}
	}
}
