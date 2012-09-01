using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class GenVDP
    {
        // TODO: make this a requirement of constructor?
        public Func<int, short> DmaReadFrom68000; // TODO make ushort

        public int DmaLength { get { return Registers[19] | (Registers[20] << 8); } }
        public int DmaMode   { get { return (Registers[23] >> 6) & 3; } }

        public int DmaSource
        {
            get
            {
                if ((Registers[23] & 0x80) == 0) // 68000 -> VRAM copy mode
                    return ((Registers[21] << 1) | (Registers[22] << 9) | (Registers[23] << 17)) & 0xFFFFFE;
                
                // Else VRAM/VRAM copy mode
                return (Registers[21] | (Registers[22] << 8)) & 0xFFFFFE;
            }
        }

        bool DmaFillModePending;

        void ExecuteVramFill(ushort data)
        {
            Log.Note("VDP", "DMA VRAM FILL REQD, WRITE {0:X4}, {1:X4} times, at {2:X4}", data, DmaLength, VdpDataAddr);

            // TODO: It should spread this out, not do it all at once.
            // TODO: DMA can go to places besides just VRAM (eg CRAM, VSRAM) ??? can it?
            // TODO: what is this genvdp.txt comment about accurate vdp fill emulation writing some other byte first?

            int length = DmaLength;
            if (length == 0)
                length = 0x10000;

            byte fillByte = (byte)(data >> 8);

            do
            {
                VRAM[VdpDataAddr] = fillByte;
                UpdatePatternBuffer(VdpDataAddr);
                VdpDataAddr += Registers[15];
            } while (--length > 0);

            DmaFillModePending = false;
        }

        void Execute68000VramCopy()
        {
            Log.Note("VDP", "DMA 68000 -> VRAM COPY REQ'D. LENGTH {0:X4}, SOURCE {1:X4}", DmaLength, DmaSource);
            
            int length = DmaLength;
            if (length == 0)
                length = 0x10000;

            int source = DmaSource;

            do
            {
                ushort value = (ushort) DmaReadFrom68000(source);
                source += 2;
                // TODO funky source behavior
                WriteVdpData(value);
            } while (--length > 0);

            // TODO: find correct number of 68k cycles to burn
        }

        void ExecuteVramVramCopy()
        {
            Log.Note("VDP", "DMA VRAM -> VRAM COPY REQ'D. LENGTH {0:X4}, SOURCE {1:X4}", DmaLength, DmaSource);

            int length = DmaLength;
            if (length == 0)
                length = 0x10000;

            int source = DmaSource;

            do
            {
                ushort value = (ushort)ReadVdpData();
                source += 2;
                // TODO funky source behavior
                WriteVdpData(value);
            } while (--length > 0);

            // TODO: find correct number of 68k cycles to burn
        }
    }
}