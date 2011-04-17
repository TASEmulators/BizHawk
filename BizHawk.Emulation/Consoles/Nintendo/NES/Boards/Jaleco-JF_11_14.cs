using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
    /*
     * Life Span: October 1986 - April 1987
PCB Class: Jaleco-JF-11
           Jaleco-JF-14
iNES Mapper #140

JF-11
PRG-ROM: 128kb
CHR-ROM: 32kb
Battery is not available
Uses vertical mirroring
No CIC present
Other chips used: Sunsoft-1
     * 
     * Games:
     * Bio Senshi Dan - Increaser Tono Tatakai
     */

    class Jaleco_JF_11_14 : NES.NESBoardBase
    {
        
        public override bool Configure(NES.EDetectionOrigin origin)
        {
            //configure
			switch (Cart.board_type)
			{
				case "JALECO-JF-14":
					break;
				default:
					return false;
			}
            SetMirrorType(Cart.pad_h, Cart.pad_v);
            return true;
        }

        public override byte ReadPPU(int addr)
        {
            return base.ReadPPU(addr);
        }

        public override void SyncStateBinary(BinarySerializer ser)
        {
            base.SyncStateBinary(ser);
        }
    }
}
