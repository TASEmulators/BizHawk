using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 80

	//Minelvaton Saga
	//Taito Grand Prix - Eikou heno License


/*
* Registers:
---------------------------

  $7EF0-7EF5:  CHR Regs

  $7EF6:  [.... ...M]  Mirroring
    0 = Horz
    1 = Vert

  $7EFA,7EFB:  PRG Reg 0 (8k @ $8000)
  $7EFC,7EFD:  PRG Reg 1 (8k @ $A000)
  $7EFE,7EFF:  PRG Reg 2 (8k @ $C000)


CHR Setup:
---------------------------

       $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
     +---------------+---------------+-------+-------+-------+-------+
     |    <$7EF0>    |    <$7EF1>    | $7EF2 | $7EF3 | $7EF4 | $7EF5 |
     +---------------+---------------+-------+-------+-------+-------+

PRG Setup:
---------------------------

      $8000   $A000   $C000   $E000  
    +-------+-------+-------+-------+
    | $7EFA | $7EFC | $7EFE | { -1} |
    +-------+-------+-------+-------+
*/

	class TAITO_X1_005 : NES.NESBoardBase
	{
		int prg, chr;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "TAITO-X1-005":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr)
			{
				case 0x7EF6:
					if (value.Bit(0))
						SetMirrorType(EMirrorType.Vertical);
					else
						SetMirrorType(EMirrorType.Horizontal);
					break;

				case 0x7EF0:
					chr = 0;
					break;
				case 0x7EF1:
					chr = 1;
					break;
				case 0x7EF2:
					chr = 2;
					break;
				case 0x7EF3:
					chr = 3;
					break;
				case 0x7EF4:
					chr = 4;
					break;
				case 0X7EF5:
					chr = 5;
					break;

				case 0x7EFA: //PRG Reg 0
				case 0x7EFB:
					prg = 0;
					break;
				case 0x7EFC: //PRG Reg 1
				case 0x7EFD:
					prg = 1;
					break;
				case 0x7EFE: //PRG Reg 2
				case 0x7EFF:
					prg = 2;
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(addr & 0x01FFF)+ (prg * 0x2000)];
		}

		public override byte ReadPPU(int addr)
		{
			if (chr < 2)
				return VROM[(addr & 0x7FF) + (chr * 0x800)];
			else
				return VROM[(addr & 0x3FF) + 0x800 + (chr * 0x400)];
		}
	}
}