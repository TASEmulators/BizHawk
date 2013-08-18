using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public abstract partial class Vic
    {
        public void Precache()
        {
            cachedAEC = (pixelTimer >= 4);
            cachedBA = ba;
            cachedIRQ = irq;
        }
    }
}
