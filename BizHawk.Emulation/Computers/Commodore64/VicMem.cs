using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII
	{
		public byte Peek(int addr)
		{
			addr &= 0x3F;
			return this[(uint)addr];
		}

		public void Poke(int addr, byte val)
		{
			addr &= 0x3F;
			this[(uint)addr] = val;
		}

		public byte Read(ushort addr)
		{
			byte result = 0xFF;

			addr &= 0x3F;
			switch (addr)
			{
				default:
					result = this[addr];
					break;
			}

			return result;
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0x3F;
			switch (addr)
			{
				default:
					this[addr] = val;
					break;
			}
		}
	}
}
