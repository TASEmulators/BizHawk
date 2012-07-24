using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper246 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 246          =
		========================


		Example Game:
		--------------------------
		Fong Shen Bang - Zhu Lu Zhi Zhan


		Notes:
		--------------------------

		Regs lie at $6000-67FF, but SRAM exists at $6800-7FFF.

		Don't know if there's only 6k of SRAM, or if there's 8k, but the first 2k is inaccessable.  I find the latter
		more likely.


		Registers:
		---------------------------

		Range,Mask:   $6000-67FF, $6007


		$6000-6003:  PRG Regs
		$6004-6007:  CHR Regs


		CHR Setup:
		---------------------------

		$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
		+---------------+---------------+---------------+---------------+
		|     $6004     |     $6005     |     $6006     |     $6007     |
		+---------------+---------------+---------------+---------------+


		PRG Setup:
		---------------------------

		$8000   $A000   $C000   $E000  
		+-------+-------+-------+-------+
		| $6000 | $6001 | $6002 | $6003 |
		+-------+-------+-------+-------+


		Powerup/Reset:
		---------------------------
		$6003 set to $FF on powerup (and probably reset, but not sure).
		*/

		int prg_bank_mask_8k;
		ByteBuffer prg_banks_8k = new ByteBuffer(4);

		int chr_bank_mask_2k;
		ByteBuffer chr_banks_2k = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER246":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			chr_bank_mask_2k = (Cart.chr_size / 2) - 1;
			prg_banks_8k[3] = 0xFF;
			SetMirrorType(EMirrorType.Horizontal);
			SyncMap();
			return true;
		}

		void SyncMap()
		{
			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);
			ApplyMemoryMapMask(chr_bank_mask_2k, chr_banks_2k);
		}

		public override void Dispose()
		{
			prg_banks_8k.Dispose();
			chr_banks_2k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_2k", ref chr_banks_2k);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x0800)
			{
				addr &= 0x0007;
				switch (addr)
				{
					case 0:
						prg_banks_8k[0] = value;
						break;
					case 1:
						prg_banks_8k[1] = value;
						break;
					case 2:
						prg_banks_8k[2] = value;
						break;
					case 3:
						prg_banks_8k[3] = value;
						break;
					case 4:
						chr_banks_2k[0] = value;
						break;
					case 5:
						chr_banks_2k[1] = value;
						break;
					case 6:
						chr_banks_2k[2] = value;
						break;
					case 7:
						chr_banks_2k[3] = value;
						break;
				}
				SyncMap();
			}
			else
			{
				base.WriteWRAM(addr, value);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(11, chr_banks_2k, addr);
				return base.ReadPPUChr(addr);
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override byte ReadPRG(int addr)
		{
			addr = ApplyMemoryMap(13, prg_banks_8k, addr);
			return ROM[addr];
		}
	}
}
