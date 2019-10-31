using System;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public partial class I8048
	{
		public void Read_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null)
			{
				if (src == PC) CDLCallback(Regs[src], eCDLogMemFlags.FetchOperand);
				else CDLCallback(Regs[src], eCDLogMemFlags.Data);
			}
			Regs[dest] = ReadMemory(Regs[src]);
		}

		public void Read_Inc_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null)
			{
				if (src == PC) CDLCallback(Regs[src], eCDLogMemFlags.FetchOperand);
				else CDLCallback(Regs[src], eCDLogMemFlags.Data);
			}
			//Console.WriteLine(dest + " " + src + " " + opcode_see);

			Regs[dest] = ReadMemory(Regs[src]);

			Regs[src]++;
		}

		public void Write_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)Regs[src]);
		}

		public void TR_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void LD_8_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void TST_Func(ushort src)
		{

		}

		public void CLR_Func(ushort src)
		{
			Regs[src] = 0;

			FlagC = false;
		}

		// source is considered a 16 bit signed value, used for long relative branch
		// no flags used
		public void ADD16BR_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] + (short)Regs[src]);
		}

		public void ADD8BR_Func(ushort dest, ushort src)
		{
			if (Regs[src] > 127) { Regs[src] |= 0xFF00; }
			Regs[dest] = (ushort)(Regs[dest] + (short)Regs[src]);
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			Regs[dest] = ans;
		}

		public void SUB8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			Regs[dest] = ans;
		}

		// same as SUB8 but result not stored
		public void CMP8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);
		}

		public void BIT_Func(ushort dest, ushort src)
		{
			ushort ans = (ushort)(Regs[dest] & Regs[src]);
		}

		public void ASL_Func(ushort src)
		{
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)((Regs[src] << 1) & 0xFF);
		}

		public void ASR_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			ushort temp = (ushort)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (ushort)((Regs[src] >> 1) | temp);
		}

		public void LSR_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(Regs[src] >> 1);
		}

		public void COM_Func(ushort src)
		{
			Regs[src] = (ushort)((~Regs[src]) & 0xFF);

			FlagC = true;
		}

		public void AND8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);
		}

		public void ROR_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 0x80 : 0);

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | (Regs[src] >> 1));
		}

		public void ROL_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 1 : 0);
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);
		}

		public void RRC_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 0x80 : 0);

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | (Regs[src] >> 1));
		}

		public void RLC_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 1 : 0);
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);
		}

		public void INC8_Func(ushort src)
		{
			Regs[src] = (ushort)((Regs[src] + 1) & 0xFF);
		}

		public void DEC8_Func(ushort src)
		{
			Regs[src] = (ushort)((Regs[src] - 1) & 0xFF);
		}

		public void INC16_Func(ushort src)
		{
			Regs[src] += 1;
		}

		public void DEC16_Func(ushort src)
		{
			Regs[src] -= 1;
		}

		public void ADC8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int c = FlagC ? 1 : 0;

			Reg16_d += (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += ((Regs[src] & 0xF) + c);

			Regs[dest] = ans;
		}

		public void DA_Func(ushort src)
		{
			int a = Regs[src];

			byte CF = 0;
			if (FlagC || ((a & 0xF) > 9))
			{
				CF = 6;
			}
			if (FlagC || (((a >> 4) & 0xF) > 9) || ((((a >> 4) & 0xF) > 8) && ((a & 0xF) > 9)))
			{
				CF |= (byte)(6 << 4);
			}

			a += CF;

			if ((a > 0xFF) || FlagC)
			{
				FlagC = true;
			}
			else
			{
				FlagC = false;
			}
			Regs[src] = (byte)a;
		}

		public void CMP16_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int Reg16_s = Regs[src];

			Reg16_d -= Reg16_s;

			FlagC = Reg16_d.Bit(16);

			ushort ans = (ushort)(Reg16_d & 0xFFFF);
		}

		public void EXG_Func(ushort sel)
		{

		}

		public void TFR_Func(ushort sel)
		{

		}
	}
}
