using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : ISoundProvider
	{
		private short[] soundbuff = new short[2048];
		private int numsamp;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = soundbuff;
			nsamp = numsamp;
		}

		public void DiscardSamples()
		{
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}
	}
}
