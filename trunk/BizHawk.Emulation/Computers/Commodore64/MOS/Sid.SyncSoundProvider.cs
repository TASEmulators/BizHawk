using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : ISyncSoundProvider
	{
		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = buffer;
			nsamp = (int)bufferIndex;
			bufferIndex = 0;
		}

		public void DiscardSamples()
		{
			// todo
		}
	}
}
