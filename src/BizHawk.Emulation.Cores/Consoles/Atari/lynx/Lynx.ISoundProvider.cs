using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx : ISyncSoundProvider
	{
		private readonly short[] _soundBuff = new short[2048];
		private int _numSamp;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuff;
			nsamp = _numSamp;
		}

		public void DiscardSamples()
		{
			// Nothing to do
		}
	}
}
