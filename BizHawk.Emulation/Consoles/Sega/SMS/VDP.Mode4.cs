using System;

// Contains rendering functions for TMS9918 Mode 4.

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class VDP
    {
        internal void RenderBackgroundCurrentLine(bool show)
        {
            if (DisplayOn == false)
            {
                for (int x = 0; x < 256; x++)
                    FrameBuffer[(ScanLine*256) + x] = Palette[BackdropColor];
                return;
            }

            // Clear the priority buffer for this scanline
            Array.Clear(ScanlinePriorityBuffer, 0, 256);

            int mapBase = NameTableBase;

            int vertOffset = ScanLine + Registers[9];
            if (FrameHeight == 192)
            {
                if (vertOffset >= 224)
                    vertOffset -= 224;
            }
            else
            {
                if (vertOffset >= 256)
                    vertOffset -= 256;
            }
            byte horzOffset = (HorizScrollLock && ScanLine < 16) ? (byte)0 : Registers[8];

            int yTile = vertOffset / 8;

            for (int xTile = 0; xTile < 32; xTile++)
            {
                if (xTile == 24 && VerticalScrollLock)
                {
                    vertOffset = ScanLine;
                    yTile = vertOffset / 8;
                }

                byte PaletteBase = 0;
                int tileInfo = VRAM[mapBase + ((yTile * 32) + xTile) * 2] | (VRAM[mapBase + (((yTile * 32) + xTile) * 2) + 1] << 8);
                int tileNo = tileInfo & 0x01FF;
                if ((tileInfo & 0x800) != 0)
                    PaletteBase = 16;
                bool Priority = (tileInfo & 0x1000) != 0;
                bool VFlip = (tileInfo & 0x400) != 0;
                bool HFlip = (tileInfo & 0x200) != 0;

                int yOfs = vertOffset & 7;
                if (VFlip)
                    yOfs = 7 - yOfs;

                if (HFlip == false)
                {
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor];

                    if (Priority)
                    {
                        horzOffset -= 8;
                        for (int k = 0; k < 8; k++)
                        {
                            if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
                                ScanlinePriorityBuffer[horzOffset] = 1;
                            horzOffset++;
                        }
                    }
                }
                else // Flipped Horizontally
                {
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor];

                    if (Priority)
                    {
                        horzOffset -= 8;
                        for (int k = 7; k >= 0; k--)
                        {
                            if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
                                ScanlinePriorityBuffer[horzOffset] = 1;
                            horzOffset++;
                        }
                    }
                }
            }
        }

        internal void RenderSpritesCurrentLine(bool show)
        {
            if (DisplayOn == false) return;
            int SpriteBase = SpriteAttributeTableBase;
            int SpriteHeight = EnableLargeSprites ? 16 : 8;

            // Clear the sprite collision buffer for this scanline
            Array.Clear(SpriteCollisionBuffer, 0, 256);

            // Loop through these sprites and render the current scanline
            int SpritesDrawnThisScanline = 0;
            for (int i=0; i<64; i++)
            {
                if (SpritesDrawnThisScanline >= 8)
                {
                    StatusByte |= 0x40; // Set Overflow bit
                    if (SpriteLimit) break;
                }

                int x = VRAM[SpriteBase + 0x80 + (i * 2)];
                if (ShiftSpritesLeft8Pixels)
                    x -= 8;

                int y = VRAM[SpriteBase + i] + 1;
                if (y == 209 && FrameHeight == 192) break; // 208 is special terminator sprite (in 192-line mode)
                if (y >= (EnableLargeSprites ? 240 : 248)) y -= 256;

                if (y + SpriteHeight <= ScanLine || y > ScanLine)
                    continue;

                int tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
                if (EnableLargeSprites)
                    tileNo &= 0xFE;
                tileNo += SpriteTileBase;

                int ys = ScanLine - y;

                for (int xs = 0; xs < 8 && x + xs < 256; xs++)
                {
                    byte color = PatternBuffer[(tileNo * 64) + (ys * 8) + xs];
                    if (color != 0 && x + xs >= 0)
                    {
                        if (SpriteCollisionBuffer[x + xs] != 0)
                            StatusByte |= 0x20; // Set Collision bit
                        else if (ScanlinePriorityBuffer[x + xs] == 0)
                        {
                            if (show) FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
                            SpriteCollisionBuffer[x + xs] = 1;
                        }
                    }
                }
                SpritesDrawnThisScanline++;
            }
        }

        internal void RenderSpritesCurrentLineDoubleSize(bool show)
        {
            if (DisplayOn == false) return;
            int SpriteBase = SpriteAttributeTableBase;
            int SpriteHeight = EnableLargeSprites ? 16 : 8;

            // Clear the sprite collision buffer for this scanline
            Array.Clear(SpriteCollisionBuffer, 0, 256); 

            // Loop through these sprites and render the current scanline
            int SpritesDrawnThisScanline = 0;
            for (int i = 0; i <64; i++)
            {
                if (SpritesDrawnThisScanline >= 8)
                {
                    StatusByte |= 0x40; // Set Overflow bit
                    if (SpriteLimit) break;
                }

                int x = VRAM[SpriteBase + 0x80 + (i * 2)];
                if (ShiftSpritesLeft8Pixels)
                    x -= 8;

                int y = VRAM[SpriteBase + i] + 1;
                if (y == 209 && FrameHeight == 192) break; // terminator sprite
                if (y >= (EnableLargeSprites ? 240 : 248)) y -= 256;

                if (y + (SpriteHeight*2) <= ScanLine || y > ScanLine)
                    continue;

                int tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
                if (EnableLargeSprites)
                    tileNo &= 0xFE;
                tileNo += SpriteTileBase;

                int ys = ScanLine - y;

                for (int xs = 0; xs < 16 && x + xs < 256; xs++)
                {
                    byte color = PatternBuffer[(tileNo * 64) + ((ys/2) * 8) + (xs/2)];
                    if (color != 0 && x + xs >= 0 && ScanlinePriorityBuffer[x + xs] == 0)
                    {
                        if (SpriteCollisionBuffer[x + xs] != 0)
                            StatusByte |= 0x20; // Set Collision bit
                        else
                        {
                            if (show) FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
                            SpriteCollisionBuffer[x + xs] = 1;
                        }
                    }
                }
                SpritesDrawnThisScanline++;
            }
        }

        internal void ProcessSpriteCollisionForFrameskip()
        {
            if (DisplayOn == false) return;
            int SpriteBase = SpriteAttributeTableBase;
            int SpriteHeight = EnableLargeSprites ? 16 : 8;

            // Clear the sprite collision buffer for this scanline
            Array.Clear(SpriteCollisionBuffer, 0, 256);

            // 208 is a special terminator sprite (in 192-line mode). Lets find it...
            int TerminalSprite = 64;
            if (FrameHeight == 192)
                for (int i = 0; i < 64; i++)
                {
                    if (VRAM[SpriteBase + i] == 208)
                    {
                        TerminalSprite = i;
                        break;
                    }
                }

            // Loop through these sprites and render the current scanline
            int SpritesDrawnThisScanline = 0;
            for (int i = TerminalSprite - 1; i >= 0; i--)
            {
                if (SpritesDrawnThisScanline >= 8)
                    StatusByte |= 0x40; // Set Overflow bit

                int x = VRAM[SpriteBase + 0x80 + (i * 2)];
                if (ShiftSpritesLeft8Pixels)
                    x -= 8;

                int y = VRAM[SpriteBase + i] + 1;
                if (y >= (EnableLargeSprites ? 240 : 248)) y -= 256;

                if (y + SpriteHeight <= ScanLine || y > ScanLine)
                    continue;

                int tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
                if (EnableLargeSprites)
                    tileNo &= 0xFE;
                tileNo += SpriteTileBase;

                int ys = ScanLine - y;

                for (int xs = 0; xs < 8 && x + xs < 256; xs++)
                {
                    byte color = PatternBuffer[(tileNo * 64) + (ys * 8) + xs];
                    if (color != 0 && x + xs >= 0)
                    {
                        if (SpriteCollisionBuffer[x + xs] != 0)
                            StatusByte |= 0x20; // Set Collision bit
                        SpriteCollisionBuffer[x + xs] = 1;
                    }
                }
                SpritesDrawnThisScanline++;
            }
        }

        // Performs render buffer blanking. This includes the left-column blanking as well as Game Gear blanking if requested.
        // Should be called at the end of the frame.
        internal void RenderBlankingRegions()
        {
            int blankingColor = Palette[BackdropColor];

            if (LeftBlanking)
            {
                for (int y = 0; y < FrameHeight; y++)
                {
                    for (int x = 0; x < 8; x++)
                        FrameBuffer[(y * 256) + x] = blankingColor;
                }
            }

            if (mode == VdpMode.GameGear)
            {
                if (Sms.CoreInputComm.GG_ShowClippedRegions == false) 
                {
                    int yStart = (FrameHeight - 144)/2;
                    for (int y = 0; y < 144; y++)
                        for (int x = 0; x < 160; x++)
                            GameGearFrameBuffer[(y * 160) + x] = FrameBuffer[((y + yStart) * 256) + x + 48];
                }

                if (Sms.CoreInputComm.GG_HighlightActiveDisplayRegion && Sms.CoreInputComm.GG_ShowClippedRegions)
                {
                    // Top 24 scanlines
                    for (int y = 0; y < 24; y++)
                    {
                        for (int x = 0; x < 256; x++)
                        {
                            int frameOffset = (y * 256) + x;
                            int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
                            FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
                        }
                    }

                    // Bottom 24 scanlines
                    for (int y = 168; y < 192; y++)
                    {
                        for (int x = 0; x < 256; x++)
                        {
                            int frameOffset = (y * 256) + x;
                            int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
                            FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
                        }
                    }

                    // Left 48 pixels
                    for (int y = 24; y < 168; y++)
                    {
                        for (int x = 0; x < 48; x++)
                        {
                            int frameOffset = (y * 256) + x;
                            int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
                            FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
                        }
                    }

                    // Right 48 pixels
                    for (int y = 24; y < 168; y++)
                    {
                        for (int x = 208; x < 256; x++)
                        {
                            int frameOffset = (y * 256) + x;
                            int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
                            FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
                        }
                    }
                }
            }
        }
    }
}
