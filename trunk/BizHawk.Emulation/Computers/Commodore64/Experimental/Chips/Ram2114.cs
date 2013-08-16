using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips
{
    public class Ram2114 : Internals.Ram
    {
        public Ram2114() : base(0x800, 0x7FF, 0xF) { }
    }
}
