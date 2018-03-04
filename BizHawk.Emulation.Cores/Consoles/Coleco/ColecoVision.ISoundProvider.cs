using System;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : ISoundProvider
	{
		private SN76489col PSG;
		private AY_3_8910_SGM SGM_sound;

		private short[] _sampleBuffer = new short[0];

		public void DiscardSamples()
		{
			SGM_sound.DiscardSamples();
			PSG.DiscardSamples();
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = 524;

			samples = new short[nsamp * 2];

			for (int i = 0; i < nsamp; i++)
			{
				samples[i * 2] = PSG._sampleBuffer[i];
				samples[i * 2 + 1] = PSG._sampleBuffer[i];
			}

			if (use_SGM)
			{
				for (int i = 0; i < nsamp; i++)
				{
					samples[i * 2] += SGM_sound._sampleBuffer[i];
					samples[i * 2 + 1] += SGM_sound._sampleBuffer[i];
				}
			}

			DiscardSamples();
		}

		public void GetSamples(short[] samples)
		{
			throw new Exception();
		}

	}
}
