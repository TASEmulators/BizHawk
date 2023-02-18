using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISyncSoundProvider
	{
		public void DiscardSamples()
		{
			_soundoutbuffcontains = 0;
		}

		public void GetSyncSoundSamples(out short[] samples, out int nsamp)
		{
			samples = _soundoutbuff;
			nsamp = _soundoutbuffcontains;
			DiscardSamples();
		}

		private int _soundoutbuffcontains = 0;

		private readonly short[] _soundoutbuff = new short[2048];
	}
}
