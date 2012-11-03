using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public enum MemoryDesignation
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

    public class MemoryLayout
    {
        public MemoryDesignation Mem1000 = MemoryDesignation.RAM;
        public MemoryDesignation Mem8000 = MemoryDesignation.RAM;
        public MemoryDesignation MemA000 = MemoryDesignation.RAM;
        public MemoryDesignation MemC000 = MemoryDesignation.RAM;
        public MemoryDesignation MemD000 = MemoryDesignation.RAM;
        public MemoryDesignation MemE000 = MemoryDesignation.RAM;
    }

    public class Memory
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
        public MemoryLayout layout;

        // ram
        public byte[] colorRam;
        public byte[] ram;
        public int vicOffset;

        // registers
        public byte busData;
        public byte cpu00;      // register $00
        public byte cpu01;      // register $01
        public bool readTrigger = true;
        public bool writeTrigger = true;

        public Memory(string sourceFolder, VicII newVic, Sid newSid, Cia newCia1, Cia newCia2)
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

            layout = new MemoryLayout();
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

        public byte CIA2ReadPortA()
        {
            return 0;
        }

        public byte CIA2ReadPortB()
        {
            return 0;
        }

        public void CIA2WritePortA(byte val, byte direction)
        {
        }

        public void CIA2WritePortB(byte val, byte direction)
        {
        }

        public MemoryDesignation GetDesignation(ushort addr)
        {
            MemoryDesignation result;

            if (addr < 0x1000)
            {
                result = MemoryDesignation.RAM;
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

            if (result == MemoryDesignation.IO)
            {
                addr &= 0x0FFF;
                if (addr < 0x0400)
                {
                    result = MemoryDesignation.Vic;
                }
                else if (addr < 0x0800)
                {
                    result = MemoryDesignation.Sid;
                }
                else if (addr < 0x0C00)
                {
                    result = MemoryDesignation.ColorRam;
                }
                else if (addr < 0x0D00)
                {
                    result = MemoryDesignation.Cia1;
                }
                else if (addr < 0x0E00)
                {
                    result = MemoryDesignation.Cia2;
                }
                else if (addr < 0x0F00)
                {
                    result = MemoryDesignation.Expansion1;
                }
                else
                {
                    result = MemoryDesignation.Expansion2;
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
                MemoryDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryDesignation.Basic:
                        result = basicRom[addr & 0x1FFF];
                        break;
                    case MemoryDesignation.Character:
                        result = charRom[addr & 0x0FFF];
                        break;
                    case MemoryDesignation.Vic:
                        result = vic.regs[addr & 0x3F];
                        break;
                    case MemoryDesignation.Sid:
                        result = sid.regs[addr & 0x1F];
                        break;
                    case MemoryDesignation.ColorRam:
                        result = colorRam[addr & 0x03FF];
                        break;
                    case MemoryDesignation.Cia1:
                        result = cia1.regs[addr & 0x0F];
                        break;
                    case MemoryDesignation.Cia2:
                        result = cia2.regs[addr & 0x0F];
                        break;
                    case MemoryDesignation.Expansion1:
                        result = 0;
                        break;
                    case MemoryDesignation.Expansion2:
                        result = 0;
                        break;
                    case MemoryDesignation.Kernal:
                        result = kernalRom[addr & 0x1FFF];
                        break;
                    case MemoryDesignation.RAM:
                        result = ram[addr];
                        break;
                    case MemoryDesignation.ROMHi:
                        result = cart.chips[0].data[addr & cart.chips[0].romMask];
                        break;
                    case MemoryDesignation.ROMLo:
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
                MemoryDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryDesignation.Basic:
                        result = basicRom[addr & 0x1FFF];
                        break;
                    case MemoryDesignation.Character:
                        result = charRom[addr & 0x0FFF];
                        break;
                    case MemoryDesignation.Vic:
                        result = vic.Read(addr);
                        break;
                    case MemoryDesignation.Sid:
                        result = sid.Read(addr);
                        break;
                    case MemoryDesignation.ColorRam:
                        result = ReadColorRam(addr);
                        break;
                    case MemoryDesignation.Cia1:
                        result = cia1.Read(addr);
                        break;
                    case MemoryDesignation.Cia2:
                        result = cia2.Read(addr);
                        break;
                    case MemoryDesignation.Expansion1:
                        result = 0;
                        break;
                    case MemoryDesignation.Expansion2:
                        result = 0;
                        break;
                    case MemoryDesignation.Kernal:
                        result = kernalRom[addr & 0x1FFF];
                        break;
                    case MemoryDesignation.RAM:
                        result = ram[addr];
                        break;
                    case MemoryDesignation.ROMHi:
                        result = cart.Read(addr);
                        break;
                    case MemoryDesignation.ROMLo:
                        result = cart.Read(addr);
                        break;
                    default:
                        return 0;
                }
            }

            busData = result;
            return result;
        }

        public byte ReadColorRam(ushort addr)
        {
            return (byte)((busData & 0xF0) | (colorRam[addr & 0x03FF]));
        }

        public void UpdateLayout()
        {
            bool loRom = ((cpu01 & 0x01) != 0);
            bool hiRom = ((cpu01 & 0x02) != 0);
            bool ioEnable = ((cpu01 & 0x04) != 0);

            if (loRom && hiRom && exRomPin && gamePin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.Basic;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.Kernal;
            }
            else if (loRom && !hiRom && exRomPin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.RAM;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.RAM;
            }
            else if (loRom && !hiRom && !exRomPin && !gamePin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.RAM;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.RAM;
                layout.MemE000 = MemoryDesignation.RAM;
            }
            else if ((!loRom && hiRom && gamePin) || (!loRom && !hiRom && !exRomPin))
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.RAM;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.Kernal;
            }
            else if (!loRom && !hiRom && gamePin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.RAM;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = MemoryDesignation.RAM;
                layout.MemE000 = MemoryDesignation.RAM;
            }
            else if (loRom && hiRom && gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.ROMLo;
                layout.MemA000 = MemoryDesignation.Basic;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.Kernal;
            }
            else if (!loRom && hiRom && !gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.RAM;
                layout.MemA000 = MemoryDesignation.ROMHi;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.Kernal;
            }
            else if (loRom && hiRom && !gamePin && !exRomPin)
            {
                layout.Mem1000 = MemoryDesignation.RAM;
                layout.Mem8000 = MemoryDesignation.ROMLo;
                layout.MemA000 = MemoryDesignation.ROMHi;
                layout.MemC000 = MemoryDesignation.RAM;
                layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
                layout.MemE000 = MemoryDesignation.Kernal;
            }
            else if (!gamePin && exRomPin)
            {
                layout.Mem1000 = MemoryDesignation.Disabled;
                layout.Mem8000 = MemoryDesignation.ROMLo;
                layout.MemA000 = MemoryDesignation.Disabled;
                layout.MemC000 = MemoryDesignation.Disabled;
                layout.MemD000 = MemoryDesignation.IO;
                layout.MemE000 = MemoryDesignation.ROMHi;
            }
        }

        public byte VicRead(ushort addr)
        {
            return Read(addr);
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
                MemoryDesignation des = GetDesignation(addr);

                switch (des)
                {
                    case MemoryDesignation.Vic:
                        vic.Write(addr, val);
                        break;
                    case MemoryDesignation.Sid:
                        sid.Write(addr, val);
                        break;
                    case MemoryDesignation.ColorRam:
                        colorRam[addr & 0x03FF] = (byte)(val & 0x0F);
                        break;
                    case MemoryDesignation.Cia1:
                        cia1.Write(addr, val);
                        break;
                    case MemoryDesignation.Cia2:
                        cia2.Write(addr, val);
                        break;
                    case MemoryDesignation.Expansion1:
                        break;
                    case MemoryDesignation.Expansion2:
                        break;
                    case MemoryDesignation.RAM:
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
