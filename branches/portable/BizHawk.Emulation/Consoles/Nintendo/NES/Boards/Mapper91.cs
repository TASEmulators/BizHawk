using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper91 : NES.NESBoardBase
	{
		/*
		*  Here are Disch's original notes:  
		========================
		=  Mapper 091          =
		========================
 
 
		Example Game:
		--------------------------
		Street Fighter 3
 
 
		Notes:
		---------------------------
		Regs exist at $6000-7FFF, so this mapper has no SRAM.
 
 
		Registers:
		---------------------------
 
		Range,Mask:   $6000-7FFF, $7003
 
		$6000-6003:  CHR Regs
		$7000-7001:  [.... PPPP]  PRG Regs
 
		$7002 [.... ....]  IRQ Stop
		$7003 [.... ....]  IRQ Start
 
 
 
		CHR Setup:
		---------------------------
 
		  $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
		+---------------+---------------+---------------+---------------+
		|     $6000     |     $6001     |     $6002     |     $6003     |
		+---------------+---------------+---------------+---------------+
 
		PRG Setup:
		---------------------------
 
		  $8000   $A000   $C000   $E000  
		+-------+-------+-------+-------+
		| $7000 | $7001 | { -2} | { -1} |
		+-------+-------+-------+-------+
 
 
		IRQs:
		---------------------------
 
		IRQs on this mapper seem to behave exactly like MMC3 -- except it's fixed so that it will only fire after 8
		scanlines.  This is easily emulatable by using MMC3 logic.
 
		Write to $7002/$7003 can translate directly to write(s) to the following MMC3 registers:
 
		on $7002 write:
		a) write to $E000
 
		on $7003 write:
		a) write $07 to $C000
		b) write to $C001
		c) write to $E001
 
 
		For details on MMC3 IRQ operation, see mapper 004
		*/

		ByteBuffer chr_regs_2k = new ByteBuffer(4);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);
		int chr_bank_mask_2k, prg_bank_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER091":
				case "MAPPER197": //GoodNES reports 197 instead of 91
					break;
				default:
					return false;
			}

			chr_bank_mask_2k = Cart.chr_size / 2 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;

			prg_regs_8k[3] = 0xFF;
			prg_regs_8k[2] = 0xFE;
			
			return true;
		}

		public override void Dispose()
		{
			prg_regs_8k.Dispose();
			chr_regs_2k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_regs", ref prg_regs_8k);
			ser.Sync("chr_regs", ref chr_regs_2k);
			base.SyncState(ser);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				switch (addr)
				{
					case 0x0000:
						chr_regs_2k[0] = value;
						break;
					case 0x0001:
						chr_regs_2k[1] = value;
						break;
					case 0x0002:
						chr_regs_2k[2] = value;
						break;
					case 0x0003:
						chr_regs_2k[3] = value;
						break;
					case 0x1000:
						prg_regs_8k[0] = (byte)(value & 0x0F);
						break;
					case 0x1001:
						prg_regs_8k[1] = (byte)(value & 0x0F);
						break;
				}
			}
			base.WritePPU(addr, value);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_2k = (addr >> 11) - 1;
				bank_2k = chr_regs_2k[bank_2k];
				bank_2k &= chr_bank_mask_2k;
				return VROM[(bank_2k * 0x800) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			return ROM[(bank_8k * 0x2000) + (addr & 0x1FFF)];
		}
	}
}
