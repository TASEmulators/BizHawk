using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public abstract partial class Sid
    {
        public Func<int> InputAddress;
        public Func<int> InputData;
        public Func<bool> InputRead;

        virtual public int Data { get { return 0xFF; } }
        public int OutputData() { return Data; }
        public void Precache() { }
        public void SyncState(Serializer ser) { }
    }
}
