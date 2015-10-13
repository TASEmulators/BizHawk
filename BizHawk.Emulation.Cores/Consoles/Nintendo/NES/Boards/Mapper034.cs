using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper034 : NES.NESBoardBase
	{
		// zombie board that tries to handle both bxrom and ave-nina at once

		//configuration
		int prg_bank_mask_32k, chr_bank_mask_4k;

		//state
		int[] chr = new int[2];
		int prg;


		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER034": // 3-D Battles of World Runner, The (U) [b5].nes
					// TODO: No idea what to assert here
					break;
				default:
					return false;
			}

			Cart.wram_size = 8;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_4k = Cart.chr_size / 4 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			chr[1] = 1;

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return (VROM ?? VRAM)[addr & 0xfff | chr[addr >> 12] << 12];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}

		public override void WritePRG(int addr, byte value)
		{
			prg = value & prg_bank_mask_32k;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1ffd:
					prg = value & prg_bank_mask_32k;
					break;
				case 0x1ffe:
					chr[0] = value & chr_bank_mask_4k;
					break;
				case 0x1fff:
					chr[1] = value & chr_bank_mask_4k;
					break;
				default:
					// on NINA, the regs sit on top of WRAM
					base.WriteWRAM(addr, value);
					break;
			}
		}
	}
}
