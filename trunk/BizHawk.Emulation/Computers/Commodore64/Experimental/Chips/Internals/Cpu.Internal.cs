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
        int cachedPort;
        bool cachedRead;
        int delayCycles;
        int portDirection;
        int portLatch;
        MOS6502X processor;
        bool resetBuffer;
        bool resetEdge;
        int resetPC;

        public Cpu()
        {
            processor = new MOS6502X();
            processor.DummyReadMemory = CoreReadMemory;
            processor.ReadMemory = CoreReadMemory;
            processor.WriteMemory = CoreWriteMemory;
            resetBuffer = false;
            resetEdge = false;
            cachedAddress = 0xFFFF;
            cachedData = 0xFF;
            cachedPort = 0xFF;
            cachedRead = true;
        }

        public void Clock()
        {
            bool reset = InputReset();
            if (reset)
            {
                if (delayCycles > 0)
                {
                    delayCycles--;
                    if (delayCycles == 1)
                    {
                        cachedAddress = 0xFFFC;
                        resetPC = InputData();
                    }
                    else if (delayCycles == 0)
                    {
                        cachedAddress = 0xFFFD;
                        resetPC |= InputData() << 8;
                        processor.PC = (ushort)resetPC;
                    }
                }
                else
                {
                    if (!resetBuffer)
                    {
                        // perform these actions on positive edge of /reset
                        processor.Reset();
                        processor.BCD_Enabled = true;
                        processor.PC = (ushort)((CoreReadMemory(0xFFFD) << 8) | CoreReadMemory(0xFFFC));
                    }
                    else if (InputAEC())
                    {
                        processor.IRQ = !InputIRQ(); //6502 core expects inverted input
                        processor.NMI = !InputNMI(); //6502 core expects inverted input
                        processor.RDY = InputRDY();
                        processor.ExecuteOne();
                    }
                }
            }
            else
            {
                cachedAddress = 0xFFFF;
                cachedData = 0xFF;
                delayCycles = 8;
                portDirection = 0xFF;
                portLatch = 0xFF;
            }
            resetBuffer = reset;
        }

        byte CoreReadMemory(ushort addr)
        {
            cachedAddress = addr;
            cachedRead = true;
            if (addr == 0x0000)
            {
                cachedData = portDirection;
            }
            else if (addr == 0x0001)
            {
                cachedData = InputPort() | (portDirection ^ 0xFF);
            }
            else
            {
                cachedData = InputData();
            }
            return (byte)(cachedData & 0xFF);
        }

        void CoreWriteMemory(ushort addr, byte val)
        {
            cachedAddress = addr;
            cachedData = val;
            if (addr == 0x0000)
            {
                cachedRead = true;
                portDirection = val;
            }
            else if (addr == 0x0001)
            {
                cachedRead = true;
                portLatch = val;
            }
            else
            {
                cachedRead = false;
            }
        }
    }
}
