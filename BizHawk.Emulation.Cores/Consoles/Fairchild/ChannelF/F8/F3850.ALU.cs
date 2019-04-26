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
	/// The arithmetic and logic unit provides all data manipulating logic for the F3850.
	/// It contains logic that operates on a single 8-bit source data work or combines two 8-bit words of source data
	/// to generate a single 8-bit result. Additional information is reported in status flags, where appropriate.
	///
	/// Operations Performed:
	/// * Addition
	/// * Compare
	/// * AND
	/// * OR
	/// * XOR
	/// </summary>
	public sealed partial class F3850
	{
		/// <summary>
		/// Clears all status flags (excluding the ICB flag)
		/// </summary>
		public void ALU_ClearFlags()
		{
			FlagC = false;
			FlagO = false;
			FlagS = false;
			FlagZ = false;
		}

		/// <summary>
		/// Sets the SIGN and ZERO flags based on the supplied byte
		/// </summary>
		/// <param name="val"></param>
		public void ALU_SetFlags_SZ(ushort src)
		{
			FlagZ = (byte)Regs[src] == 0;
			FlagS = (~((byte)Regs[src]) & 0x80) != 0;
		}

		/// <summary>
		/// Performs addition and sets the CARRY and OVERFLOW flags accordingly
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="src"></param>
		/// <param name="carry"></param>
		public void ALU_ADD8_Func(ushort dest, ushort src, bool carry = false)
		{
			byte d = (byte)Regs[dest];
			byte s = (byte)Regs[src];
			byte c = carry ? (byte)1 : (byte)0;
			ushort result = (ushort)(d + s + c);

			FlagC = (result & 0x100) != 0;
			FlagO = ((d ^ result) & (s ^ result) & 0x80) != 0;

			Regs[dest] = (ushort)(result & 0xFF);
		}

		/// <summary>
		/// Performs addition and sets the CARRY and OVERFLOW flags accordingly WITHOUT saving to destination
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="src"></param>
		/// <param name="carry"></param>
		public void ALU_ADD8_FLAGSONLY_Func(ushort dest, ushort src)
		{
			byte d = (byte)Regs[dest];
			byte s = (byte)Regs[src];
			ushort result = (ushort)(d + s);

			FlagC = (result & 0x100) != 0;
			FlagO = ((d ^ result) & (s ^ result) & 0x80) != 0;
		}

		/// <summary>
		/// Performs decimal addition based on the two supplied bytes
		/// (looks like this is only used in the AMD operation)
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="src"></param>
		public void ALU_ADD8D_Func(ushort dest, ushort src)
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

			ALU_ClearFlags();
			ALU_ADD8_FLAGSONLY_Func(dest, src);
			Regs[ALU0] = tmp;
			ALU_SetFlags_SZ(ALU0);

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

		public void ALU_SUB8_Func(ushort dest, ushort src)
		{
			byte d = (byte)Regs[dest];
			byte s = (byte)Regs[src];
			ushort result = (ushort)(d - s);

			FlagC = (result & 0x100) != 0;
			FlagO = ((d ^ result) & (s ^ result) & 0x80) != 0;

			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			FlagO = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			Regs[dest] = ans;
		}

		/*
		public void ALU_SUB8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			FlagO = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			Regs[dest] = ans;
		}
		*/

		/// <summary>
		/// Right shift 'src' 'shift' positions (zero fill)
		/// </summary>
		/// <param name="src"></param>
		/// <param name="shift"></param>
		public void ALU_SR_Func(ushort src, ushort shift)
		{
			Regs[src] = (ushort)((Regs[src] >> shift) & 0xFF);
			ALU_ClearFlags();
			ALU_SetFlags_SZ(src);
		}

		/// <summary>
		/// Left shit 'src' 'shift' positions (zero fill)
		/// </summary>
		/// <param name="src"></param>
		/// <param name="shift"></param>
		public void ALU_SL_Func(ushort src, ushort shift)
		{
			Regs[src] = (ushort)((Regs[src] << shift) & 0xFF);
			ALU_ClearFlags();
			ALU_SetFlags_SZ(src);
		}

		/// <summary>
		/// AND
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="src"></param>
		public void ALU_AND8_Func(ushort dest, ushort src)
		{
			ALU_ClearFlags();
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);
			ALU_SetFlags_SZ(dest);
		}

		public void ALU_OR8_Func(ushort dest, ushort src)
		{
			ALU_ClearFlags();
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);
			ALU_SetFlags_SZ(dest);
		}

		public void ALU_XOR8_Func(ushort dest, ushort src)
		{
			ALU_ClearFlags();
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);
			ALU_SetFlags_SZ(dest);
		}
		/*
		public void ALU_XOR8C_Func(ushort dest, ushort src)
		{
			// TODO
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);
			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagO = false;
			FlagS = Regs[dest] > 127;
		}
		*/

		public void ADDS_FuncX(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
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

		public void Read_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
		}

		public void Write_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
		}

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

		/*
		public void ALU_INC8_Func(ushort src)
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
		*/


	}
}
