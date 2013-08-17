using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Sid
    {
        public ISoundProvider GetSoundProvider()
        {
            return new NullSound();
        }
    }
}
