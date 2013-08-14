using BizHawk.Emulation.Computers.Commodore64.MOS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public partial class Motherboard
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
            WriteInputPort();
            return cia0InputLatchA;
        }

        byte Cia0_ReadPortB()
        {
            WriteInputPort();
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

        byte Pla_ReadBasicRom(int addr)
        {
            address = addr; 
            bus = basicRom.Read(addr); 
            return bus;
        }

        byte Pla_ReadCartridgeHi(int addr)
        {
            address = addr;
            bus = cartPort.ReadHiRom(addr);
            return bus;
        }

        byte Pla_ReadCartridgeLo(int addr)
        {
            address = addr; 
            bus = cartPort.ReadLoRom(addr); 
            return bus;
        }

        bool Pla_ReadCharen()
        {
            return (cpu.PortData & 0x04) != 0;
        }

        byte Pla_ReadCharRom(int addr)
        {
            address = addr; 
            bus = charRom.Read(addr); 
            return bus;
        }

        byte Pla_ReadCia0(int addr)
        {
            address = addr;
            bus = cia0.Read(addr);
            if (!inputRead && (addr == 0xDC00 || addr == 0xDC01))
                inputRead = true;
            return bus;
        }

        byte Pla_ReadCia1(int addr)
        {
            address = addr; 
            bus = cia1.Read(addr); 
            return bus;
        }

        byte Pla_ReadColorRam(int addr)
        {
            address = addr;
            bus &= 0xF0;
            bus |= colorRam.Read(addr);
            return bus;
        }

        byte Pla_ReadExpansionHi(int addr)
        {
            address = addr;
            bus = cartPort.ReadHiExp(addr);
            return bus;
        }

        byte Pla_ReadExpansionLo(int addr)
        {
            address = addr;
            bus = cartPort.ReadLoExp(addr);
            return bus;
        }

        bool Pla_ReadHiRam()
        {
            return (cpu.PortData & 0x02) != 0;
        }

        byte Pla_ReadKernalRom(int addr)
        {
            address = addr;
            bus = kernalRom.Read(addr);
            return bus;
        }

        bool Pla_ReadLoRam()
        {
            return (cpu.PortData & 0x01) != 0;
        }

        byte Pla_ReadMemory(int addr)
        {
            address = addr;
            bus = ram.Read(addr);
            return bus;
        }

        byte Pla_ReadSid(int addr)
        {
            address = addr;
            bus = sid.Read(addr);
            return bus;
        }

        byte Pla_ReadVic(int addr)
        {
            address = addr;
            bus = vic.Read(addr);
            return bus;
        }

        void Pla_WriteCartridgeHi(int addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteHiRom(addr, val);
        }

        void Pla_WriteCartridgeLo(int addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteLoRom(addr, val);
        }

        void Pla_WriteCia0(int addr, byte val)
        {
            address = addr;
            bus = val;
            cia0.Write(addr, val);
        }

        void Pla_WriteCia1(int addr, byte val)
        {
            address = addr;
            bus = val;
            cia1.Write(addr, val);
        }

        void Pla_WriteColorRam(int addr, byte val)
        {
            address = addr;
            bus = val;
            colorRam.Write(addr, val);
        }

        void Pla_WriteExpansionHi(int addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteHiExp(addr, val);
        }

        void Pla_WriteExpansionLo(int addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteLoExp(addr, val);
        }

        void Pla_WriteMemory(int addr, byte val)
        {
            address = addr;
            bus = val;
            ram.Write(addr, val);
        }

        void Pla_WriteSid(int addr, byte val)
        {
            address = addr;
            bus = val;
            sid.Write(addr, val);
        }

        void Pla_WriteVic(int addr, byte val)
        {
            address = addr;
            bus = val;
            vic.Write(addr, val);
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
            switch (cia1.PortAData & 0x3)
            {
                case 0:
                    addr |= 0xC000;
                    break;
                case 1:
                    addr |= 0x8000;
                    break;
                case 2:
                    addr |= 0x4000;
                    break;
            }
            address = addr;
            if ((addr & 0x7000) == 0x1000)
                bus = charRom.Read(addr);
            else
                bus = ram.Read(addr);
            return bus;
        }

        byte Vic_ReadColorRam(int addr)
        {
            address = addr;
            bus &= 0xF0;
            bus |= colorRam.Read(addr);
            return bus;
        }
    }
}
