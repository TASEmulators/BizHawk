using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class GenVDP
    {
        // TODO: make this a requirement of constructor?
        public Func<int, short> DmaReadFrom68000; // TODO make ushort

        public int DmaLength { get { return Registers[19] | (Registers[20] << 8); } }

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

        void ExecuteDmaFill(ushort data)
        {
            Console.WriteLine("DMA FILL REQD, WRITE {0:X4}, {1:X4} times, at {2:X4}", data, DmaLength, VdpDataAddr);

            // TODO: Is the address really VdpDataAddr and not DMA source? I guess that makes sense.
            // TODO: It should spread this out, not do it all at once.
            // TODO: DMA can go to places besides just VRAM (eg CRAM, VSRAM)
            // TODO: Does DMA fill really use the actual increment register value?

            int length = DmaLength;
            if (length == 0)
                length = 0x10000; // Really necessary?

            byte fillByte = (byte)(data >> 8);

            do
            {
                VRAM[VdpDataAddr & 0xFFFF] = fillByte;
                UpdatePatternBuffer(VdpDataAddr & 0xFFFF);
                VdpDataAddr += Registers[15];
            } while (--length > 0);

            DmaFillModePending = false;
        }

        void Execute68000VramCopy()
        {
            Console.WriteLine("DMA 68000 -> VRAM COPY REQ'D. LENGTH {0:X4}, SOURCE {1:X4}", DmaLength, DmaSource);
            
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
    }
}