using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Cpu
    {
        public Func<int> InputAddress;
        public Func<bool> InputAEC;
        public Func<bool> InputClock;
        public Func<int> InputData;
        public Func<bool> InputIRQ;
        public Func<bool> InputNMI;
        public Func<int> InputPort;
        public Func<bool> InputRDY;
        public Func<bool> InputReset;

        virtual public int Address { get { return 0xFFFF; } }
        virtual public int Data { get { return 0xFF; } }
        public int OutputAddress() { return Address; }
        public int OutputData() { return Data; }
        public int OutputPort() { return Port; }
        public bool OutputRead() { return Read; }
        virtual public int Port { get { return 0xFF; } }
        virtual public bool Read { get { return true; } }
        virtual public void Precache() { }
        virtual public void SyncState(Serializer ser) { }
    }
}
