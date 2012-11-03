using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    public enum VicIIMode
    {
        NTSC,
        PAL
    }

    public class VicII
    {
        // buffer
        public int[] buffer;
        public int bufferSize;

        // palette
        public int[] palette =
        {
            Colors.ARGB(0x00, 0x00, 0x00),
            Colors.ARGB(0xFF, 0xFF, 0xFF),
            Colors.ARGB(0x68, 0x37, 0x2B),
            Colors.ARGB(0x70, 0xA4, 0xB2),
            Colors.ARGB(0x6F, 0x3D, 0x86),
            Colors.ARGB(0x58, 0x8D, 0x43),
            Colors.ARGB(0x35, 0x28, 0x79),
            Colors.ARGB(0xB8, 0xC7, 0x6F),
            Colors.ARGB(0x6F, 0x4F, 0x25),
            Colors.ARGB(0x43, 0x39, 0x00),
            Colors.ARGB(0x9A, 0x67, 0x59),
            Colors.ARGB(0x44, 0x44, 0x44),
            Colors.ARGB(0x6C, 0x6C, 0x6C),
            Colors.ARGB(0x9A, 0xD2, 0x84),
            Colors.ARGB(0x6C, 0x5E, 0xB5),
            Colors.ARGB(0x95, 0x95, 0x95)
        };

        // interrupts
        public bool interrupt = true;
        public bool lightPenInterrupt = true;
        public bool lightPenInterruptEnabled;
        public bool rasterInterrupt = true;
        public bool rasterInterruptEnabled;
        public bool spriteBackgroundInterrupt = true;
        public bool spriteBackgroundInterruptEnabled;
        public bool spriteSpriteInterrupt = true;
        public bool spriteSpriteInterruptEnabled;

        // memory
        public bool characterFetch;
        public int characterFetchOffset;
        public int characterMemoryOffset;
        public int screenMemoryOffset;

        // lightpen
        public int lightPenX;
        public int lightPenY;

        // raster
        public int[] backgroundColor;
        public bool backgroundMode;
        public bool bitmapMode;
        public int borderBottom;
        public int borderColor;
        public int borderLeft;
        public bool borderOnHorizontal;
        public bool borderOnVertical;
        public int borderRight;
        public int borderTop;
        public byte[] charBuffer;
        public bool extendHeight;
        public bool extendWidth;
        public int horizontalScroll;
        public bool multiColorMode;
        public int rasterInterruptLine;
        public int rasterLineLeft;
        public int rasterOffset;
        public int rasterOffsetX;
        public int rasterOffsetY;
        public int rasterTotalLines;
        public int rasterWidth;
        public int renderOffset;
        public bool screenEnabled;
        public int verticalScroll;
        public int visibleBottom;
        public int visibleHeight;
        public int visibleLeft;
        public bool visibleRenderX;
        public bool visibleRenderY;
        public int visibleRight;
        public int visibleTop;
        public int visibleWidth;

        // sprites
        public bool[] spriteBackgroundCollision;
        public bool[] spriteCollision;
        public int[] spriteColor;
        public bool[] spriteEnabled;
        public int[] spriteExtraColor;
        public bool[] spriteMultiColor;
        public bool[] spritePriority;
        public bool[] spriteStretchHorizontal;
        public bool[] spriteStretchVertical;
        public int[] spriteX;
        public int[] spriteY;

        public VicSignals cpuSignal;
        public byte[] regs;

        public VicII(VicSignals signals, VicIIMode videoMode)
        {
            cpuSignal = signals;

            switch (videoMode)
            {
                case VicIIMode.NTSC:
                    rasterWidth = 512;
                    rasterTotalLines = 263;
                    rasterLineLeft = 0x19C;
                    visibleLeft = 0x1E9;
                    visibleRight = 0x18B;
                    visibleTop = 0x41;
                    visibleBottom = 0x13;
                    visibleRenderX = false;
                    visibleRenderY = true;
                    visibleWidth = 418;
                    visibleHeight = 217;
                    renderOffset = 0;
                    borderLeft = 0x018;
                    borderRight = 0x158;
                    borderTop = 0x033;
                    borderBottom = 0x0FA;
                    break;
                case VicIIMode.PAL:
                    break;
                default:
                    break;
            }

            // initialize raster
            backgroundColor = new int[4];
            charBuffer = new byte[40];
            rasterOffsetX = rasterLineLeft;
            rasterOffsetY = 0;

            // initialize sprites
            spriteBackgroundCollision = new bool[8];
            spriteCollision = new bool[8];
            spriteColor = new int[8];
            spriteEnabled = new bool[8];
            spriteExtraColor = new int[2];
            spriteMultiColor = new bool[8];
            spritePriority = new bool[8];
            spriteStretchHorizontal = new bool[8];
            spriteStretchVertical = new bool[8];
            spriteX = new int[8];
            spriteY = new int[8];

            // initialize buffer
            buffer = new int[rasterWidth * rasterTotalLines];
            bufferSize = buffer.Length;

            // initialize registers
            HardReset();
        }

        public void HardReset()
        {
            // power on state
            regs = new byte[0x40];
            Write(0x0016, 0xC0);
            Write(0x0018, 0x01);
            Write(0x0019, 0x71);
            Write(0x001A, 0xF0);
            for (ushort i = 0x0020; i <= 0x002E; i++)
                Write(i, 0xF0);

            // unused registers always return FF
            for (int i = 0x2F; i <= 0x3F; i++)
                regs[i] = 0xFF; 

            UpdateRegs();
        }

        public void LockBus()
        {
            cpuSignal.Lock();
        }

        public void PerformCycle()
        {
            for (int i = 0; i < 8; i++)
            {
                if (rasterOffsetX == visibleLeft)
                    visibleRenderX = true;
                if (rasterOffsetX == visibleRight)
                    visibleRenderX = false;
                if (rasterOffsetX == borderLeft)
                    borderOnHorizontal = false;
                if (rasterOffsetX == borderRight)
                    borderOnHorizontal = true;

                if (borderOnVertical || borderOnHorizontal)
                {
                    WritePixel(borderColor);
                }
                else
                {
                    WritePixel(backgroundColor[0]);
                }

                rasterOffsetX++;
                if (rasterOffsetX == rasterWidth)
                    rasterOffsetX = 0;

                if (rasterOffsetX == rasterLineLeft)
                {
                    rasterOffsetY++;

                    if (rasterOffsetY == visibleTop)
                        visibleRenderY = true;
                    if (rasterOffsetY == visibleBottom)
                        visibleRenderY = false;
                    if (rasterOffsetY == borderTop)
                        borderOnVertical = false;
                    if (rasterOffsetY == borderBottom)
                        borderOnVertical = true;
                    if (rasterOffsetY == rasterTotalLines)
                    {
                        rasterOffsetY = 0;
                        renderOffset = 0;
                    }

                    if (rasterInterruptEnabled && (rasterOffsetY == rasterInterruptLine))
                    {
                        rasterInterrupt = true;
                    }
                }
            }


            interrupt = 
                (rasterInterrupt & rasterInterruptEnabled) |
                (spriteSpriteInterrupt & spriteSpriteInterruptEnabled) |
                (spriteBackgroundInterrupt & spriteBackgroundInterruptEnabled) |
                (lightPenInterrupt & lightPenInterruptEnabled);

            cpuSignal.Interrupt = interrupt;
            UpdateRegs();
        }

        public byte Read(ushort addr)
        {
            return regs[addr & 0x3F];
        }

        public void UnlockBus()
        {
            cpuSignal.Unlock();
        }

        public void UpdateRegs()
        {
            // these registers update on their own

            regs[0x11] = (byte)
                ((verticalScroll & 0x07) |
                (extendHeight ? 0x08 : 0x00) |
                (screenEnabled ? 0x10 : 0x00) |
                (bitmapMode ? 0x20 : 0x00) |
                (backgroundMode ? 0x40 : 0x00) |
                ((rasterOffsetY & 0x100) >> 1));
            regs[0x12] = (byte)(rasterOffsetY & 0xFF);
            regs[0x13] = (byte)(lightPenX >> 1);
            regs[0x14] = (byte)(lightPenY);
            regs[0x19] = (byte)
                ((rasterInterrupt ? 0x01 : 0x00) |
                (spriteBackgroundInterrupt ? 0x02 : 0x00) |
                (spriteSpriteInterrupt ? 0x04 : 0x00) |
                (lightPenInterrupt ? 0x08 : 0x00) |
                (interrupt ? 0x80 : 0x00));
        }

        public void Write(ushort addr, byte val)
        {
            int index = 0;
            bool allowWrite = true;
            addr &= 0x3F;

            switch (addr)
            {
                case 0x00:
                case 0x02:
                case 0x04:
                case 0x06:
                case 0x08:
                case 0x0A:
                case 0x0C:
                case 0x0E:
                    index = addr >> 1;
                    spriteX[index] &= 0xFF;
                    spriteX[index] |= val;
                    break;
                case 0x01:
                case 0x03:
                case 0x05:
                case 0x07:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                    index = addr >> 1;
                    spriteY[index] &= 0xFF;
                    spriteY[index] |= val;
                    break;
                case 0x10:
                    spriteX[0] = (spriteX[0] & 0xFF) | ((val & 0x01) << 8);
                    spriteX[1] = (spriteX[1] & 0xFF) | ((val & 0x02) << 7);
                    spriteX[2] = (spriteX[2] & 0xFF) | ((val & 0x04) << 6);
                    spriteX[3] = (spriteX[3] & 0xFF) | ((val & 0x08) << 5);
                    spriteX[4] = (spriteX[4] & 0xFF) | ((val & 0x10) << 4);
                    spriteX[5] = (spriteX[5] & 0xFF) | ((val & 0x20) << 3);
                    spriteX[6] = (spriteX[6] & 0xFF) | ((val & 0x40) << 2);
                    spriteX[7] = (spriteX[7] & 0xFF) | ((val & 0x80) << 1);
                    break;
                case 0x11:
                    verticalScroll = val & 0x07;
                    extendHeight = ((val & 0x08) != 0x00);
                    screenEnabled = ((val & 0x10) != 0x00);
                    bitmapMode = ((val & 0x20) != 0x00);
                    backgroundMode = ((val & 0x40) != 0x00);
                    rasterInterruptLine = (rasterInterruptLine & 0xFF) | ((val & 0x80) << 1);
                    val = (byte)((val & 0x7F) | ((rasterOffsetY & 0x100) >> 1));
                    break;
                case 0x12:
                    rasterInterruptLine = (rasterInterruptLine & 0x100) | val;
                    allowWrite = false;
                    break;
                case 0x15:
                    spriteEnabled[0] = ((val & 0x01) != 0x00);
                    spriteEnabled[1] = ((val & 0x02) != 0x00);
                    spriteEnabled[2] = ((val & 0x04) != 0x00);
                    spriteEnabled[3] = ((val & 0x08) != 0x00);
                    spriteEnabled[4] = ((val & 0x10) != 0x00);
                    spriteEnabled[5] = ((val & 0x20) != 0x00);
                    spriteEnabled[6] = ((val & 0x40) != 0x00);
                    spriteEnabled[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x16:
                    horizontalScroll = val & 0x07;
                    extendWidth = ((val & 0x08) != 0x00);
                    multiColorMode = ((val & 0x10) != 0x00);
                    bitmapMode = ((val & 0x20) != 0x00);
                    val |= 0xC0;
                    break;
                case 0x17:
                    spriteStretchVertical[0] = ((val & 0x01) != 0x00);
                    spriteStretchVertical[1] = ((val & 0x02) != 0x00);
                    spriteStretchVertical[2] = ((val & 0x04) != 0x00);
                    spriteStretchVertical[3] = ((val & 0x08) != 0x00);
                    spriteStretchVertical[4] = ((val & 0x10) != 0x00);
                    spriteStretchVertical[5] = ((val & 0x20) != 0x00);
                    spriteStretchVertical[6] = ((val & 0x40) != 0x00);
                    spriteStretchVertical[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x18:
                    characterMemoryOffset = (int)(val & 0x0E) << 10;
                    screenMemoryOffset = (int)(val & 0xF0) << 6;
                    break;
                case 0x19:
                    rasterInterrupt = ((val & 0x01) != 0);
                    spriteSpriteInterrupt = ((val & 0x02) != 0);
                    spriteBackgroundInterrupt = ((val & 0x04) != 0);
                    lightPenInterrupt = ((val & 0x08) != 0);
                    allowWrite = false;
                    break;
                case 0x1A:
                    rasterInterruptEnabled = ((val & 0x01) != 0);
                    spriteSpriteInterruptEnabled = ((val & 0x02) != 0);
                    spriteBackgroundInterruptEnabled = ((val & 0x04) != 0);
                    lightPenInterruptEnabled = ((val & 0x08) != 0);
                    break;
                case 0x1B:
                    spritePriority[0] = ((val & 0x01) != 0x00);
                    spritePriority[1] = ((val & 0x02) != 0x00);
                    spritePriority[2] = ((val & 0x04) != 0x00);
                    spritePriority[3] = ((val & 0x08) != 0x00);
                    spritePriority[4] = ((val & 0x10) != 0x00);
                    spritePriority[5] = ((val & 0x20) != 0x00);
                    spritePriority[6] = ((val & 0x40) != 0x00);
                    spritePriority[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x1C:
                    spriteMultiColor[0] = ((val & 0x01) != 0x00);
                    spriteMultiColor[1] = ((val & 0x02) != 0x00);
                    spriteMultiColor[2] = ((val & 0x04) != 0x00);
                    spriteMultiColor[3] = ((val & 0x08) != 0x00);
                    spriteMultiColor[4] = ((val & 0x10) != 0x00);
                    spriteMultiColor[5] = ((val & 0x20) != 0x00);
                    spriteMultiColor[6] = ((val & 0x40) != 0x00);
                    spriteMultiColor[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x1D:
                    spriteStretchHorizontal[0] = ((val & 0x01) != 0x00);
                    spriteStretchHorizontal[1] = ((val & 0x02) != 0x00);
                    spriteStretchHorizontal[2] = ((val & 0x04) != 0x00);
                    spriteStretchHorizontal[3] = ((val & 0x08) != 0x00);
                    spriteStretchHorizontal[4] = ((val & 0x10) != 0x00);
                    spriteStretchHorizontal[5] = ((val & 0x20) != 0x00);
                    spriteStretchHorizontal[6] = ((val & 0x40) != 0x00);
                    spriteStretchHorizontal[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x1E:
                    spriteCollision[0] = ((val & 0x01) != 0x00);
                    spriteCollision[1] = ((val & 0x02) != 0x00);
                    spriteCollision[2] = ((val & 0x04) != 0x00);
                    spriteCollision[3] = ((val & 0x08) != 0x00);
                    spriteCollision[4] = ((val & 0x10) != 0x00);
                    spriteCollision[5] = ((val & 0x20) != 0x00);
                    spriteCollision[6] = ((val & 0x40) != 0x00);
                    spriteCollision[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x1F:
                    spriteBackgroundCollision[0] = ((val & 0x01) != 0x00);
                    spriteBackgroundCollision[1] = ((val & 0x02) != 0x00);
                    spriteBackgroundCollision[2] = ((val & 0x04) != 0x00);
                    spriteBackgroundCollision[3] = ((val & 0x08) != 0x00);
                    spriteBackgroundCollision[4] = ((val & 0x10) != 0x00);
                    spriteBackgroundCollision[5] = ((val & 0x20) != 0x00);
                    spriteBackgroundCollision[6] = ((val & 0x40) != 0x00);
                    spriteBackgroundCollision[7] = ((val & 0x80) != 0x00);
                    break;
                case 0x20:
                    borderColor = val;
                    break;
                case 0x21:
                    backgroundColor[0] = val;
                    break;
                case 0x22:
                    backgroundColor[1] = val;
                    break;
                case 0x23:
                    backgroundColor[2] = val;
                    break;
                case 0x24:
                    backgroundColor[3] = val;
                    break;
                case 0x25:
                    spriteExtraColor[0] = val;
                    break;
                case 0x26:
                    spriteExtraColor[1] = val;
                    break;
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                    index = addr - 0x27;
                    spriteColor[index] = val;
                    break;
                default:
                    allowWrite = false;
                    break;
            }

            if (allowWrite)
                regs[addr] = val;
        }

        private void WritePixel(int value)
        {
            if (visibleRenderX && visibleRenderY)
            {
                value &= 0x0F;
                buffer[renderOffset++] = palette[value];
            }
        }
    }

    public class VicSignals
    {
        public bool AllowCpu;
        public bool Interrupt;
        public int LockCounter;

        public VicSignals()
        {
            AllowCpu = true;
            Interrupt = false;
            LockCounter = 0;
        }

        public void Lock()
        {
            if (AllowCpu)
            {
                LockCounter = 4;
            }
        }

        public void PerformCycle()
        {
            if (AllowCpu)
            {
                if (LockCounter > 0)
                {
                    LockCounter--;
                    if (LockCounter == 0)
                    {
                        AllowCpu = false;
                    }
                }
            }
        }

        public void Unlock()
        {
            AllowCpu = true;
        }

    }
}
