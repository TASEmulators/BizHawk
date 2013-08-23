using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Expansion
    {
        virtual public bool ExRom { get { return true; } }
        virtual public bool Game { get { return true; } }
        virtual public bool IRQ { get { return true; } }
        virtual public bool NMI { get { return true; } }
        public bool OutputExRom() { return ExRom; }
        public bool OutputGame() { return Game; }
        public bool OutputIRQ() { return IRQ; }
        public bool OutputNMI() { return NMI; }
        virtual public void SyncState(Serializer ser) { Sync.SyncObject(ser, this); }
    }
}
