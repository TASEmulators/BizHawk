using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ISyncSoundProvider
	{
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_machine.Memory.Speaker.GetSamples(out samples, out nsamp);
		}

		public void DiscardSamples()
		{
			_machine.Memory.Speaker.Clear();
		}
	}
}
