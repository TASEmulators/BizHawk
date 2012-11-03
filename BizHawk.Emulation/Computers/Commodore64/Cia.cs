using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public class Cia
    {
        public int cycles;
        public bool flagPin;
        public bool interrupt;
        public byte[] regs;
        public bool serialData;
        public bool serialReady;
        public int shiftRegisterCycles;
        public bool shiftRegisterInterrupt;
        public bool shiftRegisterInterruptEnabled;
        public bool shiftRegisterIsOutput;
        public bool timeOfDayAlarmInterrupt;
        public bool timeOfDayAlarmInterruptEnabled;
        public bool underflowTimerAInterrupt;
        public bool underflowTimerAInterruptEnabled;
        public bool underflowTimerBInterrupt;
        public bool underflowTimerBInterruptEnabled;

        public Func<byte> ReadPortA;
        public Func<byte> ReadPortB;
        public Action<byte, byte> WritePortA;
        public Action<byte, byte> WritePortB;

        public Cia(Func<byte> funcReadPortA, Func<byte> funcReadPortB, Action<byte, byte> actWritePortA, Action<byte, byte> actWritePortB)
        {
            regs = new byte[0x10];
        }

        static public byte DummyReadPort()
        {
            return 0xFF;
        }

        static public void DummyWritePort(byte val, byte direction)
        {
            // do nothing
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
                    result = ReadPortA();
                    break;
                case 0x01:
                    result = ReadPortB();
                    break;
                case 0x0D:
                    result = regs[addr];
                    shiftRegisterInterrupt = false;
                    timeOfDayAlarmInterrupt = false;
                    underflowTimerAInterrupt = false;
                    underflowTimerBInterrupt = false;
                    interrupt = false;
                    break;
                default:
                    result = regs[addr];
                    break;
            }

            return result;
        }

        public void Write(ushort addr, byte val)
        {
            switch (addr & 0x0F)
            {
                case 0x00:
                    WritePortA(val, regs[0x02]);
                    break;
                case 0x01:
                    WritePortB(val, regs[0x03]);
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
