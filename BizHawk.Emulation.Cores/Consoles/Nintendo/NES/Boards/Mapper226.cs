using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper226 : NES.NESBoardBase
	{
		/*
		 *  Here are Disch's original notes:  
		 ========================
		 =  Mapper 226          =
		 ========================
 
		 Example Games:
		 --------------------------
		 76-in-1
		 Super 42-in-1
 
 
		 Registers:
		 ---------------------------
 
		 Range, Mask:  $8000-FFFF, $8001
 
		   $8000:  [PMOP PPPP]
			  P = Low 6 bits of PRG Reg
			  M = Mirroring (0=Horz, 1=Vert)
			  O = PRG Mode
 
		   $8001:  [.... ...H]
			  H = high bit of PRG
 
 
		 PRG Setup:
		 ---------------------------
 
		 Low 6 bits of the PRG Reg come from $8000, high bit comes from $8001
 
 
						$8000   $A000   $C000   $E000  
					  +-------------------------------+
		 PRG Mode 0:  |             <Reg>             |
					  +-------------------------------+
		 PRG Mode 1:  |      Reg      |      Reg      |
					  +---------------+---------------+
		*/

		public int prg_page;
		public bool prg_mode;

		private int prg_mask_32k;
		private int prg_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER226":
				case "UNIF_BMC-42in1ResetSwitch":
					break;
				default:
					return false;
			}
			prg_page = 0;
			prg_mode = false;

			prg_mask_32k = Cart.prg_size / 32 - 1;
			prg_mask_16k = Cart.prg_size / 16 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_page", ref prg_page);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 1;
			if (addr == 0)
			{
				prg_page &= ~0x3F;
				prg_page |= ((value & 0x1F) + ((value & 0x80) >> 2));
				prg_mode = value.Bit(5);

				if (value.Bit(6))
				{
					SetMirrorType(EMirrorType.Vertical);
				}
				else
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
			}
			else if (addr == 1)
			{
				prg_page &= ~0x40;
				prg_page |= ((value & 0x1) << 6);
			}
		}
		
		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				return ROM[( ((prg_page >> 1) & prg_mask_32k) * 0x8000) + (addr & 0x07FFF)];
			}
			else
			{
				return ROM[((prg_page & prg_mask_16k) * 0x4000) + (addr & 0x03FFF)];
			}
		}
	}
}
