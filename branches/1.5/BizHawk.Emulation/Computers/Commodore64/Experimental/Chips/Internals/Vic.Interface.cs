using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        // inputs
        public Action ClockPhi0;
        public Func<int, int> ReadColorRam;
        public Func<int, int> ReadRam;

        // outputs
        public bool AEC { get { return aec; } }
        public bool BA { get { return ba; } }
        public bool IRQ { get { return irq; } }
        public bool OutputAEC() { return aec; }
        public bool OutputBA() { return ba; }
        public bool OutputIRQ() { return irq; }

        // exposed internal data
        public int Address { get { return address; } }
        public int CharacterData { get { return characterData; } }
        public int ColorData { get { return colorData; } }
        public int Data { get { return data; } }
        public int DataPhi1 { get { return phi1Data; } }
        public int GraphicsData { get { return graphicsData; } }
    }
}
