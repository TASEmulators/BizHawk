// C# port of C-based 3-band equalizer (C) Neil C / Etanza Systems / 2006

using System;

namespace BizHawk.Emulation.Common
{
	public sealed class Equalizer
	{
		private double lowFilter;
		private double lowFilterPole0;
		private double lowFilterPole1;
		private double lowFilterPole2;
		private double lowFilterPole3;

		private double highFilter;
		private double highFilterPole0;
		private double highFilterPole1;
		private double highFilterPole2;
		private double highFilterPole3;

		private double sampleDataMinus1;
		private double sampleDataMinus2;
		private double sampleDataMinus3;

		private double lowGain;
		private double midGain;
		private double highGain;

		private const double sampleRate = 44100.0;
		private const double verySmallAmount = 1.0 / 4294967295.0;

		private double lowfreq;

		public double LowFreqCutoff
		{
			get { return lowfreq; }
			set
			{
				lowfreq = value;
				lowFilter = 2 * Math.Sin(Math.PI * (lowfreq / sampleRate));
			}
		}

		private double highfreq;
		public double HighFreqCutoff
		{
			get { return highfreq; }
			set
			{
				highfreq = value;
				highFilter = 2 * Math.Sin(Math.PI * (highfreq / sampleRate));
			}
		}

		public Equalizer(double lowFreq = 880, double highFreq = 5000)
		{
			lowGain = 1.3;
			midGain = 0.9;
			highGain = 1.3;
			LowFreqCutoff = lowFreq;
			HighFreqCutoff = highFreq;
		}

		public short EqualizeSample(short sample)
		{
			lowFilterPole0 += (lowFilter * (sample - lowFilterPole0)) + verySmallAmount;
			lowFilterPole1 += lowFilter * (lowFilterPole0 - lowFilterPole1);
			lowFilterPole2 += lowFilter * (lowFilterPole1 - lowFilterPole2);
			lowFilterPole3 += lowFilter * (lowFilterPole2 - lowFilterPole3);
			double l = lowFilterPole3;

			highFilterPole0 += (highFilter * (sample - highFilterPole0)) + verySmallAmount;
			highFilterPole1 += highFilter * (highFilterPole0 - highFilterPole1);
			highFilterPole2 += highFilter * (highFilterPole1 - highFilterPole2);
			highFilterPole3 += highFilter * (highFilterPole2 - highFilterPole3);
			double h = sampleDataMinus3 - highFilterPole3;

			double m = sample - (h + l);
			l *= lowGain;
			m *= midGain;
			h *= highGain;

			sampleDataMinus3 = sampleDataMinus2;
			sampleDataMinus2 = sampleDataMinus1;
			sampleDataMinus1 = sample;

			return (short)(l + m + h);
		}

		public void Equalize(short[] samples)
		{
			for (int i = 0; i < samples.Length; i++)
			{
				samples[i] = EqualizeSample(samples[i]);
			}
		}
	}
}