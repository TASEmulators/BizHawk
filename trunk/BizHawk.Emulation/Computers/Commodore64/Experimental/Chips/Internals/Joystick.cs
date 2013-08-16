using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Joystick
    {
        virtual public int Data { get { return 0x1F; } }
        public int OutputData() { return Data; }
        public int OutputPotX() { return PotX; }
        public int OutputPotY() { return PotY; }
        virtual public int PotX { get { return 0xFF; } }
        virtual public int PotY { get { return 0xFF; } }
        virtual public void Precache() { }
        virtual public void SyncState(Serializer ser) { }
    }
}
