using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Cpu
    {
        public Func<int> InputAddress;
        public Func<bool> InputAEC;
        public Func<int> InputData;
        public Func<bool> InputIRQ;
        public Func<bool> InputNMI;
        public Func<int> InputPort;
        public Func<bool> InputRDY;
        public Func<bool> InputReset;

        public int Address { get { return cachedAddress; } }
        public int Data { get { return cachedData; } }
        public int OutputAddress() { return Address; }
        public int OutputData() { return Data; }
        public int OutputPort() { return Port; }
        public bool OutputRead() { return Read; }
        public int Port { get { return cachedPort; } }
        public bool Read { get { return cachedRead; } }
        public void Precache() { }
        public void SyncState(Serializer ser) { }
    }
}
