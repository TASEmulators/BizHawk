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

		private readonly BlipBuffer _blip = new BlipBuffer(4096);

		public void DiscardSamples()
		{
			_blip.Clear();
			_sampleClock = 0;
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
