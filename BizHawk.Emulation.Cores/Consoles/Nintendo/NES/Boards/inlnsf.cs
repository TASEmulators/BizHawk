using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class INLNSF : NES.NESBoardBase
	{

		// config
		int prg_bank_mask_4k;

		// state
		int[] prg = new int[8];

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg, true);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER031":
					AssertChr(0, 8);
					if(Cart.chr_size == 0)
						Cart.vram_size = 8;
					break;
				case "MAPPER0031-00":
					AssertVram(8);
					break;
				default:
					return false;
			}
			SetMirrorType(CalculateMirrorType(Cart.pad_h, Cart.pad_v));
			AssertPrg(16, 32, 64, 128, 256, 512, 1024);
			Cart.wram_size = 0;
			prg_bank_mask_4k = Cart.prg_size / 4 - 1;
			prg[7] = prg_bank_mask_4k;
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1000)
				prg[addr & 0x07] = value & prg_bank_mask_4k;
			else
				base.WriteEXP(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[prg[(addr & 0x7000) >> 12] << 12 | addr & 0x0fff];
		}
	}
}
