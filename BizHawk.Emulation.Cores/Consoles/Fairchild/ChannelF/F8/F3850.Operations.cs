using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// ALU Operations
	/// </summary>
	public sealed partial class F3850
	{
		public void Read_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
		}

		public void Write_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
		}

		public void IN_Func(ushort dest, ushort src)
		{
			Regs[dest] = ReadHardware(Regs[src]);
		}

		public void LR_A_IO_Func(ushort dest, ushort src)
		{
			// helper method that simulates transferring DB to accumulator (as part of an IN operation)
			// this sets flags accordingly
			// dest should always == A and src should always == DB for this function

			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = Regs[src];

			FlagZ = Regs[dest] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[dest].Bit(7) == false;

			// ICB flag not affected
		}

		public void ClearFlags_Func()
		{
			FlagC = false;
			FlagO = false;
			FlagS = false;
			FlagZ = false;
		}

		public void LR_Func(ushort dest, ushort src)
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
			else if (dest == ISAR)
			{
				// mask for ISAR register
				Regs[dest] = (ushort)(Regs[src] & 0x3F);
			}
			else
			{
				Regs[dest] = Regs[src];
			}
		}

		/// <summary>
		/// Right shift 'src' 'shift' positions (zero fill)
		/// </summary>
		/// <param name="src"></param>
		/// <param name="shift"></param>
		public void SR_Func(ushort src, ushort shift)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[src] = (ushort)((Regs[src] >> shift) & 0xFF);

			FlagZ = Regs[src] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[src].Bit(7) == false;

			// ICB flag not affected
		}

		/// <summary>
		/// Left shit 'src' 'shift' positions (zero fill)
		/// </summary>
		/// <param name="src"></param>
		/// <param name="shift"></param>
		public void SL_Func(ushort src, ushort shift)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[src] = (ushort)((Regs[src] << shift) & 0xFF);

			FlagZ = Regs[src] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[src].Bit(7) == false;

			// ICB flag not affected
		}

		public void ADD_Func(ushort dest, ushort src)
		{
			// addition of 2 signed bytes
			ushort dest16 = Regs[dest];

			dest16 += Regs[src];

			FlagC = dest16.Bit(8);
			FlagZ = (dest16 & 0xFF) == 0;

			ushort ans = (ushort)(dest16 & 0xFF);

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = ans.Bit(7) == false;

			// overflow based on carry out of bit6 XOR carry out of bit7
			var b6c = dest16 >> 7;
			var b7c = dest16 >> 8;
			FlagO = (b6c ^ b7c) != 0;

			Regs[dest] = ans;
		}

		public void SUB_Func(ushort dest, ushort src)
		{
			Regs[ALU0] = (ushort)((Regs[src] ^ 0xff) + 1);
			ADD_Func(dest, ALU0);
		}

		public void ADDD_Func(ushort dest, ushort src)
		{
			var d = Regs[dest];
			var s = Regs[src];
			var bcdRes = d + s;

			var carryIntermediate = ((d & 0x0F) + (s & 0x0F)) > 0x0F;
			var carryUpper = bcdRes >= 0x100;

			// temporary storage and set flags
			Regs[ALU0] = Regs[dest];
			Regs[ALU1] = Regs[src];
			ADD_Func(ALU0, ALU1);

			if (!carryIntermediate)
			{
				bcdRes = (bcdRes & 0xF0) | ((bcdRes + 0x0A) & 0x0F);
			}

			if (!carryUpper)
			{
				bcdRes = (bcdRes + 0xA0);
			}

			Regs[dest] = (ushort)(bcdRes & 0xFF);
		}
		
		public void CI_Func()
		{
			// compare immediate
			// we need to achieve DB - A + 1
			// flags set - results not stored
			var comp = ((Regs[A] ^ 0xFF) + 1);
			Regs[ALU0] = (ushort)comp;
			ADD_Func(DB, ALU0);
		}

		public void ADDD_Func_(ushort dest, ushort src)
		{
			// from MAME f8.cpp (BSD-3)
			// https://github.com/mamedev/mame/blob/97b67170277437131adf6ed4d60139c172529e4f/src/devices/cpu/f8/f8.cpp#L264
			byte d = (byte)Regs[dest];
			byte s = (byte)Regs[src];
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

			//ALU_ClearFlags();
			ALU_ADD8_FLAGSONLY_Func(dest, src);
			Regs[ALU0] = tmp;
			//ALU_SetFlags_SZ(ALU0);

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

		public void AND_Func(ushort dest, ushort src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = Regs[src] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[src].Bit(7) == false;

			// ICB flag not affected
		}

		public void OR_Func(ushort dest, ushort src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);

			FlagZ = Regs[src] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[src].Bit(7) == false;

			// ICB flag not affected
		}

		public void XOR_Func(ushort dest, ushort src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);

			FlagZ = Regs[src] == 0;

			// Sign SET if MSB == 0 (positive signed number)
			FlagS = Regs[src].Bit(7) == false;

			// ICB flag not affected
		}
	}
}
