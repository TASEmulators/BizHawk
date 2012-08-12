using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class BANDAI_74_161_02_74 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 096          =
		========================


		Example Games:
		--------------------------
		Oeka Kids - Anpanman no Hiragana Daisuki
		Oeka Kids - Anpanman to Oekaki Shiyou!!


		Notes:
		---------------------------
		These games use the Oeka Kids tablet -- so you'll need to add support for that if you really want to test
		these.

		These games use 32k of CHR-RAM, which is swappable in a very unique fashion.  Be sure to read the CHR Setup
		section in detail.


		Registers:
		---------------------------
		I'm unsure whether or not this mapper suffers from bus conflicts.  Use caution!


		  $8000-FFFF:  [.... .CPP]
			C = CHR Block select (see CHR Setup)
			P = PRG Page select (32k @ $8000)



		CHR Setup:
		---------------------------

		This mapper is tricky!!!

		Firstly, this mapper divides the 32k CHR-RAM into two 16k blocks (above 'C' bit selects which block is used).
		The selected pages (including the fixed page) are taken from only the currently selected 16k block.

			  $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
			+-------------------------------+-------------------------------+
			|         **See below**         |             { 3 }             |
			+-------------------------------+-------------------------------+


		But that's the easy part.  This mapper does a very, very cool trick which watches the PPU address lines to
		effectively "split" the nametable into 4 smaller sections -- thereby assigning a different CHR-RAM page to
		each section.  This allows **every single tile in the NT** to have a unique tile graphic!

		Long story short:

		A nametable spans from $2000-$23BF   ($23C0-$23FF are the attribute table).
		The mapper breaks the NT up like so:

		$2000-20FF = use CHR page 0
		$2100-21FF = use CHR page 1
		$2200-22FF = use CHR page 2
		$2300-23BF = use CHR page 3

		the other nametables at $2400, $2800, $2C00 are broken up in the same fashion.




		Long story long:

		PPU Address lines are modified as the PPU fetches tiles, and also when the game manually changes the PPU
		address (via the second write to $2006 --- or by the increment after read/writing $2007).  The mapper
		monitors every change to the PPU Address lines, and when it lies within a certain range, it swaps the
		appropriate CHR page in.

		It will only swap CHR when the address falls between $2000-2FFF (or mirrored regions like $6000-6FFF,
		$A000-AFFF, $E000-EFFF).  $3xxx will not trigger a swap.

		When in that range, it checks to make sure the address is not attribute tables ((Addr AND $03FF) < $03C0).
		Note I'm not 100% sure if the mapper really does this or not.  It's very possible that attribute fetches will
		also swap CHR... this would not really disrupt anything other than making the game be more careful about its
		PPU writes.

		When all that checks out, bits 8 and 9 (Addr AND $0300) select the 4k CHR page to swap in to $0000.


		Note that the mapper does not distinguish between PPU driven line changes and game driven line changes.
		This means that games can manually swap the CHR page by doing specific writes to $2006:


		LDA #$20
		STA $2006
		STA $2006   ; Addr set to $20xx -- CHR page 0 selected
 
		LDA #$21
		STA $2006
		STA $2006   ; Addr set to $21xx -- CHR page 1 selected
 
		And in fact, games would HAVE to do that to select CHR, since that's the only way to fill CHR RAM with the
		desired data.  So make sure your emu supports this.
		*/
		int chr_block;
		int chr_pos = 0;
		int prg_bank_mask_32k;
		byte prg_bank_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER096":
				case "BANDAI-74*161/02/74":
					break;
				default:
					return false;
			}
			chr_block = 0;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_block", ref chr_block);
			ser.Sync("chr_pos", ref chr_pos);
			ser.Sync("prg_bank_mask_16k", ref prg_bank_mask_32k);
			ser.Sync("prg_bank_16k", ref prg_bank_32k);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_bank_32k = (byte)(value & 0x03);
			chr_block = (value >> 2) & 0x01;
		}

		public override byte ReadPRG(int addr)
		{
			int bank_32k = prg_bank_32k & prg_bank_mask_32k;

			int x = 0;
			switch (bank_32k)
			{
				case 0:
					x += 0;
					break;
				case 1:
					x += 1;
					break;
				case 2:
					x += 2;
					break;
				case 3:
					x += 3;
					break;
			}
			return ROM[(bank_32k << 15) + addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr >= 0x1000)
				{
					if (chr_block == 1)
					{
						return VRAM[(0x1000 * 3 * 2) + addr];
					}
					else
					{
						return VRAM[(0x1000 * 3) + addr];
					}
				}
				else
				{
					if (chr_block == 1)
					{
						return VRAM[(0x1000 * chr_pos * 2) + addr];
					}
					else
					{
						return VRAM[(0x1000 * chr_pos * 2) + addr];
					}
				}
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (addr >= 0x1000)
				{
					if (chr_block == 1)
					{
						VRAM[(0x1000 * 3 * 2) + addr] = value;
					}
					else
					{
						VRAM[(0x1000 * 3) + addr] = value;
					}
				}
				{
					if (chr_block == 1)
					{
						VRAM[(0x1000 * chr_pos * 2) + addr] = value;
					}
					else
					{
						VRAM[(0x1000 * chr_pos * 2) + addr] = value;
					}
				}
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}

		public override void AddressPPU(int addr)
		{
			byte newpos;
			if ((addr & 0x3000) != 0x2000) return;
			if ((addr & 0x3FF) >= 0x3C0) return;
			newpos = (byte)((addr >> 8) & 3);
			if (chr_pos != newpos)
			{
				chr_pos = newpos;
			}

			base.AddressPPU(addr);
		}
	}
}
