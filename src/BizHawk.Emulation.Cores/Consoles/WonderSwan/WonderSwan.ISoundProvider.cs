using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	public partial class WonderSwan : ISyncSoundProvider
	{
		private readonly short[] sbuff = new short[1536];
		private int sbuffcontains = 0;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = sbuff;
			nsamp = sbuffcontains;
		}

		public void DiscardSamples()
		{
			sbuffcontains = 0;
		}
	}
}
