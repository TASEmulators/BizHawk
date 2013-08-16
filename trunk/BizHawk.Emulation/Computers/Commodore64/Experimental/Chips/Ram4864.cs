using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips
{
    public class Ram4864 : Internals.Ram
    {
        public Ram4864() : base(0x10000, 0xFFFF, 0xFF) { }
    }
}
