using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision : ICart
	{
		private ushort[] memory = new ushort[65536];

		public void Parse()
		{
			// Check to see if the header is valid.
			if (Rom[0] != 0xA8 || Rom[1] != (0xFF ^ Rom[2]))
				throw new ArgumentException();
		}

		public ushort ReadMemory(ushort addr, out bool responded)
		{
			responded = false;
			return 0;
		}

		public void WriteMemory(ushort addr, ushort value, out bool responded)
		{
			responded = false;
		}
	}
}
