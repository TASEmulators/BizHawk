using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public partial class VIC
    {
        int ADDR;
        bool AEC;
        bool BA;
        bool CAS;
        int DATA;
        bool IRQ;
        bool PHI0;
        bool RAS;

        bool badLineCondition;
        bool badLineEnable;
        bool idleState;
        int pixelTimer;
        int rowCounter;
        int videoCounter;
        int videoCounterBase;
        int videoMatrixLineIndex;

        public void Execute()
        {
            if (pixelTimer == 0)
            {
                PHI0 = !PHI0;
                pixelTimer = 8;

                badLineEnable |= (rasterY == 0x30 && displayEnable);
                if (!PHI0)
                {
                    badLineCondition = (
                        badLineEnable && 
                        rasterY >= 0x030 && 
                        rasterY <= 0x0F7 && 
                        (rasterY & 0x007) == yScroll
                        );
                    if (!idleState && badLineCondition)
                        idleState = true;
                }
            }
            pixelTimer--;

        }
    }
}
