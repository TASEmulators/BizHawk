using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public enum SidMode
    {
        Sid6581,
        Sid8580
    }

    public class Sid
    {
        public byte[] regs;

        public Sid()
        {
            regs = new byte[0x20];
        }

        public void PerformCycle()
        {
        }

        public byte Read(ushort addr)
        {
            switch (addr & 0x1F)
            {
                default:
                    return 0;
            }
        }

        public void Write(ushort addr, byte val)
        {
            switch (addr & 0x1F)
            {
                default:
                    break;
            }
        }
    }
}
