using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Cpu
    {
        public Func<bool> InputAEC;
        public Func<bool> InputIRQ;
        public Func<bool> InputNMI;
        public Func<int> InputPort;
        public Func<bool> InputRDY;
        public Func<int, int> ReadMemory;
        public Action<int, int> WriteMemory;

        public int OutputPort() { return Port; }
        public bool OutputPort0() { return Port0; }
        public bool OutputPort1() { return Port1; }
        public bool OutputPort2() { return Port2; }
        public bool OutputPort3() { return Port3; }
        public bool OutputPort4() { return Port4; }
        public bool OutputPort5() { return Port5; }
        public bool OutputPort6() { return Port6; }
        public bool OutputPort7() { return Port7; }
        public int Port { get { return (portLatch | (~portDirection)) & 0xFF; } }
        public bool Port0 { get { return (Port & 0x01) != 0; } }
        public bool Port1 { get { return (Port & 0x02) != 0; } }
        public bool Port2 { get { return (Port & 0x04) != 0; } }
        public bool Port3 { get { return (Port & 0x08) != 0; } }
        public bool Port4 { get { return (Port & 0x10) != 0; } }
        public bool Port5 { get { return (Port & 0x20) != 0; } }
        public bool Port6 { get { return (Port & 0x40) != 0; } }
        public bool Port7 { get { return (Port & 0x80) != 0; } }
        public void SyncState(Serializer ser) { Sync.SyncObject(ser, this); }
    }
}
