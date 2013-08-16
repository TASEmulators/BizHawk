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
            addr &= cpu.Address;
            addr &= expansion.Address;
            addr &= vic.Address;
            return addr;
        }

        public int ReadData()
        {
            int data = 0xFF;
            data &= expansion.Data;
            if (pla.Basic)
                data &= basicRom.Data;
            if (pla.CharRom)
                data &= characterRom.Data;
            if (pla.GraphicsRead)
                data &= colorRam.Data;
            if (pla.IO)
            {
                data &= sid.Data;
                data &= vic.Data;
            }
            data &= cpu.Data;
            data &= kernalRom.Data;
            data &= memory.Data;
            return data;
        }
    }
}
