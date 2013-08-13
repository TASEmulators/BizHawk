using BizHawk.Emulation.Computers.Commodore64.MOS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public partial class Motherboard
    {
        bool CassPort_DeviceReadLevel()
        {
            return (cpu.PortData & 0x08) != 0;
        }

        bool CassPort_DeviceReadMotor()
        {
            return (cpu.PortData & 0x20) != 0;
        }

        bool Cia0_ReadFlag()
        {
            return cassPort.DataInput;
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

        bool Cia1_ReadFlag()
        {
            return true;
        }

        byte Cia1_ReadPortA()
        {
            // the low bits are actually the VIC memory address.
            return 0x3F;
        }

        byte Cia1_ReadPortB()
        {
            return 0xFF;
        }

        bool Cpu_ReadAEC()
        {
            return vic.AEC;
        }

        bool Cpu_ReadCassetteButton()
        {
            return true;
        }

        bool Cpu_ReadIRQ()
        {
            return cia0.IRQ & vic.IRQ & cartPort.IRQ;
        }

        bool Cpu_ReadNMI()
        {
            return cia1.IRQ;
        }

        byte Cpu_ReadPort()
        {
            byte data = 0x1F;
            if (!cassPort.Sense)
                data &= 0xEF;
            return data;
        }

        bool Cpu_ReadRDY()
        {
            return vic.BA;
        }

        bool Pla_ReadAEC()
        {
            return vic.AEC;
        }

        bool Pla_ReadBA()
        {
            return vic.BA;
        }

        byte Pla_ReadBasicRom(ushort addr)
        {
            address = addr; 
            bus = basicRom.Read(addr); 
            return bus;
        }

        byte Pla_ReadCartridgeHi(ushort addr)
        {
            address = addr;
            bus = cartPort.ReadHiRom(addr);
            return bus;
        }

        byte Pla_ReadCartridgeLo(ushort addr)
        {
            address = addr; 
            bus = cartPort.ReadLoRom(addr); 
            return bus;
        }

        bool Pla_ReadCharen()
        {
            return (cpu.PortData & 0x04) != 0;
        }

        byte Pla_ReadCharRom(ushort addr)
        {
            address = addr; 
            bus = charRom.Read(addr); 
            return bus;
        }

        byte Pla_ReadCia0(ushort addr)
        {
            address = addr;
            bus = cia0.Read(addr);
            if (!inputRead && (addr == 0xDC00 || addr == 0xDC01))
                inputRead = true;
            return bus;
        }

        byte Pla_ReadCia1(ushort addr)
        {
            address = addr; 
            bus = cia1.Read(addr); 
            return bus;
        }

        byte Pla_ReadColorRam(ushort addr)
        {
            address = addr;
            bus &= 0xF0;
            bus |= colorRam.Read(addr);
            return bus;
        }

        byte Pla_ReadExpansionHi(ushort addr)
        {
            address = addr;
            bus = cartPort.ReadHiExp(addr);
            return bus;
        }

        byte Pla_ReadExpansionLo(ushort addr)
        {
            address = addr;
            bus = cartPort.ReadLoExp(addr);
            return bus;
        }

        bool Pla_ReadExRom()
        {
            return cartPort.ExRom;
        }

        bool Pla_ReadGame()
        {
            return cartPort.Game;
        }

        bool Pla_ReadHiRam()
        {
            return (cpu.PortData & 0x02) != 0;
        }

        byte Pla_ReadKernalRom(ushort addr)
        {
            address = addr;
            bus = kernalRom.Read(addr);
            return bus;
        }

        bool Pla_ReadLoRam()
        {
            return (cpu.PortData & 0x01) != 0;
        }

        byte Pla_ReadMemory(ushort addr)
        {
            address = addr;
            bus = ram.Read(addr);
            return bus;
        }

        byte Pla_ReadSid(ushort addr)
        {
            address = addr;
            bus = sid.Read(addr);
            return bus;
        }

        byte Pla_ReadVic(ushort addr)
        {
            address = addr;
            bus = vic.Read(addr);
            return bus;
        }

        void Pla_WriteCartridgeHi(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteHiRom(addr, val);
        }

        void Pla_WriteCartridgeLo(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteLoRom(addr, val);
        }

        void Pla_WriteCia0(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cia0.Write(addr, val);
        }

        void Pla_WriteCia1(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cia1.Write(addr, val);
        }

        void Pla_WriteColorRam(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            colorRam.Write(addr, val);
        }

        void Pla_WriteExpansionHi(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteHiExp(addr, val);
        }

        void Pla_WriteExpansionLo(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            cartPort.WriteLoExp(addr, val);
        }

        void Pla_WriteMemory(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            ram.Write(addr, val);
        }

        void Pla_WriteSid(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            sid.Write(addr, val);
        }

        void Pla_WriteVic(ushort addr, byte val)
        {
            address = addr;
            bus = val;
            vic.Write(addr, val);
        }

        bool SerPort_DeviceReadAtn()
        {
            return (cia1.PortBData & 0x08) == 0;
        }

        bool SerPort_DeviceReadClock()
        {
            return (cia1.PortAData & 0x10) == 0;
        }

        bool SerPort_DeviceReadData()
        {
            return (cia1.PortAData & 0x20) == 0;
        }

        bool SerPort_DeviceReadReset()
        {
            // this triggers hard reset on ext device when low
            return true;
        }

        void SerPort_DeviceWriteAtn(bool val)
        {
            // currently not wired
        }

        void SerPort_DeviceWriteClock(bool val)
        {
            //cia1DataA = Port.ExternalWrite(cia1DataA, (byte)((cia1DataA & 0xBF) | (val ? 0x00 : 0x40)), cia1DirA);
        }

        void SerPort_DeviceWriteData(bool val)
        {
            //cia1DataA = Port.ExternalWrite(cia1DataA, (byte)((cia1DataA & 0x7F) | (val ? 0x00 : 0x80)), cia1DirA);
        }

        void SerPort_DeviceWriteSrq(bool val)
        {
            //cia0FlagSerial = val;
            //cia0.FLAG = cia0FlagCassette & cia0FlagSerial;
        }

        byte Sid_ReadPotX()
        {
            return 0;
        }

        byte Sid_ReadPotY()
        {
            return 0;
        }

        byte Vic_ReadMemory(ushort addr)
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

        byte Vic_ReadColorRam(ushort addr)
        {
            address = addr;
            bus &= 0xF0;
            bus |= colorRam.Read(addr);
            return bus;
        }
    }
}
