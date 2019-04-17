using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public sealed partial class F3850
	{
		/*
		private void IncrementBySignedByte(ushort dest, byte src)
		{
			if (src >= 0x80)
			{
				dest -= (ushort)(src & 0x80);
			}
			else
			{
				dest += (ushort)(src & 0x80);
			}
		}

		private void IncrementBySignedByte(byte dest, byte src)
		{
			if (src >= 0x80)
			{
				dest -= (byte)(src & 0x80);
			}
			else
			{
				dest += (byte)(src & 0x80);
			}
		}
		*/

		public void LR8_Func(ushort dest, ushort src)
		{
			if (dest == DB)
			{
				// byte storage
				Regs[dest] = (ushort)(Regs[src] & 0xFF);
			}
			else if (dest == W)
			{
				// mask for status register
				Regs[dest] = (ushort)(Regs[src] & 0x1F);
			}
			else
			{
				Regs[dest] = Regs[src];
			}
		}

		public void LR8_IO_Func(ushort dest, ushort src)
		{
			if (dest == DB)
			{
				// byte storage
				Regs[dest] = (ushort)(Regs[src] & 0xFF);
			}
			else if (dest == W)
			{
				// mask for status register
				Regs[dest] = (ushort)(Regs[src] & 0x1F);
			}
			else
			{
				Regs[dest] = Regs[src];
			}

			// update flags
			FlagO = false;
			FlagC = false;
			FlagZ = (Regs[dest] & 0xFF) == 0;
			FlagS = Regs[dest] > 127;
		}

		public void SR_Func(ushort src, ushort index)
		{
			int shft = (Regs[src] >> index) & 0xFF;
			FlagO = false;
			FlagC = false;
			FlagZ = shft == 0;
			FlagS = (~shft & 0x80) != 0;
			Regs[src] = (ushort)shft;
		}

		public void SL_Func(ushort src, ushort index)
		{
			int shft = (Regs[src] << index) & 0xFF;
			FlagO = false;
			FlagC = false;
			FlagZ = shft == 0;
			FlagS = (~shft & 0x80) != 0;
			Regs[src] = (ushort)shft;
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			FlagO = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			Regs[dest] = ans;
		}

		public void ADDS_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l];
			int Reg16_s = Regs[src_l];

			Reg16_d += Reg16_s;

			ushort temp = 0;

			// since this is signed addition, calculate the high byte carry appropriately
			// note that flags are unaffected by this operation
			if (Reg16_s.Bit(7))
			{
				if (((Reg16_d & 0xFF) >= Regs[dest_l]))
				{
					temp = 0xFF;
				}
				else
				{
					temp = 0;
				}
			}
			else
			{
				temp = (ushort)(Reg16_d.Bit(8) ? 1 : 0);
			}

			ushort ans_l = (ushort)(Reg16_d & 0xFF);

			Regs[dest_l] = ans_l;
			Regs[dest_h] += temp;
			Regs[dest_h] &= 0xFF;
		}

		public void ADD8D_Func(ushort dest, ushort src)
		{
			// from MAME f8.cpp (BSD-3)
			// https://github.com/mamedev/mame/blob/97b67170277437131adf6ed4d60139c172529e4f/src/devices/cpu/f8/f8.cpp#L264
			byte d = (byte) Regs[dest];
			byte s = (byte) Regs[src];
			byte tmp = (byte)(d + s);

			byte c = 0; // high order carry
			byte ic = 0; // low order carry

			if (((d + s) & 0xFF0) > 0xF0)
			{
				c = 1;
			}

			if ((d & 0x0F) + (s & 0x0F) > 0x0F)
			{
				ic = 1;
			}

			// binary addition performed and flags set accordingly
			int Reg16_d = Regs[dest];
			Reg16_d += Regs[src];
			ushort ans = (ushort)(Reg16_d & 0xFF);

			FlagC = tmp.Bit(8);
			FlagZ = (tmp & 0xFF) == 0;

			FlagO = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			if (c == 0 && ic == 0)
			{
				tmp = (byte)(((tmp + 0xa0) & 0xf0) + ((tmp + 0x0a) & 0x0f));
			}

			if (c == 0 && ic == 1)
			{
				tmp = (byte)(((tmp + 0xa0) & 0xf0) + (tmp & 0x0f));
			}

			if (c == 1 && ic == 0)
			{
				tmp = (byte)((tmp & 0xf0) + ((tmp + 0x0a) & 0x0f));
			}

			Regs[dest] = tmp;
		}

		public void SUB8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			FlagO = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;
		}

		public void INC8_Func(ushort src)
		{
			int Reg16_d = Regs[src];
			Reg16_d += 1;

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			Regs[src] = ans;

			FlagS = Regs[src].Bit(7);
			FlagO = Regs[src] == 0x80;
		}

		public void AND8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagO = false;
			FlagS = Regs[dest] > 127;
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);
			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagO = false;
			FlagS = Regs[dest] > 127;
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);
			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagO = false;
			FlagS = Regs[dest] > 127;
		}

		public void XOR8C_Func(ushort dest, ushort src)
		{
			// TODO
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);
			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagO = false;
			FlagS = Regs[dest] > 127;
		}

		/*
		 *
		 * public void COM_Func(ushort src)
		{
			byte b = (byte)Regs[src];
			var r = (byte)~b;
			FlagO = false;
			FlagC = false;
			FlagZ = r == 0;
			FlagS = (~r & 0x80) != 0;
			Regs[src] = (ushort)r;
		}
		 */

		public void IN_Func(ushort dest, ushort src)
		{
			Regs[dest] = ReadHardware(Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagO = false;
			FlagC = false;
			FlagS = Regs[dest].Bit(7);
		}

		public void OUT_Func(ushort dest, ushort src)
		{
			WriteHardware(Regs[dest], (byte) Regs[src]);
		}


		public void Read_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}
	}
}
