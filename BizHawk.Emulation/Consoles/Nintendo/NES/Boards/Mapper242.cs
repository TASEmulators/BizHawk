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
        int prg, mirror;
        
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
            return ROM[addr + (prg * 0x8000)];
        }

        public override void WriteWRAM(int addr, byte value)
        {
            mirror = ((addr>>1) & 0x01);
			prg = (addr >> 3) & 15;
			if (mirror == 1) SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal);
			else SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical);
        }

		public override void SyncState(Serializer ser)
        {
			base.SyncState(ser);
            ser.Sync("prg", ref prg);
            ser.Sync("mirror", ref mirror);
        }
    }
}
