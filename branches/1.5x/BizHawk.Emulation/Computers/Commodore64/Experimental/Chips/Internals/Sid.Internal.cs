using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Sid
    {
        public Sid(SidSettings settings)
        {
            Reset();
        }

        public void Clock()
        {
        }

        public void Reset()
        {
            for (int i = 0; i < 0x20; i++)
                Poke(i, 0);
        }
    }
}
