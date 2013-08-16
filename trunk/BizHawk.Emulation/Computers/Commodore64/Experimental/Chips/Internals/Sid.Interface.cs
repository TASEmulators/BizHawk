using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public abstract partial class Sid
    {
        public Func<int> InputAddress;
        public Func<bool> InputChipSelect;
        public Func<int> InputData;
        public Func<bool> InputRead;

        public int Data { get { return 0xFF; } }
        virtual public int OutputData() { return Data; }
        virtual public void Precache() { }
        virtual public void SyncState(Serializer ser) { }
    }
}
