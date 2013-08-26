using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Keyboard
    {
        virtual public int Column { get { return 0xFF; } }
        public int OutputColumn() { return Column; }
        public bool OutputRestore() { return Restore; }
        public int OutputRow() { return Row; }
        virtual public bool Restore { get { return true; } }
        virtual public int Row { get { return 0xFF; } }
        virtual public void SyncState(Serializer ser) { SaveState.SyncObject(ser, this); }
    }
}
