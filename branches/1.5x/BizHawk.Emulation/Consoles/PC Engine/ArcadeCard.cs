using System;
using System.IO;
using System.Globalization;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    partial class PCEngine
    {
        bool ArcadeCard, ArcadeCardRewindHack;
        int ShiftRegister;
        byte ShiftAmount;
        byte RotateAmount;
        ArcadeCardPage[] ArcadePage = new ArcadeCardPage[4];

        class ArcadeCardPage
        {
            public byte Control;
            public int Base;
            public ushort Offset;
            public ushort IncrementValue;

            public void Increment()
            {
                if ((Control & 1) == 0) 
                    return;

                if ((Control & 0x10) != 0)
                {
                    Base += IncrementValue;
                    Base &= 0xFFFFFF;
                }
                else
                    Offset += IncrementValue;
            }

            public int EffectiveAddress
            {
                get
                {
                    int address = Base;
                    if ((Control & 2) != 0)
                        address += Offset;
                    if ((Control & 8) != 0)
                        address += 0xFF0000;
                    return address & 0x1FFFFF;
                }
            }
        }

        void WriteArcadeCard(int addr, byte value)
        {
            if (ArcadeCard == false) return;
            var page = ArcadePage[(addr >> 4) & 3];
            switch (addr & 0x0F)
            {
                case 0:
                case 1:
                    ArcadeRam[page.EffectiveAddress] = value;
                    page.Increment();
                    break;
                case 2:
                    page.Base &= ~0xFF;
                    page.Base |= value;
                    break;
                case 3:
                    page.Base &= ~0xFF00;
                    page.Base |= (value << 8);
                    break;
                case 4:
                    page.Base &= ~0xFF0000;
                    page.Base |= (value << 16);
                    break;
                case 5:
                    page.Offset &= 0xFF00;
                    page.Offset |= value;
                    break;
                case 6:
                    page.Offset &= 0x00FF;
                    page.Offset |= (ushort) (value << 8);
                    if ((page.Control & 0x60) == 0x40)
                    {
                        page.Base += page.Offset + (((page.Control & 0x08)==0) ? 0 : 0xFF00000);
                        page.Base &= 0xFFFFFF;
                    }
                    break;
                case 7:
                    page.IncrementValue &= 0xFF00;
                    page.IncrementValue |= value;
                    break;
                case 8:
                    page.IncrementValue &= 0x00FF;
                    page.IncrementValue |= (ushort)(value << 8);
                    break;
                case 9:
                    page.Control = (byte) (value & 0x7F);
                    break;
                case 10:
                    if ((page.Control & 0x60) == 0x60)
                    {
                        page.Base += page.Offset;
                        page.Base &= 0xFFFFFF;
                        if ((page.Control & 8) != 0)
                        {
                            page.Base += 0xFF0000;
                            page.Base &= 0xFFFFFF;
                        }
                    }
                    break;
            }
        }

        byte ReadArcadeCard(int addr)
        {
            if (ArcadeCard == false) return 0xFF;
            var page = ArcadePage[(addr >> 4) & 3];
            switch (addr & 0x0F)
            {
                case 0:
                case 1:
                    byte value = ArcadeRam[page.EffectiveAddress];
                    page.Increment();
                    return value;
                case 2: return (byte) (page.Base >> 0);
                case 3: return (byte) (page.Base >> 8);
                case 4: return (byte) (page.Base >> 16);
                case 5: return (byte) (page.Offset >> 0);
                case 6: return (byte) (page.Offset >> 8);
                case 7: return (byte) (page.IncrementValue >> 0);
                case 8: return (byte) (page.IncrementValue >> 8);
                case 9: return (byte) (page.Control >> 0);
                case 10: return 0;
            }
            return 0xFF;
        }

        void SaveArcadeCardBinary(BinaryWriter writer)
        {
            writer.Write(ShiftRegister);
            writer.Write(ShiftAmount);
            writer.Write(RotateAmount);
            for (int i = 0; i < 4; i++)
            {
                writer.Write(ArcadePage[i].Control);
                writer.Write(ArcadePage[i].Base);
                writer.Write(ArcadePage[i].Offset);
                writer.Write(ArcadePage[i].IncrementValue);
            }
            if (ArcadeCardRewindHack == false)
                writer.Write(ArcadeRam);
        }

        void LoadArcadeCardBinary(BinaryReader reader)
        {
            ShiftRegister = reader.ReadInt32();
            ShiftAmount = reader.ReadByte();
            RotateAmount = reader.ReadByte();
            for (int i = 0; i < 4; i++)
            {
                ArcadePage[i].Control = reader.ReadByte();
                ArcadePage[i].Base = reader.ReadInt32();
                ArcadePage[i].Offset = reader.ReadUInt16();
                ArcadePage[i].IncrementValue = reader.ReadUInt16();
            }
            if (ArcadeCardRewindHack == false)
                ArcadeRam = reader.ReadBytes(0x200000);
        }

        void SaveArcadeCardText(TextWriter writer)
        {
            writer.WriteLine("[ArcadeCard]");
            writer.WriteLine("ShiftRegister {0} ", ShiftRegister);
            writer.WriteLine("RotateAmount {0} ", ShiftAmount);
            writer.WriteLine("RotateAmount {0} ", RotateAmount);
            for (int i = 0; i < 4; i++)
            {
                writer.WriteLine("Control {0} {1:X2}", i, ArcadePage[i].Control);
                writer.WriteLine("Base {0} {1:X6}", i, ArcadePage[i].Base);
                writer.WriteLine("Offset {0} {1:X4}", i, ArcadePage[i].Offset);
                writer.WriteLine("Increment {0} {1:X4}", i, ArcadePage[i].IncrementValue);
            }
            writer.Write("RAM "); ArcadeRam.SaveAsHex(writer);
            writer.WriteLine("[/ArcadeCard]");
            writer.WriteLine();
        }

        public void LoadArcadeCardText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/ArcadeCard]") break;
                if (args[0] == "ShiftRegister")
                    ShiftRegister = int.Parse(args[1]);
                else if (args[0] == "ShiftAmount")
                    ShiftAmount = byte.Parse(args[1]);
                else if (args[0] == "RotateAmount")
                    RotateAmount = byte.Parse(args[1]);
                else if (args[0] == "RAM")
                    ArcadeRam.ReadFromHex(args[1]);
                else if (args[0] == "Control")
                    ArcadePage[int.Parse(args[1])].Control = byte.Parse(args[2], NumberStyles.HexNumber);
                else if (args[0] == "Base")
                    ArcadePage[int.Parse(args[1])].Base = int.Parse(args[2], NumberStyles.HexNumber);
                else if (args[0] == "Offset")
                    ArcadePage[int.Parse(args[1])].Offset = ushort.Parse(args[2], NumberStyles.HexNumber);
                else if (args[0] == "Increment")
                    ArcadePage[int.Parse(args[1])].IncrementValue = ushort.Parse(args[2], NumberStyles.HexNumber);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }
    }
}