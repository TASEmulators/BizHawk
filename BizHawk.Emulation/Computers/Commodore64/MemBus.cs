using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public enum MemoryBusDesignation
    {
        Disabled,
        RAM,
        Basic,
        Kernal,
        IO,
        Character,
        ROMLo,
        ROMHi,
        Vic,
        Sid,
        ColorRam,
        Cia1,
        Cia2,
        Expansion1,
        Expansion2
    }

    public class MemoryBusLayout
    {
        public MemoryBusDesignation Mem1000 = MemoryBusDesignation.RAM;
        public MemoryBusDesignation Mem8000 = MemoryBusDesignation.RAM;
        public MemoryBusDesignation MemA000 = MemoryBusDesignation.RAM;
        public MemoryBusDesignation MemC000 = MemoryBusDesignation.RAM;
        public MemoryBusDesignation MemD000 = MemoryBusDesignation.RAM;
        public MemoryBusDesignation MemE000 = MemoryBusDesignation.RAM;
    }

    public class MemoryBus
    {
        // chips
        public Cia cia1;
        public Cia cia2;
        public VicII vic;
        public Sid sid;

        // storage
        public Cartridge cart;
        public bool cartInserted = false;

        // roms
        public byte[] basicRom;
        public byte[] charRom;
        public bool exRomPin = true;
        public bool gamePin = true;
        public byte[] kernalRom;
        public MemoryBusLayout layout;

        // ram
        public byte[] colorRam;
        public byte[] ram;

        // registers
        public byte busData;
        public byte cpu00;      // register $00
        public byte cpu01;      // register $01
        public bool readTrigger = true;
        public bool writeTrigger = true;

        public MemoryBus(string sourceFolder, VicII newVic, Sid newSid, Cia newCia1, Cia newCia2)
        {
            ram = new byte[0x10000];
            WipeMemory();

            string basicFile = "basic";
            string charFile = "chargen";
            string kernalFile = "kernal";

            basicRom = File.ReadAllBytes(Path.Combine(sourceFolder, basicFile));
            charRom = File.ReadAllBytes(Path.Combine(sourceFolder, charFile));
            kernalRom = File.ReadAllBytes(Path.Combine(sourceFolder, kernalFile));
            colorRam = new byte[0x1000];

            vic = newVic;
            sid = newSid;
            cia1 = newCia1;
            cia2 = newCia2;
            cpu00 = 0x2F;
            cpu01 = 0x37;

            layout = new MemoryBusLayout();
            UpdateLayout();
        }

        public void ApplyCartridge(Cartridge newCart)
        {
            cart = newCart;
            cartInserted = true;
            exRomPin = cart.exRomPin;
            gamePin = cart.gamePin;
            UpdateLayout();
        }

        public MemoryBusDesignation GetDesignation(ushort addr)
        {
            MemoryBusDesignation result;

            if (addr < 0x1000)
            {
                result = MemoryBusDesignation.RAM;
            }
            else if (addr < 0x8000)
            {
                result = layout.Mem1000;
            }
            else if (addr < 0xA000)
            {
                result = layout.Mem8000;
            }
            else if (addr < 0xC000)
            {
                result = layout.MemA000;
            }
            else if (addr < 0xD000)
            {
                result = layout.MemC000;
            }
            else if (addr < 0xE000)
            {
                result = layout.MemD000;
            }
            else
            {
                result = layout.MemE000;
            }

            if (result == MemoryBusDesignation.IO)
            {
                addr &= 0x0FFF;
                if (addr < 0x0400)
                {
                    result = MemoryBusDesignation.Vic;
                }
                else if (addr < 0x0800)
                {
                    result = MemoryBusDesignation.Sid;
                }
                else if (addr < 0x0C00)
                {
                    result = MemoryBusDesignation.ColorRam;
                }
                else if (addr < 0x0D00)
                {
                    result = MemoryBusDesignation.Cia1;
                }
                else if (addr < 0x0E00)
                {
                    result = MemoryBusDesignation.Cia2;
                }
                else if (addr < 0x0F00)
                {
                    result = MemoryBusDesignation.Expansion1;
                }
                else
                {
                    result = MemoryBusDesignation.Expansion2;
                }
            }

            return result;
        }

        public byte Peek(ushort addr)
        {
            byte result;

            if (addr == 0x0000)
            {
                result = cpu00;
            }
            else if (addr == 0x0001)
            {
                result = cpu01;
            }
            else
            {
                MemoryBusDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryBusDesignation.Basic:
                        result = basicRom[addr & 0x1FFF];
                        break;
                    case MemoryBusDesignation.Character:
                        result = charRom[addr & 0x0FFF];
                        break;
                    case MemoryBusDesignation.Vic:
                        result = vic.regs[addr & 0x3F];
                        break;
                    case MemoryBusDesignation.Sid:
                        result = sid.regs[addr & 0x1F];
                        break;
                    case MemoryBusDesignation.ColorRam:
                        result = colorRam[addr & 0x03FF];
                        break;
                    case MemoryBusDesignation.Cia1:
                        result = cia1.regs[addr & 0x0F];
                        break;
                    case MemoryBusDesignation.Cia2:
                        result = cia2.regs[addr & 0x0F];
                        break;
                    case MemoryBusDesignation.Expansion1:
                        result = 0;
                        break;
                    case MemoryBusDesignation.Expansion2:
                        result = 0;
                        break;
                    case MemoryBusDesignation.Kernal:
                        result = kernalRom[addr & 0x1FFF];
                        break;
                    case MemoryBusDesignation.RAM:
                        result = ram[addr];
                        break;
                    case MemoryBusDesignation.ROMHi:
                        result = cart.chips[0].data[addr & cart.chips[0].romMask];
                        break;
                    case MemoryBusDesignation.ROMLo:
                        result = cart.chips[0].data[addr & cart.chips[0].romMask];
                        break;
                    default:
                        return 0;
                }
            }

            busData = result;
            return result;
        }

        public byte Read(ushort addr)
        {
            byte result;

            if (addr == 0x0000)
            {
                result = cpu00;
            }
            else if (addr == 0x0001)
            {
                result = cpu01;
            }
            else
            {
                MemoryBusDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryBusDesignation.Basic:
                        result = basicRom[addr & 0x1FFF];
                        break;
                    case MemoryBusDesignation.Character:
                        result = charRom[addr & 0x0FFF];
                        break;
                    case MemoryBusDesignation.Vic:
                        result = vic.Read(addr);
                        break;
                    case MemoryBusDesignation.Sid:
                        result = sid.Read(addr);
                        break;
                    case MemoryBusDesignation.ColorRam:
                        result = (byte)((busData & 0xF0) | (colorRam[addr & 0x03FF]));
                        break;
                    case MemoryBusDesignation.Cia1:
                        result = cia1.Read(addr);
                        break;
                    case MemoryBusDesignation.Cia2:
                        result = cia2.Read(addr);
                        break;
                    case MemoryBusDesignation.Expansion1:
                        result = 0;
                        break;
                    case MemoryBusDesignation.Expansion2:
                        result = 0;
                        break;
                    case MemoryBusDesignation.Kernal:
                        result = kernalRom[addr & 0x1FFF];
                        break;
                    case MemoryBusDesignation.RAM:
                        result = ram[addr];
                        break;
                    case MemoryBusDesignation.ROMHi:
                        result = cart.Read(addr);
                        break;
                    case MemoryBusDesignation.ROMLo:
                        result = cart.Read(addr);
                        break;
                    default:
                        return 0;
                }
            }

            busData = result;
            return result;
        }

        public void UpdateLayout()
        {
            bool loRom = ((cpu01 & 0x01) != 0);
            bool hiRom = ((cpu01 & 0x02) != 0);
            bool ioEnable = ((cpu01 & 0x04) != 0);

            if (loRom && hiRom && exRomPin && gamePin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.Basic;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.Kernal;
            }
            else if (loRom && !hiRom && exRomPin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.RAM;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.RAM;
            }
            else if (loRom && !hiRom && !exRomPin && !gamePin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.RAM;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.RAM;
                layout.MemE000 = MemoryBusDesignation.RAM;
            }
            else if ((!loRom && hiRom && gamePin) || (!loRom && !hiRom && !exRomPin))
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.RAM;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.Kernal;
            }
            else if (!loRom && !hiRom && gamePin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.RAM;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = MemoryBusDesignation.RAM;
                layout.MemE000 = MemoryBusDesignation.RAM;
            }
            else if (loRom && hiRom && gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.ROMLo;
                layout.MemA000 = MemoryBusDesignation.Basic;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.Kernal;
            }
            else if (!loRom && hiRom && !gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.RAM;
                layout.MemA000 = MemoryBusDesignation.ROMHi;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.Kernal;
            }
            else if (loRom && hiRom && !gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryBusDesignation.RAM;
                layout.Mem8000 = MemoryBusDesignation.ROMLo;
                layout.MemA000 = MemoryBusDesignation.ROMHi;
                layout.MemC000 = MemoryBusDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryBusDesignation.IO : MemoryBusDesignation.Character;
                layout.MemE000 = MemoryBusDesignation.Kernal;
            }
            else if (!gamePin && exRomPin)
            {
                layout.Mem1000 = MemoryBusDesignation.Disabled;
                layout.Mem8000 = MemoryBusDesignation.ROMLo;
                layout.MemA000 = MemoryBusDesignation.Disabled;
                layout.MemC000 = MemoryBusDesignation.Disabled;
                layout.MemD000 = MemoryBusDesignation.IO;
                layout.MemE000 = MemoryBusDesignation.ROMHi;
            }
        }

        public void WipeMemory()
        {
            for (int i = 0; i < 0x10000; i += 0x80)
            {
                for (int j = 0; j < 0x40; j++)
                    ram[i + j] = 0x00;
                for (int j = 0x40; j < 0x80; j++)
                    ram[i + j] = 0xFF;
            }
        }

        public void Write(ushort addr, byte val)
        {
            if (addr == 0x0000)
            {
                cpu00 = val;
            }
            else if (addr == 0x0001)
            {
                cpu01 &= (byte)(~cpu00);
                cpu01 |= (byte)(cpu00 & val);
                UpdateLayout();
            }
            else
            {
                MemoryBusDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryBusDesignation.Vic:
                        vic.Write(addr, val);
                        break;
                    case MemoryBusDesignation.Sid:
                        sid.Write(addr, val);
                        break;
                    case MemoryBusDesignation.ColorRam:
                        colorRam[addr & 0x03FF] = (byte)(val & 0x0F);
                        break;
                    case MemoryBusDesignation.Cia1:
                        cia1.Write(addr, val);
                        break;
                    case MemoryBusDesignation.Cia2:
                        cia2.Write(addr, val);
                        break;
                    case MemoryBusDesignation.Expansion1:
                        break;
                    case MemoryBusDesignation.Expansion2:
                        break;
                    case MemoryBusDesignation.RAM:
                        ram[addr] = val;
                        break;
                    default:
                        break;
                }
            }
            busData = val;
        }
    }
}
