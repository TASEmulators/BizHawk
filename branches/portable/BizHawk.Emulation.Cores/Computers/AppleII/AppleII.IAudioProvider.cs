using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	partial class AppleII : ISyncSoundProvider
	{
		void ISyncSoundProvider.GetSamples(out short[] samples, out int nsamp)
		{
			_machine.Speaker.AudioService.GetSamples(out samples, out nsamp);
		}

		void ISyncSoundProvider.DiscardSamples()
		{
			_machine.Speaker.AudioService.Clear();
		}
	}
}
