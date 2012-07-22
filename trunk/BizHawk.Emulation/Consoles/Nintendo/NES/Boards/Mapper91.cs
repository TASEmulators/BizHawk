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

		ByteBuffer chr_regs = new ByteBuffer(4);
		ByteBuffer prg_regs = new ByteBuffer(4);
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

			prg_regs[3] = 0xFF;
			prg_regs[2] = 0xFE;

			return true;
		}

		public override void Dispose()
		{
			prg_regs.Dispose();
			chr_regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_regs", ref prg_regs);
			ser.Sync("chr_regs", ref chr_regs);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0x1003;
			chr_regs[addr & 0x03] = value;
			
			if (addr.Bit(12))
			{
				prg_regs[addr & 0x01] = (byte)(value & 0x0F);
			}

		}

		public override byte ReadPPU(int addr)
		{
			int reg_num = (addr >> 11) - 1;
			if (addr < 0x2000)
			{
				return VROM[((chr_regs[reg_num] & chr_bank_mask_2k) * 0x800) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			addr += 0x8000;
			int reg_num;
			if (addr < 0xA000)
			{
				reg_num = 0;
			}
			else if (addr < 0xC000)
			{
				reg_num = 1;
			}
			else if (addr < 0xE000)
			{
				reg_num = 2;
			}
			else 
			{
				reg_num = 3;
			}
			
			return ROM[((prg_regs[reg_num] & prg_bank_mask_8k) * 0x2000) + (addr & 0x1FFF)];
		}
	}
}
