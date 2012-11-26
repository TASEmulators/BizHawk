using System;
using System.IO;
using BizHawk.Emulation.CPUs.H6280;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    // ------------------------------------------------------
    // HuC6202 Video Priority Controller
    // ------------------------------------------------------
    // Responsible for merging VDC1 and VDC2 data on the SuperGrafx.
    // Pretty much all documentation on the SuperGrafx courtesy of Charles MacDonald.

    public sealed class VPC : IVideoProvider
    {
        PCEngine PCE;
        public VDC VDC1;
        public VDC VDC2;
        public VCE VCE;
        public HuC6280 CPU;

        public byte[] Registers = {0x11, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00};

        public int Window1Width { get { return ((Registers[3] & 3) << 8) | Registers[2]; } }
        public int Window2Width { get { return ((Registers[5] & 3) << 8) | Registers[4]; } }
        public int PriorityModeSlot0 { get { return Registers[0] & 0x0F; } }
        public int PriorityModeSlot1 { get { return (Registers[0] >> 4) & 0x0F; } }
        public int PriorityModeSlot2 { get { return Registers[1] & 0x0F; } }
        public int PriorityModeSlot3 { get { return (Registers[1] >> 4) & 0x0F; } }

        public VPC(PCEngine pce, VDC vdc1, VDC vdc2, VCE vce, HuC6280 cpu)
        {
            PCE = pce;
            VDC1 = vdc1;
            VDC2 = vdc2;
            VCE = vce;
            CPU = cpu;

            // latch initial video buffer
            FrameBuffer = vdc1.GetVideoBuffer();
            FrameWidth = vdc1.BufferWidth;
            FrameHeight = vdc1.BufferHeight;
        }

        public byte ReadVPC(int port)
        {
            port &= 0x0F;
            switch (port)
            {
                case 0x08: return Registers[0];
                case 0x09: return Registers[1];
                case 0x0A: return Registers[2];
                case 0x0B: return Registers[3];
                case 0x0C: return Registers[4];
                case 0x0D: return Registers[5];
                case 0x0E: return Registers[6];
                case 0x0F: return 0;
                default:   return 0xFF;
            }
        }

        public void WriteVPC(int port, byte value)
        {
            port &= 0x0F;
            switch (port)
            {
                case 0x08: Registers[0] = value; break;
                case 0x09: Registers[1] = value; break;
                case 0x0A: Registers[2] = value; break;
                case 0x0B: Registers[3] = value; break;
                case 0x0C: Registers[4] = value; break;
                case 0x0D: Registers[5] = value; break;
                case 0x0E: 
                    // CPU Store Immediate VDC Select
                    CPU.WriteVDC = (value & 1) == 0 ? (Action<int,byte>) VDC1.WriteVDC : VDC2.WriteVDC;
                    Registers[6] = value; 
                    break;
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(Registers);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            Registers = reader.ReadBytes(7);
            WriteVPC(0x0E, Registers[6]);
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[VPC]");
            writer.Write("Registers ");
            Registers.SaveAsHex(writer);
            writer.WriteLine("[/VPC]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/VPC]") break;
                if (args[0] == "Registers")
                    Registers.ReadFromHex(args[1]);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
            WriteVPC(0x0E, Registers[6]);
        }

        // We use a single priority mode for the whole frame.
        // No commercial SGX games really use the 'window' features AFAIK.
        // And there are no homebrew SGX games I know of.
        // Maybe we'll emulate it in the native-code version.

        const int RCR = 6; 
        const int BXR = 7;
        const int BYR = 8;
        const int VDW = 13;
        const int DCR = 15;

        int EffectivePriorityMode = 0;

        int FrameHeight;
        int FrameWidth;
        int[] FrameBuffer;

        byte[] PriorityBuffer = new byte[512];
        byte[] InterSpritePriorityBuffer = new byte[512];

        public void ExecFrame(bool render)
        {
            // Determine the effective priority mode.
            if (Window1Width < 0x40 && Window2Width < 0x40)
                EffectivePriorityMode = PriorityModeSlot3 >> 2;
            else if (Window2Width > 512)
                EffectivePriorityMode = PriorityModeSlot1 >> 2;
            else
            {
                Console.WriteLine("Unsupported VPC window settings");
                EffectivePriorityMode = 0;
            }

            // Latch frame dimensions and framebuffer, for purely dumb reasons
            FrameWidth = VDC1.BufferWidth;
            FrameHeight = VDC1.BufferHeight;
            FrameBuffer = VDC1.GetVideoBuffer();

            int ScanLine = 0;
            while (true)
            {
                VDC1.ScanLine = ScanLine;
                VDC2.ScanLine = ScanLine;

                int ActiveDisplayStartLine = VDC1.DisplayStartLine;
                int VBlankLine = ActiveDisplayStartLine + VDC1.Registers[VDW] + 1;
                if (VBlankLine > 261)
                    VBlankLine = 261;
                VDC1.ActiveLine = ScanLine - ActiveDisplayStartLine;
                VDC2.ActiveLine = VDC1.ActiveLine;
                bool InActiveDisplay = (ScanLine >= ActiveDisplayStartLine) && (ScanLine < VBlankLine);

                if (ScanLine == ActiveDisplayStartLine)
                {
                    VDC1.RCRCounter = 0x40;
                    VDC2.RCRCounter = 0x40;
                }

                if (ScanLine == VBlankLine)
                {
                    VDC1.UpdateSpriteAttributeTable();
                    VDC2.UpdateSpriteAttributeTable();
                }

                if (VDC1.RCRCounter == (VDC1.Registers[RCR] & 0x3FF))
                {
                    if (VDC1.RasterCompareInterruptEnabled)
                    {
                        VDC1.StatusByte |= VDC.StatusRasterCompare;
                        CPU.IRQ1Assert = true;
                    }
                }

                if (VDC2.RCRCounter == (VDC2.Registers[RCR] & 0x3FF))
                {
                    if (VDC2.RasterCompareInterruptEnabled)
                    {
                        VDC2.StatusByte |= VDC.StatusRasterCompare;
                        CPU.IRQ1Assert = true;
                    }
                }

                CPU.Execute(VDC1.HBlankCycles);

                if (InActiveDisplay)
                {
                    if (ScanLine == ActiveDisplayStartLine)
                    {
                        VDC1.BackgroundY = VDC1.Registers[BYR];
                        VDC2.BackgroundY = VDC2.Registers[BYR];
                    }
                    else
                    {
                        VDC1.BackgroundY++;
                        VDC1.BackgroundY &= 0x01FF;
                        VDC2.BackgroundY++;
                        VDC2.BackgroundY &= 0x01FF;
                    }
                    if (render) RenderScanLine();
                }

                if (ScanLine == VBlankLine && VDC1.VBlankInterruptEnabled)
                    VDC1.StatusByte |= VDC.StatusVerticalBlanking;

                if (ScanLine == VBlankLine && VDC2.VBlankInterruptEnabled)
                    VDC2.StatusByte |= VDC.StatusVerticalBlanking;

                if (ScanLine == VBlankLine + 4 && VDC1.SatDmaPerformed)
                {
                    VDC1.SatDmaPerformed = false;
                    if ((VDC1.Registers[DCR] & 1) > 0)
                        VDC1.StatusByte |= VDC.StatusVramSatDmaComplete;
                }

                if (ScanLine == VBlankLine + 4 && VDC2.SatDmaPerformed)
                {
                    VDC2.SatDmaPerformed = false;
                    if ((VDC2.Registers[DCR] & 1) > 0)
                        VDC2.StatusByte |= VDC.StatusVramSatDmaComplete;
                }

                CPU.Execute(2);

                if ((VDC1.StatusByte & (VDC.StatusVerticalBlanking | VDC.StatusVramSatDmaComplete)) != 0)
                    CPU.IRQ1Assert = true;

                if ((VDC2.StatusByte & (VDC.StatusVerticalBlanking | VDC.StatusVramSatDmaComplete)) != 0)
                    CPU.IRQ1Assert = true;

                CPU.Execute(455 - VDC1.HBlankCycles - 2);

                if (InActiveDisplay == false && VDC1.DmaRequested)
                    VDC1.RunDmaForScanline();

                if (InActiveDisplay == false && VDC2.DmaRequested)
                    VDC2.RunDmaForScanline();

                VDC1.RCRCounter++;
                VDC2.RCRCounter++;
                ScanLine++;

                if (ScanLine == VCE.NumberOfScanlines)
                    break;
            }
        }

        void RenderScanLine()
        {
            if (VDC1.ActiveLine >= FrameHeight)
                return;

            InitializeScanLine(VDC1.ActiveLine);

            switch (EffectivePriorityMode)
            {
                case 0:
                    RenderBackgroundScanline(VDC1, 12, PCE.CoreInputComm.PCE_ShowBG1);
                    RenderBackgroundScanline(VDC2, 2, PCE.CoreInputComm.PCE_ShowBG2);
                    RenderSpritesScanline(VDC1, 11, 14, PCE.CoreInputComm.PCE_ShowOBJ1);
                    RenderSpritesScanline(VDC2, 1, 3, PCE.CoreInputComm.PCE_ShowOBJ2);
                    break;
                case 1:
                    RenderBackgroundScanline(VDC1, 12, PCE.CoreInputComm.PCE_ShowBG1);
                    RenderBackgroundScanline(VDC2, 2, PCE.CoreInputComm.PCE_ShowBG2);
                    RenderSpritesScanline(VDC1, 11, 14, PCE.CoreInputComm.PCE_ShowOBJ1);
                    RenderSpritesScanline(VDC2, 1, 13, PCE.CoreInputComm.PCE_ShowOBJ2);
                    break;
            }
        }

        void InitializeScanLine(int scanline)
        {
            // Clear priority buffer
            Array.Clear(PriorityBuffer, 0, FrameWidth);

            // Initialize scanline to background color
            for (int i = 0; i < FrameWidth; i++)
                FrameBuffer[(scanline * FrameWidth) + i] = VCE.Palette[256];
        }

        void RenderBackgroundScanline(VDC vdc, byte priority, bool show)
        {
            if (vdc.BackgroundEnabled == false)
                return;

            int vertLine = vdc.BackgroundY;
            vertLine %= vdc.BatHeight * 8;
            int yTile = (vertLine / 8);
            int yOfs = vertLine % 8;

            int xScroll = vdc.Registers[BXR] & 0x3FF;
            for (int x = 0; x < FrameWidth; x++)
            {
                if (PriorityBuffer[x] >= priority) continue;
                int xTile = ((x + xScroll) / 8) % vdc.BatWidth;
                int xOfs = (x + xScroll) & 7;
                int tileNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] & 2047;
                int paletteNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] >> 12;
                int paletteBase = paletteNo * 16;

                byte c = vdc.PatternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
                if (c != 0)
                {
                    FrameBuffer[(vdc.ActiveLine * FrameWidth) + x] = show ? VCE.Palette[paletteBase + c] : VCE.Palette[0];
                    PriorityBuffer[x] = priority;
                }
            }
        }

        static byte[] heightTable = { 16, 32, 64, 64 };

        void RenderSpritesScanline(VDC vdc, byte lowPriority, byte highPriority, bool show)
        {
            if (vdc.SpritesEnabled == false)
                return;

            // clear inter-sprite priority buffer
            Array.Clear(InterSpritePriorityBuffer, 0, FrameWidth);

            for (int i = 0; i < 64; i++)
            {
                int y = (vdc.SpriteAttributeTable[(i * 4) + 0] & 1023) - 64;
                int x = (vdc.SpriteAttributeTable[(i * 4) + 1] & 1023) - 32;
                ushort flags = vdc.SpriteAttributeTable[(i * 4) + 3];
                int height = heightTable[(flags >> 12) & 3];

                if (y + height <= vdc.ActiveLine || y > vdc.ActiveLine)
                    continue;

                int patternNo = (((vdc.SpriteAttributeTable[(i * 4) + 2]) >> 1) & 0x1FF);
                int paletteBase = 256 + ((flags & 15) * 16);
                int width = (flags & 0x100) == 0 ? 16 : 32;
                bool priority = (flags & 0x80) != 0;
                bool hflip = (flags & 0x0800) != 0;
                bool vflip = (flags & 0x8000) != 0;

                if (width == 32)
                    patternNo &= 0x1FE;

                int yofs;
                if (vflip == false)
                {
                    yofs = (vdc.ActiveLine - y) & 15;
                    if (height == 32)
                    {
                        patternNo &= 0x1FD;
                        if (vdc.ActiveLine - y >= 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                    else if (height == 64)
                    {
                        patternNo &= 0x1F9;
                        if (vdc.ActiveLine - y >= 48)
                        {
                            y += 48;
                            patternNo += 6;
                        }
                        else if (vdc.ActiveLine - y >= 32)
                        {
                            y += 32;
                            patternNo += 4;
                        }
                        else if (vdc.ActiveLine - y >= 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                }
                else // vflip == true
                {
                    yofs = 15 - ((vdc.ActiveLine - y) & 15);
                    if (height == 32)
                    {
                        patternNo &= 0x1FD;
                        if (vdc.ActiveLine - y < 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                    else if (height == 64)
                    {
                        patternNo &= 0x1F9;
                        if (vdc.ActiveLine - y < 16)
                        {
                            y += 48;
                            patternNo += 6;
                        }
                        else if (vdc.ActiveLine - y < 32)
                        {
                            y += 32;
                            patternNo += 4;
                        }
                        else if (vdc.ActiveLine - y < 48)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                }
                if (hflip == false)
                {
                    if (x + width > 0 && y + height > 0)
                    {
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                byte myPriority = priority ? highPriority : lowPriority;
                                if (PriorityBuffer[xs] < myPriority)
                                {
                                    if (show) FrameBuffer[(vdc.ActiveLine * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
                                    PriorityBuffer[xs] = myPriority;
                                }
                            }
                        }
                    }
                    if (width == 32)
                    {
                        patternNo++;
                        x += 16;
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                byte myPriority = priority ? highPriority : lowPriority;
                                if (PriorityBuffer[xs] < myPriority)
                                {
                                    if (show) FrameBuffer[(vdc.ActiveLine * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
                                    PriorityBuffer[xs] = myPriority;
                                }
                            }
                        }
                    }
                }
                else
                { // hflip = true
                    if (x + width > 0 && y + height > 0)
                    {
                        if (width == 32)
                            patternNo++;
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                byte myPriority = priority ? highPriority : lowPriority;
                                if (PriorityBuffer[xs] < myPriority)
                                {
                                    if (show) FrameBuffer[(vdc.ActiveLine * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
                                    PriorityBuffer[xs] = myPriority;
                                }
                            }
                        }
                        if (width == 32)
                        {
                            patternNo--;
                            x += 16;
                            for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                            {
                                byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
                                if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                                {
                                    InterSpritePriorityBuffer[xs] = 1;
                                    byte myPriority = priority ? highPriority : lowPriority;
                                    if (PriorityBuffer[xs] < myPriority)
                                    {
                                        if (show) FrameBuffer[(vdc.ActiveLine * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
                                        PriorityBuffer[xs] = myPriority;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public int[] GetVideoBuffer() { return FrameBuffer; }

        public int VirtualWidth    { get { return FrameWidth; } }
        public int BufferWidth     { get { return FrameWidth; } }
        public int BufferHeight    { get { return FrameHeight; } }
        public int BackgroundColor { get { return VCE.Palette[0]; } }
    }
}
