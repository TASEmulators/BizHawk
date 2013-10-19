using System;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    // HuC6260 Video Color Encoder
    public sealed class VCE
    {
        public ushort VceAddress;
        public ushort[] VceData = new ushort[512];
        public int[] Palette = new int[512];
        public byte CR;

        public int NumberOfScanlines { get { return ((CR & 4) != 0) ? 263 : 262; } }
        public int DotClock 
        {
            get
            {
                int clock = CR & 3;
                if (clock == 3) 
                    clock = 2;
                return clock;
            }
        }

        // guess the dot clock affects wait states in combination with the dot with regs.
        // We dont currently emulate wait states, probably will have to eventually...

        // Note: To keep the VCE class from needing a reference to the CPU, the 1-cycle access 
        // penalty for the VCE is handled by the memory mappers.
        // One day I guess I could create an ICpu with a StealCycles method...

        public void WriteVCE(int port, byte value)
        {
            port &= 0x07;
            switch (port)
            {
                case 0: // Control Port
                    CR = value;
                    break;
                case 2: // Address LSB
                    VceAddress &= 0xFF00;
                    VceAddress |= value;
                    break;
                case 3: // Address MSB
                    VceAddress &= 0x00FF;
                    VceAddress |= (ushort) (value << 8);
                    VceAddress &= 0x01FF;
                    break;
                case 4: // Data LSB
                    VceData[VceAddress] &= 0xFF00;
                    VceData[VceAddress] |= value;
                    PrecomputePalette(VceAddress);
                    break;
                case 5: // Data MSB
                    VceData[VceAddress] &= 0x00FF;
                    VceData[VceAddress] |= (ushort) (value << 8);
                    PrecomputePalette(VceAddress);
                    VceAddress++;
                    VceAddress &= 0x1FF;
                    break;
            }
        }

        public byte ReadVCE(int port)
        {
            port &= 0x07;
            switch (port)
            {
                case 4: // Data LSB
                    return (byte) (VceData[VceAddress] & 0xFF);
                case 5: // Data MSB
                    byte value = (byte) ((VceData[VceAddress] >> 8) | 0xFE);
                    VceAddress++;
                    VceAddress &= 0x1FF;
                    return value;
                default: return 0xFF;
            }
        }

        static readonly byte[] PalConvert = {0, 36, 72, 109, 145, 182, 218, 255};

        public void PrecomputePalette(int slot)
        {
            byte r = PalConvert[(VceData[slot] >> 3) & 7];
            byte g = PalConvert[(VceData[slot] >> 6) & 7];
            byte b = PalConvert[VceData[slot] & 7];
            Palette[slot] = Colors.ARGB(r, g, b);
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[VCE]");
            writer.WriteLine("VceAddress {0:X4}", VceAddress);
            writer.WriteLine("CR {0}", CR);
            writer.Write("VceData ");
            VceData.SaveAsHex(writer);
            writer.WriteLine("[/VCE]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/VCE]") break;
                if (args[0] == "VceAddress")
                    VceAddress = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "CR")
                    CR = byte.Parse(args[1]);
                else if (args[0] == "VceData")
                    VceData.ReadFromHex(args[1]);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }

            for (int i = 0; i < VceData.Length; i++)
                PrecomputePalette(i);
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(VceAddress);
            writer.Write(CR);
            for (int i = 0; i < VceData.Length; i++)
                writer.Write(VceData[i]);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            VceAddress = reader.ReadUInt16();
            CR = reader.ReadByte();
            for (int i = 0; i < VceData.Length; i++)
            {
                VceData[i] = reader.ReadUInt16();
                PrecomputePalette(i);
            }
        }
    }
}
