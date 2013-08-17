using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Cia
    {
        public Func<int> InputAddress;
        public Func<bool> InputCNT;
        public Func<int> InputData;
        public Func<bool> InputFlag;
        public Func<int> InputPortA;
        public Func<int> InputPortB;
        public Func<bool> InputRead;
        public Func<bool> InputReset;
        public Func<bool> InputSP;

        public bool CNT { get { return true; } }
        public int Data { get { return 0xFF; } }
        public bool IRQ { get { return true; } }
        public bool OutputCNT() { return CNT; }
        public int OutputData() { return Data; }
        public bool OutputIRQ() { return IRQ; }
        public bool OutputPC() { return PC; }
        public int OutputPortA() { return PortA; }
        public int OutputPortB() { return PortB; }
        public bool OutputSP() { return SP; }
        public bool PC { get { return true; } }
        public int PortA { get { return 0xFF; } }
        public int PortB { get { return 0xFF; } }
        public bool SP { get { return true; } }

        public void Clock() { }
        public void Precache() { }
    }
}
