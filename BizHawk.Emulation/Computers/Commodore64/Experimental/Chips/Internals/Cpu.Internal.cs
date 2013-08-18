using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Cpu
    {
        int cachedAddress;
        int cachedData;
        bool cachedNMI;
        int cachedPort;
        int delayCycles;
        bool nmiBuffer;
        int portDirection;
        int portLatch;
        MOS6502X processor;
        int resetPC;

        public Cpu()
        {
            processor = new MOS6502X();
            processor.DummyReadMemory = CoreReadMemory;
            processor.ReadMemory = CoreReadMemory;
            processor.WriteMemory = CoreWriteMemory;
            Reset();
        }

        public void Clock()
        {
            if (delayCycles > 0)
            {
                delayCycles--;
                if (delayCycles == 1)
                {
                    resetPC = ReadMemory(0xFFFC);
                }
                else if (delayCycles == 0)
                {
                    resetPC |= ReadMemory(0xFFFD) << 8;
                    processor.PC = (ushort)resetPC;
                }
            }
            else
            {
                if (InputAEC())
                {
                    processor.IRQ = !InputIRQ(); //6502 core expects inverted input
                    nmiBuffer = InputNMI();
                    if (!nmiBuffer && cachedNMI)
                        processor.NMI = true; //6502 core expects inverted input
                    cachedNMI = nmiBuffer;
                    processor.RDY = InputRDY();
                    processor.ExecuteOne();
                }
            }
        }

        byte CoreReadMemory(ushort addr)
        {
            if (addr == 0x0000)
                return (byte)(portDirection & 0xFF);
            else if (addr == 0x0001)
                return (byte)((InputPort() | (portDirection ^ 0xFF)) & 0xFF);
            else
                return (byte)(ReadMemory(addr) & 0xFF);
        }

        void CoreWriteMemory(ushort addr, byte val)
        {
            cachedAddress = addr;
            cachedData = val;
            if (addr == 0x0000)
                portDirection = val;
            else if (addr == 0x0001)
                portLatch = val;
            else
                WriteMemory(addr, val);
        }

        public void Reset()
        {
            delayCycles = 6;
            processor.Reset();
            processor.BCD_Enabled = true;
            processor.PC = (ushort)((CoreReadMemory(0xFFFD) << 8) | CoreReadMemory(0xFFFC));
        }
    }
}
