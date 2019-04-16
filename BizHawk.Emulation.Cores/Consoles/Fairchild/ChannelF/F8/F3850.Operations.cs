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

		public void LoadReg_Func(ushort dest, ushort src)
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

		public void ShiftRight_Func(ushort src, ushort index)
		{
			int shft = (Regs[src] >> index) & 0xFF;
			FlagO = false;
			FlagC = false;
			FlagZ = shft == 0;
			FlagS = (~shft & 0x80) != 0;
			Regs[src] = (ushort)shft;
		}

		public void ShiftLeft_Func(ushort src, ushort index)
		{
			int shft = (Regs[src] << index) & 0xFF;
			FlagO = false;
			FlagC = false;
			FlagZ = shft == 0;
			FlagS = (~shft & 0x80) != 0;
			Regs[src] = (ushort)shft;
		}

		public void COM_Func(ushort src)
		{
			byte b = (byte)Regs[src];
			var r = (byte)~b;
			FlagO = false;
			FlagC = false;
			FlagZ = r == 0;
			FlagS = (~r & 0x80) != 0;
			Regs[src] = (ushort)r;
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

		public void Read_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}
	}
}
