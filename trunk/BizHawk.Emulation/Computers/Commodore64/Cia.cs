using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public class Cia
    {
        public int cycles;
        public byte[] regs;
        
        public Cia()
        {
            regs = new byte[0x10];
        }

        public void PerformCycle()
        {
            unchecked
            {
                cycles++;
            }
        }

        public byte Read(ushort addr)
        {
            byte result = 0;

            switch (addr & 0x0F)
            {
                case 0x00:
                    break;
                case 0x01:
                    break;
                case 0x02:
                    break;
                case 0x03:
                    break;
                case 0x04:
                    break;
                case 0x05:
                    break;
                case 0x06:
                    break;
                case 0x07:
                    break;
                case 0x08:
                    break;
                case 0x09:
                    break;
                case 0x0A:
                    break;
                case 0x0B:
                    break;
                case 0x0C:
                    break;
                case 0x0D:
                    break;
                case 0x0E:
                    break;
                case 0x0F:
                    break;
                default:
                    break;
            }

            return result;
        }

        public void Write(ushort addr, byte val)
        {
            switch (addr & 0x0F)
            {
                case 0x00:
                    break;
                case 0x01:
                    break;
                case 0x02:
                    break;
                case 0x03:
                    break;
                case 0x04:
                    break;
                case 0x05:
                    break;
                case 0x06:
                    break;
                case 0x07:
                    break;
                case 0x08:
                    break;
                case 0x09:
                    break;
                case 0x0A:
                    break;
                case 0x0B:
                    break;
                case 0x0C:
                    break;
                case 0x0D:
                    break;
                case 0x0E:
                    break;
                case 0x0F:
                    break;
                default:
                    break;
            }
        }
    }
}
