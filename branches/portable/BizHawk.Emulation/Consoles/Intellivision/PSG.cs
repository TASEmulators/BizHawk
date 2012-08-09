using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class PSG
	{
		private ushort[] Registers = new ushort[16];

		public ushort? ReadPSG(ushort addr)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
				return Registers[addr - 0x01F0];
			return null;
		}

		public bool WritePSG(ushort addr, ushort value)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
			{
				Registers[addr - 0x01F0] = value;
				return true;
			}
			return false;
		}
	}
}
