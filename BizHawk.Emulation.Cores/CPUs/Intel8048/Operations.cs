using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.I8048
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

		public void Write_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)Regs[src]);
		}

		public void TR_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for aux carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			FlagAC = Reg16_d.Bit(4);

			Regs[dest] = ans;
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
			ushort c = (ushort)((Regs[src] & 1) << 7);

			Regs[src] = (ushort)(c | ((Regs[src] >> 1) & 0x7F));
		}

		public void ROL_Func(ushort src)
		{
			ushort c = (ushort)((Regs[src] >> 7) & 1);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);
		}

		public void RRC_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 0x80 : 0);

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | ((Regs[src] >> 1) & 0x7F));
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

		public void ADC8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int c = FlagC ? 1 : 0;

			Reg16_d += (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);

			ushort ans = (ushort)(Reg16_d & 0xFF);

			Regs[dest] = ans;
		}

		public void DA_Func(ushort src)
		{
			int a = Regs[src];

			if (((a & 0xF) > 9) | FlagAC)
			{
				a += 0x6;
			}

			if (a > 0xFF) { FlagC = true; }

			if ((((a >> 4) & 0xF) > 9) | FlagC)
			{
				a += 0x60;
			}

			// FlagAC is not reset, nor is FlagC reset
			if (a > 0xFF) { FlagC = true; }

			Regs[src] = (byte)a;
		}
	}
}
