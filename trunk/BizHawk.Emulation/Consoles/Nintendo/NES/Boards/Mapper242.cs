using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
    /*
PCB Class: Unknown
iNES Mapper #242
PRG-ROM: 32KB
PRG-RAM: None
CHR-ROM: 16KB
CHR-RAM: None
Battery is not available
mirroring - both
     * 
     * Games:
     * Wai Xing Zhan Shi (Ch)
     */

    class Mapper242 : NES.NESBoardBase
    {

        public override bool Configure(NES.EDetectionOrigin origin)
        {
            //configure
			switch (Cart.board_type)
			{
				case "Mapper242":
					break;
				default:
					return false;
			}
            return true;
        }

        public override byte ReadPPU(int addr)
        {
            return base.ReadPPU(addr);
        }

        public override byte ReadPRG(int addr)
        {
            return base.ReadPRG(addr);
        }

        public override void WriteWRAM(int addr, byte value)
        {

            base.WriteWRAM(addr, value);
        }

		public override void SyncState(Serializer ser)
        {
			base.SyncState(ser);
        }
    }
}
