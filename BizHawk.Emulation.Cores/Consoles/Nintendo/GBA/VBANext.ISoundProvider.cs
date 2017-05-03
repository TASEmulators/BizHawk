using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : ISoundProvider
	{
		private readonly short[] _soundbuff = new short[2048];
		private int _numsamp;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundbuff;
			nsamp = _numsamp;
		}

		public void DiscardSamples()
		{
		}

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}
	}
}
