using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISoundProvider
	{
		private readonly YM2413 YM2413;
		internal BlipBuffer BlipL { get; set; } = new BlipBuffer(4096);
		internal BlipBuffer BlipR { get; set; } = new BlipBuffer(4096);

		internal uint SampleClock;
		internal int OldSl;
		internal int OldSr;

		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new NotSupportedException("Only sync mode is supported");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (!disablePSG)
			{
				BlipL.EndFrame(SampleClock);
				BlipR.EndFrame(SampleClock);

				nsamp = Math.Max(Math.Max(BlipL.SamplesAvailable(), BlipR.SamplesAvailable()), 1);
				samples = new short[nsamp * 2];

				BlipL.ReadSamplesLeft(samples, nsamp);
				BlipR.ReadSamplesRight(samples, nsamp);

				ApplyYmAudio(samples);
			}
			else
			{
				nsamp = 735;
				samples = new short[nsamp * 2];
				ApplyYmAudio(samples);
			}

			SampleClock = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async not supported");
		}

		public void DiscardSamples()
		{
			BlipL.Clear();
			BlipR.Clear();
			SampleClock = 0;
		}

		private void ApplyYmAudio(short[] samples)
		{
			if (HasYM2413)
			{
				short[] fmSamples = new short[samples.Length];
				YM2413.GetSamples(fmSamples);

				// naive mixing. need to study more
				int len = samples.Length;
				for (int i = 0; i < len; i++)
				{
					short fmSample = (short)(fmSamples[i] << 1);
					samples[i] = (short)(samples[i] + fmSample);
				}
			}
		}
	}
}
