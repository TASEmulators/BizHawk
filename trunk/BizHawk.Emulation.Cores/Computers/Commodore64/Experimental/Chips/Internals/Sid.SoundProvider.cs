using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Experimental
{
	sealed public partial class Sid
	{
		public ISoundProvider GetSoundProvider()
		{
			return new NullSound();
		}
	}
}
