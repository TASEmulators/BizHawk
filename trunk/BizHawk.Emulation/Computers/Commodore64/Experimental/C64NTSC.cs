using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips;
using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public partial class C64NTSC : C64
    {
        static private C64Timing timing;

        public C64NTSC(byte[] basicRom, byte[] charRom, byte[] kernalRom) : base(timing)
        {
        }
    }
}
