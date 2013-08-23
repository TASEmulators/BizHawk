// C# port of C-based 3-band equalizer (C) Neil C / Etanza Systems / 2006

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound
{
    public sealed class Equalizer
    {
        double lowFilter;
        double lowFilterPole0;
        double lowFilterPole1;
        double lowFilterPole2;
        double lowFilterPole3;

        double highFilter;
        double highFilterPole0;
        double highFilterPole1;
        double highFilterPole2;
        double highFilterPole3;

        double sampleDataMinus1;
        double sampleDataMinus2;
        double sampleDataMinus3;

        double lowGain;
        double midGain;
        double highGain;
        
        const double sampleRate = 44100.0;
        const double verySmallAmount = (1.0 / 4294967295.0);

        double lowfreq;
        public double LowFreqCutoff 
        {
            get { return lowfreq; }
            set
            {
                lowfreq = value;
                lowFilter = 2 * Math.Sin(Math.PI * (lowfreq / sampleRate));
            }
        }

        double highfreq;
        public double HighFreqCutoff
        {
            get { return highfreq; }
            set
            {
                highfreq = value;
                highFilter = 2 * Math.Sin(Math.PI * (highfreq / sampleRate));
            }
        }

        public Equalizer(double lowFreq=880, double highFreq=5000)
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

            return (short) (l + m + h);
        }

        public void Equalize(short[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = EqualizeSample(samples[i]);
        }
    }
}