using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Audio related functions
	/// </summary>
	public partial class ChannelF : ISoundProvider
	{
		private double SampleRate = 44100;
		private int SamplesPerFrame;
		private double Period;
		private double CyclesPerSample;


		private int tone = 0;

		private double[] tone_freqs = new double[] { 0, 1000, 500, 120 };
		private double amplitude = 0;
		private double decay = 0.998;
		private double time = 0;
		private double cycles = 0;
		private int samplePos = 0;
		private int lastCycle = 0;

		private BlipBuffer _blip;

		private short[] SampleBuffer;

		private void SetupAudio()
		{
			Period = 1.0 / SampleRate;
			SamplesPerFrame = (int)(SampleRate / refreshRate);
			CyclesPerSample = (double)ClockPerFrame / (double)SamplesPerFrame;
			SampleBuffer = new short[SamplesPerFrame];
			_blip = new BlipBuffer(SamplesPerFrame);
			_blip.SetRates(ClockPerFrame * refreshRate, SampleRate);
		}

		private void AudioChange()
		{
			if (tone == 0)
			{
				// silence
			}
			else
			{
				int SamplesPerWave = (int)(SampleRate / tone_freqs[tone]);
				double RadPerSample = (Math.PI * 2) / (double) SamplesPerWave;
				double SinVal = 0;

				int NumSamples = (int)(((double)FrameClock - (double)lastCycle) / CyclesPerSample);

				int startPos = lastCycle;

				for (int i = 0; i < NumSamples; i++)
				{
					SinVal = Math.Sin(RadPerSample * (double) (i * SamplesPerWave));
					_blip.AddDelta((uint)startPos, (int) (Math.Floor(SinVal * 127) + 128) * 1024);
					startPos += (int)CyclesPerSample;
				}
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
			SampleBuffer = new short[SamplesPerFrame];
			samplePos = 0;
			lastCycle = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			AudioChange();

			_blip.EndFrame((uint)ClockPerFrame);
			nsamp = _blip.SamplesAvailable();
			samples = new short[nsamp * 2];
			_blip.ReadSamples(samples, nsamp, true);

			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}
		}
	}
}
