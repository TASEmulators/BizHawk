using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper245 : MMC3Board_Base
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 245          =
		========================


		Example Games:
		--------------------------
		Chu Han Zheng Ba - The War Between Chu & Han
		Xing Ji Wu Shi - Super Fighter
		Yin He Shi Dai
		Yong Zhe Dou e Long - Dragon Quest VII (As)
		Dong Fang de Chuan Shuo - The Hyrule Fantasy


		Notes:
		---------------------------
		Another ?Chinese? MMC3 clone.  Very similar to your typical MMC3.  For MMC3 info, see mapper 004.

		Register layout is identical to a typical MMC3.



		CHR Setup:
		---------------------------

		CHR-RAM is not swappable.  When there is no CHR-ROM present, 8k CHR-RAM is fixed.  However the CHR Mode bit
		($8000.7) can still "flip" the left/right pattern tables.

		Example:

							$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
						+-------------------------------+-------------------------------+
		CHR-RAM, Mode 0:  |             { 0 }             |             { 1 }             |
						+-------------------------------+-------------------------------+
		CHR-RAM, Mode 1:  |             { 1 }             |             { 0 }             |
						+---------------------------------------------------------------+
		CHR-ROM:          |                          Typical MMC3                         |
						+---------------------------------------------------------------+


		PRG Setup:
		---------------------------

		PRG Setup is the same as a normal MMC3, although there's a PRG-AND of $3F, and games select a 512k Block with
		bit 1 of R:0.  Pretty simple really:

		R:0:  [.... ..P.]

		'P'    PRG-AND    PRG-OR
		--------------------------
		0       $3F        $00
		1       $3F        $40

 
		R:0 remains the normal MMC3 CHR reg, as well.  Although the game that uses it as a PRG block selector ("DQ7")
		uses CHR-RAM, so it is normally ignored.
		*/
		bool chr_mode;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER245":
					break;
				default:
					return false;
			}
			chr_mode = false;
			BaseSetup();
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= 0x3F;
			bank_8k &= prg_mask;

			int reg0 = ((base.mmc3.chr_regs_1k[0] >> 1) & 0x01);
			if (reg0 == 1)
			{
				addr |= 0x40;
			}
			else
			{
				addr |= 0x00;
			}

			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr == 0)
			{
				chr_mode = value.Bit(7);
			}
			base.WritePRG(addr, value);
		}

		public override byte  ReadPPU(int addr)
		{
			if (chr_mode) //All games seem to have 0 Chr-ROM
			{
				if (addr < 0x1000)
				{
					return VRAM[addr + 0x1000];
				}
				else
				{
					return VRAM[addr - 0x1000];
				}
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}
	}
}
