using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Userport
    {
        public Func<bool> InputCNT1;
        public Func<bool> InputCNT2;
        public Func<int> InputData;
        public Func<bool> InputPA2;
        public Func<bool> InputPC2;
        public Func<bool> InputReset;
        public Func<bool> InputSP1;
        public Func<bool> InputSP2;

        virtual public bool ATN { get { return true; } }
        virtual public bool CNT1 { get { return true; } }
        virtual public bool CNT2 { get { return true; } }
        virtual public int Data { get { return 0xFF; } }
        virtual public bool FLAG2 { get { return true; } }
        public bool OutputATN() { return ATN; }
        public bool OutputCNT1() { return CNT1; }
        public bool OutputCNT2() { return CNT2; }
        public int OutputData() { return Data; }
        public bool OutputFLAG2() { return FLAG2; }
        public bool OutputPA2() { return PA2; }
        public bool OutputReset() { return Reset; }
        public bool OutputSP1() { return SP1; }
        public bool OutputSP2() { return SP2; }
        virtual public bool PA2 { get { return true; } }
        virtual public bool Reset { get { return true; } }
        virtual public bool SP1 { get { return true; } }
        virtual public bool SP2 { get { return true; } }
    }
}
