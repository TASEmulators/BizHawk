using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper225 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 225          =
		========================

		Example Games:
		--------------------------
		52 Games
		58-in-1
		64-in-1


		Registers:
		---------------------------

		$5800-5803:  [.... RRRR]  RAM  (readable/writable)
		(16 bits of RAM -- 4 bits in each of the 4 regs)
		$5804-5FFF:    mirrors $5800-5803

		$8000-FFFF:  A~[.HMO PPPP PPCC CCCC]
		H = High bit (acts as bit 7 for PRG and CHR regs)
		M = Mirroring (0=Vert, 1=Horz)
		O = PRG Mode
		P = PRG Reg
		C = CHR Reg


		CHR Setup:
		---------------------------

		$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
					+---------------------------------------------------------------+
		CHR Mode 0: |                             $8000                             |
					+---------------------------------------------------------------+


		PRG Setup:
		---------------------------

					  $8000   $A000   $C000   $E000  
					+-------------------------------+
		PRG Mode 0: |            <$8000>            |
					+-------------------------------+
		PRG Mode 1: |     $8000     |     $8000     |
					+---------------+---------------+
		*/

		bool prg_mode = false;
		int chr_reg;
		int prg_reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER225":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_mode = addr.Bit(12);

			if (addr.Bit(13))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}
			int high = (addr & 0x4000) >> 8;
			prg_reg = (addr >> 6) & 0x3F | high;
			chr_reg = addr & 0x3F | high;
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode)
			{
				return ROM[((prg_reg >> 1) * 0x8000) + addr];
			}
			else
			{
				return ROM[(prg_reg * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(chr_reg * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
