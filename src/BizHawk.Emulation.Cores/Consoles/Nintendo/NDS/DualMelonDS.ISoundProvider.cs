using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class DualNDS : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _sampleBufferContains;
			samples = SampleBuffer;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_sampleBufferContains = 0;
		}

		private readonly short[] SampleBuffer = new short[1024 * 2];
		private int _sampleBufferContains = 0;

		private unsafe void ProcessSound()
		{
			L.GetSamplesSync(out short[] lsamples, out int lnsamp);
			fixed (short* ls = &lsamples[0], sb = &SampleBuffer[0])
			{
				for (int i = 0; i < lnsamp; i++)
				{
					int lsamp = (lsamples[i * 2] + lsamples[i * 2 + 1]) >> 1;
					SampleBuffer[i * 2] = (short)lsamp;
				}
			}

			R.GetSamplesSync(out short[] rsamples, out int rnsamp);
			fixed (short* rs = &rsamples[0], sb = &SampleBuffer[0])
			{
				for (int i = 0; i < rnsamp; i++)
				{
					int rsamp = (rsamples[i * 2] + rsamples[i * 2 + 1]) >> 1;
					SampleBuffer[i * 2 + 1] = (short)rsamp;
				}
			}

			_sampleBufferContains = Math.Max(lnsamp, rnsamp);
		}
	}
}
