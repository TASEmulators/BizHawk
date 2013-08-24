using BizHawk.Emulation.Computers.Commodore64.MOS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    sealed public partial class Motherboard
    {
        bool CassPort_ReadDataOutput()
        {
            return (cpu.PortData & 0x08) != 0;
        }

        bool CassPort_ReadMotor()
        {
            return (cpu.PortData & 0x20) != 0;
        }

        bool Cia0_ReadCnt()
        {
            return (userPort.ReadCounter1Buffer() && cia0.ReadCNTBuffer());
        }

        byte Cia0_ReadPortA()
        {
            return cia0InputLatchA;
        }

        byte Cia0_ReadPortB()
        {
            return cia0InputLatchB;
        }

        bool Cia0_ReadSP()
        {
            return (userPort.ReadSerial1Buffer() && cia0.ReadSPBuffer());
        }

        bool Cia1_ReadCnt()
        {
            return (userPort.ReadCounter2Buffer() && cia1.ReadCNTBuffer());
        }

        byte Cia1_ReadPortA()
        {
            // the low bits are actually the VIC memory address.
            byte result = 0xFF;
            if (serPort.WriteDataIn())
                result &= 0x7F;
            if (serPort.WriteClockIn())
                result &= 0xBF;
            return result;
        }

        bool Cia1_ReadSP()
        {
            return (userPort.ReadSerial2Buffer() && cia1.ReadSPBuffer());
        }

        byte Cpu_ReadPort()
        {
            byte data = 0x1F;
            if (!cassPort.ReadSenseBuffer())
                data &= 0xEF;
            return data;
        }

        bool Glue_ReadIRQ()
        {
            return cia0.ReadIRQBuffer() & vic.ReadIRQBuffer() & cartPort.ReadIRQBuffer();
        }

        bool Pla_ReadCharen()
        {
            return (cpu.PortData & 0x04) != 0;
        }

        byte Pla_ReadCia0(int addr)
        {
            if (addr == 0xDC00 || addr == 0xDC01)
            {
                WriteInputPort();
                inputRead = true;
            }
            return cia0.Read(addr);
        }

        byte Pla_ReadColorRam(int addr)
        {
            byte result = bus;
            result &= 0xF0;
            result |= colorRam.Read(addr);
            return result;
        }

        bool Pla_ReadHiRam()
        {
            return (cpu.PortData & 0x02) != 0;
        }

        bool Pla_ReadLoRam()
        {
            return (cpu.PortData & 0x01) != 0;
        }

        bool SerPort_ReadAtnOut()
        {
            return (cia1.PortBData & 0x08) == 0;
        }

        bool SerPort_ReadClockOut()
        {
            return (cia1.PortAData & 0x10) == 0;
        }

        bool SerPort_ReadDataOut()
        {
            return (cia1.PortAData & 0x20) == 0;
        }

        byte Sid_ReadPotX()
        {
            return 0;
        }

        byte Sid_ReadPotY()
        {
            return 0;
        }

        byte Vic_ReadMemory(int addr)
        {
            // the system sees (cia1.PortAData & 0x3) but we use a shortcut
            addr |= (0x3 - (((cia1.PortALatch & cia1.PortADirection) | (~cia1.PortADirection)) & 0x3)) << 14;
            return pla.VicRead(addr);
        }
    }
}
