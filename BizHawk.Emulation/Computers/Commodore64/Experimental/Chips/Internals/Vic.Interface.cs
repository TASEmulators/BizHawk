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

        public bool AEC { get { return aec; } }
        public bool BA { get { return ba; } }
        public bool IRQ { get { return irq; } }

        public bool OutputAEC() { return aec; }
        public bool OutputBA() { return ba; }
        public bool OutputIRQ() { return irq; }
    }
}
