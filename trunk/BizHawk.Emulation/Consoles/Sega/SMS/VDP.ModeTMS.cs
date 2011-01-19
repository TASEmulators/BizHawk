// Contains rendering functions for legacy TMS9918 modes.

using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class VDP
    {
        private int[] PaletteTMS9918 = new int[] 
        {
			unchecked((int)0x00000000),
			unchecked((int)0xFF000000),
			unchecked((int)0xFF47B73B),
			unchecked((int)0xFF7CCF6F),
			unchecked((int)0xFF5D4EFF),
			unchecked((int)0xFF8072FF),
			unchecked((int)0xFFB66247),
			unchecked((int)0xFF5DC8ED),
			unchecked((int)0xFFD76B48),
			unchecked((int)0xFFFB8F6C),
			unchecked((int)0xFFC3CD41),
			unchecked((int)0xFFD3DA76),
			unchecked((int)0xFF3E9F2F),
			unchecked((int)0xFFB664C7),
			unchecked((int)0xFFCCCCCC),
			unchecked((int)0xFFFFFFFF)
		};

        private void RenderBackgroundM0()
        {
            int yc = ScanLine/8;
            int yofs = ScanLine%8;
            int FrameBufferOffset = ScanLine*256;
            int PatternNameOffset = TmsPatternNameTableBase + (yc*32);
             
            for (int xc=0; xc<32; xc++)
            {
                int pn = VRAM[PatternNameOffset++];
                int pv = VRAM[PatternGeneratorBase + (pn*8) + yofs];
                int colorEntry = VRAM[ColorTableBase + (pn/8)];
                int fgColor = PaletteTMS9918[(colorEntry >> 4 & 0x0F)];
                int bgColor = PaletteTMS9918[(colorEntry & 0x0F)];
                
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x02) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x01) > 0) ? fgColor : bgColor;
            }
        }

        private void RenderBackgroundM2()
        {
            int yrow = ScanLine/8;
            int yofs = ScanLine%8;
            int FrameBufferOffset = ScanLine*256;
            int PatternNameOffset = TmsPatternNameTableBase + (yrow*32);
            int PatternGeneratorOffset = (((Registers[4] & 4) << 11) & 0x2000);// +((yrow / 8) * 0x100);
            int ColorOffset = (ColorTableBase & 0x2000);// +((yrow / 8) * 0x100);

            for (int xc=0; xc<32; xc++)
            {
                int pn = VRAM[PatternNameOffset++];
                int pv = VRAM[PatternGeneratorOffset + (pn * 8) + yofs];
                int colorEntry = VRAM[ColorOffset + (pn * 8) + yofs];
                int fgColor = PaletteTMS9918[(colorEntry >> 4 & 0x0F)];
                int bgColor = PaletteTMS9918[(colorEntry & 0x0F)];
                
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x02) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x01) > 0) ? fgColor : bgColor;
            }
        }

        private void RenderTmsSprites()
        {
            Array.Clear(ScanlinePriorityBuffer, 0, 256);
            Array.Clear(SpriteCollisionBuffer, 0, 256);

            bool Double = EnableDoubledSprites;
            bool LargeSprites = EnableLargeSprites;

            int SpriteSize = 8;
            if (LargeSprites) SpriteSize *= 2;
            if (Double) SpriteSize *= 2;
            int OneCellSize = Double ? 16 : 8;

            int NumSpritesOnScanline = 0;
            for (int i=0; i<32; i++)
            {
                int SpriteBase = TmsSpriteAttributeBase + (i*4);
                int y = VRAM[SpriteBase++];
                int x = VRAM[SpriteBase++];
                int Pattern = VRAM[SpriteBase++];
                int Color = VRAM[SpriteBase];

                if (y == 208) break; // terminator sprite
                if (y > 224) y -= 256; // sprite Y wrap
                y++; // inexplicably, sprites start on Y+1
                if (y > ScanLine || y + SpriteSize <= ScanLine) continue; // sprite is not on this scanline
                if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

                if (++NumSpritesOnScanline == 5)
                {
                    StatusByte |= (byte) i; // set 5th sprite index
                    StatusByte |= 0x40; // set overflow bit
                    break;
                }

                if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-byte alignment
                int SpriteLine = ScanLine - y;
                if (Double) SpriteLine /= 2;

                byte pv = VRAM[SpritePatternGeneratorBase + (Pattern*8) + SpriteLine];

                for (int xp = 0; xp < SpriteSize && x + xp < 256; xp++)
                {
                    if (x+xp < 0) continue;
                    if (LargeSprites && xp == OneCellSize)
                        pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

                    if ((pv & (1 << (7 - (xp & 7)))) > 0)
                    {
                        if (Color != 0)
                            FrameBuffer[(ScanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
                    }
                    
                }
            }
        }
    }
}