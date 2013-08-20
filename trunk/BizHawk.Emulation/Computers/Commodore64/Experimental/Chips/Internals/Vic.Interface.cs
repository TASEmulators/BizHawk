using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        public Action ClockPhi0;
        public Func<int, int> ReadColorRam;
        public Func<int, int> ReadRam;

        public bool AEC { get { return cachedAEC; } }
        public bool BA { get { return cachedBA; } }
        public bool IRQ { get { return cachedIRQ; } }

        public bool OutputAEC() { return AEC; }
        public bool OutputBA() { return BA; }
        public bool OutputIRQ() { return IRQ; }
    }
}
