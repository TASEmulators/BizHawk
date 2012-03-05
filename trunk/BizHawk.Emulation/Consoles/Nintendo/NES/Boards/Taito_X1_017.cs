using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Taito_X1_017 : NES.NESBoardBase 
	{
		/*
		ines Mapper 82
		http://wiki.nesdev.com/w/index.php/INES_Mapper_082

		 *  Example Games:
		 --------------------------
		 SD Keiji - Blader
		 Kyuukyoku Harikiri Stadium
 
 
		 Notes:
		 ---------------------------
		 Regs appear at $7EFx, I'm unsure whether or not PRG-RAM can exist at $6000-7FFF
 
 
		 Registers:
		 ---------------------------
 
		   $7EF0-7EF5:  CHR Regs
 
		   $7EF6:  [.... ..CM]  CHR Mode/Mirroring
			 C = CHR Mode select
			 M = Mirroring:
				0 = Horz
				1 = Vert
 
		   $7EFA:  [PPPP PP..]  PRG Reg 0 (8k @ $8000)
		   $7EFB:  [PPPP PP..]  PRG Reg 1 (8k @ $A000)
		   $7EFC:  [PPPP PP..]  PRG Reg 2 (8k @ $C000)
 
 
		 CHR Setup:
		 ---------------------------
 
						 $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
					   +---------------+---------------+-------+-------+-------+-------+
		 CHR Mode 0:   |    <$7EF0>    |    <$7EF1>    | $7EF2 | $7EF3 | $7EF4 | $7EF5 |
					   +---------------+---------------+---------------+---------------+
		 CHR Mode 1:   | $7EF2 | $7EF3 | $7EF4 | $7EF5 |    <$7EF0>    |    <$7EF1>    |
					   +-------+-------+-------+-------+---------------+---------------+
 
 
		 PRG Setup:
		 ---------------------------
 
			   $8000   $A000   $C000   $E000  
			 +-------+-------+-------+-------+
			 | $7EFA | $7EFB | $7EFC | { -1} |
			 +-------+-------+-------+-------+
 
		 Note:  remember that the low 2 bits are not used (right-shift written values by 2)
		*/

		int prg_bank_mask, chr_bank_mask;
		ByteBuffer prg_regs_8k = new ByteBuffer(4);
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		bool ChrMode;

		public override void Dispose()
		{
			base.Dispose();
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_regs_8k", ref prg_regs_8k);
			ser.Sync("chr_regs_1k", ref chr_regs_1k);
			ser.Sync("ChrMOde", ref ChrMode);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "TAITO-X1-017":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);
			chr_bank_mask = Cart.chr_size / 2 - 1;
			prg_bank_mask = Cart.prg_size / 8 - 1;
			prg_regs_8k[3] = 0xFF;
			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1EF0:
					chr_regs_1k[0] = value;
					break;
				case 0x1EF1:
					chr_regs_1k[1] = value;
					break;

				case 0x1EF2:
					chr_regs_1k[2] = value;
					break;
				case 0x1EF3:
					chr_regs_1k[3] = value;
					break;
				case 0x1EF4:
					chr_regs_1k[4] = value;
					break;
				case 0X1EF5:
					chr_regs_1k[5] = value;
					break; 

				case 0x1EF6:
					ChrMode = value.Bit(1);
					if (value.Bit(0))
						SetMirrorType(EMirrorType.Vertical);
					else
						SetMirrorType(EMirrorType.Horizontal);
					break;

				case 0x1EFA:  
					prg_regs_8k[0] = (byte)(value >> 2);
					break;
				case 0x1EFB:
					prg_regs_8k[1] = (byte)(value >> 2);
					break;
				case 0x1EFC:
					prg_regs_8k[1] = (byte)(value >> 2);
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask;
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			int bank_2k = addr >> 11;
			int ofs = addr & ((1 << 11) - 1);
			bank_2k = chr_regs_1k[bank_2k];
			bank_2k &= chr_bank_mask;
			addr = (bank_2k << 11) | ofs;
			return VROM[addr];
		}
	}
}
