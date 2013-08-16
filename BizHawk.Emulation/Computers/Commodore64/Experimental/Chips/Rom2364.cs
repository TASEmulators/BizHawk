using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips
{
    public class Rom2364 : Internals.Rom
    {
        public Rom2364() : base(0x2000, 0x1FFF, 0xFF) { }
    }
}
