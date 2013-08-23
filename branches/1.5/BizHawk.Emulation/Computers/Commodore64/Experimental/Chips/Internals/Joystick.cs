using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Joystick
    {
        virtual public int Data { get { return 0xFF; } }
        public int OutputData() { return Data; }
        public int OutputPot() { return Pot; }
        virtual public int Pot { get { return 0xFF; } }
        virtual public void SyncState(Serializer ser) { Sync.SyncObject(ser, this); }
    }
}
