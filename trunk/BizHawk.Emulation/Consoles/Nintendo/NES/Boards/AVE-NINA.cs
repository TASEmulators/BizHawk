using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
    /*
    
     */

    class AVI_NINA_001 : NES.NESBoardBase
    {
        public override bool Configure(NES.EDetectionOrigin origin)
        {
            //configure
			switch (Cart.board_type)
			{
				case "AVI_NINA_001":
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

		public override void SyncState(Serializer ser)
        {
			base.SyncState(ser);
        }
    }
    
    /*
    */
    class AVI_Misc : NES.NESBoardBase
    {
        public override bool Configure(NES.EDetectionOrigin origin)
        {
            //configure
            switch (Cart.board_type)
            {
                case "AVI_Misc":
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

        public override void SyncState(Serializer ser)
        {
            base.SyncState(ser);
        }
    }
}
