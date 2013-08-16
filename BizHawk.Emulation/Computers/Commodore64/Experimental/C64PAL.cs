using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public partial class C64PAL : C64
    {
        static private C64Timing timing;

        public C64PAL() : base(timing)
        {
        }
    }
}
