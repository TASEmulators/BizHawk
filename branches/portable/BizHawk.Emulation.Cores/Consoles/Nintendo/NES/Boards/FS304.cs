using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class FS304 : NES.NESBoardBase
	{
		// waixing?

		int prg;
		int prg_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-FS304":
					AssertChr(0);
					AssertPrg(512, 1024, 2048, 4096);
					Cart.vram_size = 8;
					Cart.wram_size = 8;
					Cart.wram_battery = true;
					break;
				default:
					return false;
			}

			prg_mask_32k = Cart.prg_size / 32 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr & 0x1300)
			{
				case 0x1000:
					prg &= ~0x0e;
					prg |= value & 0x0e;
					break;
				case 0x1100:
					prg &= ~0x01;
					prg |= value >> 1 & 0x01;
					break;
				case 0x1200:
					prg &= ~0xf0;
					prg |= value << 4 & 0xf0;
					break;
			}
			prg &= prg_mask_32k;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}
	}
}
