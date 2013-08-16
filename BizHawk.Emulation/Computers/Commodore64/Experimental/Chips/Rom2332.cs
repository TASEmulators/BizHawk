using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips
{
    public class Rom2332 : Internals.Rom
    {
        public Rom2332() : base(0x1000, 0xFFF, 0xFF) { }
    }
}
