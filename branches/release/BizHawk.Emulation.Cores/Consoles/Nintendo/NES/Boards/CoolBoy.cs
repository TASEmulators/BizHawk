using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class CoolBoy : MMC3Board_Base
	{
		// eldritch horror pirate multicart
		// 32MB prg rom, no prg ram, no chr rom, 128KB chr ram

		// behavior directly from fceu-mm

		// this could be broken down into more sensibly named variables
		byte[] exp = new byte[4];

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_COOLBOY":
					AssertChr(0);
					break;
				default:
					return false;
			}

			Cart.vram_size = 128;
			Cart.wram_size = 0;

			BaseSetup();

			// normally base mmc3 sets this right, but it has a hardcoded assumption
			// that no chr rom => 8K of chr ram, not 128K.  i'd change the base class,
			// but not sure if that would break something else
			chr_mask = 127;

			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x1000)
			{
				if (!exp[3].Bit(7))
				{
					exp[addr & 3] = value;
					/*
					if (exp[3].Bit(7))
					{
						Console.WriteLine("EXP Write Protect Activated");
					}
					if (exp[3].Bit(4))
					{
						Console.WriteLine("Funky Mode Active");
					}
					*/
				}
			}
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			int mask = 0, shift = 0;
			int baseaddr = exp[0] & 0x07 | (exp[1] & 0x10) >> 1 | (exp[1] & 0x0c) << 2 | (exp[0] & 0x30) << 2;

			switch (exp[0] & 0xc0)
			{
				case 0x00:
					baseaddr >>= 2;
					mask = 0x3f;
					shift = 6;
					break;
				case 0x80:
					baseaddr >>= 1;
					mask = 0x1f;
					shift = 5;
					break;
				case 0xc0:
					shift = 4;
					if (exp[3].Bit(4))
					{
						mask = 1 | exp[1] & 2;
					}
					else
					{
						mask = 0xf;
					}
					break;
				case 0x40:
					// ??
					throw new InvalidOperationException();
			}

			int v = base.Get_PRGBank_8K(addr);

			int ret = baseaddr << shift | v & mask;
			if (exp[3].Bit(4))
			{
				ret |= exp[3] & (0x0e ^ exp[1] & 2);
			}
			return ret;
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			if (exp[3].Bit(4))
			{
				return (exp[2] & 15) << 3 | addr >> 10 & 7;
			}
			else
			{
				return base.Get_CHRBank_1K(addr);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exp", ref exp, false);
		}

		public override void NESSoftReset()
		{
			Array.Clear(exp, 0, 4);
		}
	}
}
