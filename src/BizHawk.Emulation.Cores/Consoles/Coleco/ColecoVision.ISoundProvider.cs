using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : ISyncSoundProvider
	{
		private readonly SN76489col PSG;
		private readonly AY_3_8910_SGM SGM_sound;

		private readonly BlipBuffer _blip = new BlipBuffer(4096);

		public void DiscardSamples()
		{
			_blip.Clear();
			_sampleClock = 0;
		}

		public void GetSyncSoundSamples(out short[] samples, out int nsamp)
		{
			_blip.EndFrame((uint)_sampleClock);
			_sampleClock = 0;

			nsamp = _blip.SamplesAvailable();
			samples = new short[nsamp * 2];

			_blip.ReadSamples(samples, nsamp, true);

			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}
		}

		public void GetSamples(short[] samples)
		{
			throw new Exception();
		}

	}
}
