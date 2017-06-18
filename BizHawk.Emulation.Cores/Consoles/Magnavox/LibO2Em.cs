using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Magnavox
{
	public abstract class LibO2Em : LibWaterboxCore
	{
		[BizImport(CC)]
		public abstract bool Init(byte[] rom, int romlen, byte[] bios, int bioslen);
	}
}
