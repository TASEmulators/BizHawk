using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public abstract partial class C64
    {
        public void InitializeConnections()
        {
            
        }

        public int ReadAddress()
        {
            int addr = 0xFFFF;
            addr &= cpu.OutputAddress();
            addr &= expansion.OutputAddress();
            addr &= vic.OutputAddress();
            return addr;
        }
    }
}
