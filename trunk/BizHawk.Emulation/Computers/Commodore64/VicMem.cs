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
				case 0x1E:
					// clear after read
					result = this[addr];
					this[addr] = 0x00;
					irqSpriteCollision = false;
					break;
				case 0x1F:
					// clear after read
					result = this[addr];
					this[addr] = 0x00;
					irqDataCollision = false;
					break;
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
				case 0x19:
					// interrupt reg
					if ((val & 0x01) != 0x00)
						irqRaster = false;
					if ((val & 0x02) != 0x00)
						irqDataCollision = false;
					if ((val & 0x04) != 0x00)
						irqSpriteCollision = false;
					if ((val & 0x08) != 0x00)
						irqLightPen = false;
					break;
				case 0x1E:
				case 0x1F:
					// non writeable regs
					break;
				default:
					this[addr] = val;
					break;
			}
		}
	}
}
