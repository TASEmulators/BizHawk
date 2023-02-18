using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ISyncSoundProvider
	{
		private readonly short[] _soundbuff = new short[2048];
		private readonly short[] _dummysoundbuff = new short[2048];
		private int _nsamp;

		public void GetSyncSoundSamples(out short[] samples, out int nsamp)
		{
			nsamp = _nsamp;
			samples = _soundbuff;
			DiscardSamples();
		}

		public void DiscardSamples()
		{
			_nsamp = 0;
		}
	}
}
