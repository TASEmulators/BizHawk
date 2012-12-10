namespace GarboDev
{
    partial class Renderer
    {
        #region Sprite Drawing
        private void DrawSpritesNormal(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBlend(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBrightInc(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * this.blendY) >> 4);
                                        g = g + (((0xFF - g) * this.blendY) >> 4);
                                        b = b + (((0xFF - b) * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * this.blendY) >> 4);
                                        g = g + (((0xFF - g) * this.blendY) >> 4);
                                        b = b + (((0xFF - b) * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * this.blendY) >> 4);
                                        g = g + (((0xFF - g) * this.blendY) >> 4);
                                        b = b + (((0xFF - b) * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * this.blendY) >> 4);
                                        g = g + (((0xFF - g) * this.blendY) >> 4);
                                        b = b + (((0xFF - b) * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBrightDec(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                        {
                                            uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                            uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                            uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                            uint sourceValue = this.scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * this.blendY) >> 4);
                                        g = g - ((g * this.blendY) >> 4);
                                        b = b - ((b * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * this.blendY) >> 4);
                                        g = g - ((g * this.blendY) >> 4);
                                        b = b - ((b * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * this.blendY) >> 4);
                                        g = g - ((g * this.blendY) >> 4);
                                        b = b - ((b * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        uint r = pixelColor & 0xFF;
                                        uint g = (pixelColor >> 8) & 0xFF;
                                        uint b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * this.blendY) >> 4);
                                        g = g - ((g * this.blendY) >> 4);
                                        b = b - ((b * this.blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindow(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBlend(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBrightInc(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * this.blendY) >> 4);
                                            g = g + (((0xFF - g) * this.blendY) >> 4);
                                            b = b + (((0xFF - b) * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * this.blendY) >> 4);
                                            g = g + (((0xFF - g) * this.blendY) >> 4);
                                            b = b + (((0xFF - b) * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * this.blendY) >> 4);
                                            g = g + (((0xFF - g) * this.blendY) >> 4);
                                            b = b + (((0xFF - b) * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * this.blendY) >> 4);
                                            g = g + (((0xFF - g) * this.blendY) >> 4);
                                            b = b + (((0xFF - b) * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBrightDec(int priority)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            byte blendMaskType = (byte)(1 << 4);

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

                bool semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
                }
                else
                {
                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
                }

                int scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                int spritey = this.curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((this.blend[i & 0x1ff] & this.blendTarget) != 0 && this.blend[i & 0x1ff] != blendMaskType)
                                            {
                                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                                uint sourceValue = this.scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * this.blendY) >> 4);
                                            g = g - ((g * this.blendY) >> 4);
                                            b = b - ((b * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * this.blendY) >> 4);
                                            g = g - ((g * this.blendY) >> 4);
                                            b = b - ((b * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        int rotScaleParam = (attr1 >> 9) & 0x1F;

                        short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        int cx = rwidth / 2;
                        int cy = rheight / 2;

                        int baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((this.dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        int rx = (int)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                        int ry = (int)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * this.blendY) >> 4);
                                            g = g - ((g * this.blendY) >> 4);
                                            b = b - ((b * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (int i = x; i < x + rwidth; i++)
                            {
                                int tx = rx >> 8;
                                int ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (this.windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((this.windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            uint r = pixelColor & 0xFF;
                                            uint g = (pixelColor >> 8) & 0xFF;
                                            uint b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * this.blendY) >> 4);
                                            g = g - ((g * this.blendY) >> 4);
                                            b = b - ((b * this.blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        this.scanline[(i & 0x1ff)] = pixelColor;
                                        this.blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        #endregion Sprite Drawing
        #region Rot/Scale Bg
        private void RenderRotScaleBgNormal(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if (true)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBlend(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if (true)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.blend[i] & this.blendTarget) != 0)
                            {
                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                uint sourceValue = this.scanline[i];
                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBrightInc(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if (true)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * this.blendY) >> 4);
                            g = g + (((0xFF - g) * this.blendY) >> 4);
                            b = b + (((0xFF - b) * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBrightDec(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if (true)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * this.blendY) >> 4);
                            g = g - ((g * this.blendY) >> 4);
                            b = b - ((b * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindow(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if ((this.windowCover[i] & (1 << bg)) != 0)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBlend(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if ((this.windowCover[i] & (1 << bg)) != 0)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((this.blend[i] & this.blendTarget) != 0)
                                {
                                    uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                    uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                    uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                    uint sourceValue = this.scanline[i];
                                    r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBrightInc(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if ((this.windowCover[i] & (1 << bg)) != 0)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * this.blendY) >> 4);
                                g = g + (((0xFF - g) * this.blendY) >> 4);
                                b = b + (((0xFF - b) * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBrightDec(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int x = this.memory.Bgx[bg - 2];
            int y = this.memory.Bgy[bg - 2];

            short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            bool transparent = (bgcnt & (1 << 13)) == 0;

            for (int i = 0; i < 240; i++)
            {
                if ((this.windowCover[i] & (1 << bg)) != 0)
                {
                    int ax = x >> 8;
                    int ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        int tmpTileIdx = (int)(screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8));
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * this.blendY) >> 4);
                                g = g - ((g * this.blendY) >> 4);
                                b = b - ((b * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        #endregion Rot/Scale Bg
        #region Text Bg
        private void RenderTextBgNormal(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBlend(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.blend[i] & this.blendTarget) != 0)
                            {
                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                uint sourceValue = this.scanline[i];
                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((this.blend[i] & this.blendTarget) != 0)
                            {
                                uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                uint sourceValue = this.scanline[i];
                                r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBrightInc(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * this.blendY) >> 4);
                            g = g + (((0xFF - g) * this.blendY) >> 4);
                            b = b + (((0xFF - b) * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * this.blendY) >> 4);
                            g = g + (((0xFF - g) * this.blendY) >> 4);
                            b = b + (((0xFF - b) * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBrightDec(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * this.blendY) >> 4);
                            g = g - ((g * this.blendY) >> 4);
                            b = b - ((b * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            uint r = pixelColor & 0xFF;
                            uint g = (pixelColor >> 8) & 0xFF;
                            uint b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * this.blendY) >> 4);
                            g = g - ((g * this.blendY) >> 4);
                            b = b - ((b * this.blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindow(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBlend(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((this.blend[i] & this.blendTarget) != 0)
                                {
                                    uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                    uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                    uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                    uint sourceValue = this.scanline[i];
                                    r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((this.blend[i] & this.blendTarget) != 0)
                                {
                                    uint r = ((pixelColor & 0xFF) * this.blendA) >> 4;
                                    uint g = (((pixelColor >> 8) & 0xFF) * this.blendA) >> 4;
                                    uint b = (((pixelColor >> 16) & 0xFF) * this.blendA) >> 4;
                                    uint sourceValue = this.scanline[i];
                                    r += ((sourceValue & 0xFF) * this.blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * this.blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * this.blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBrightInc(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * this.blendY) >> 4);
                                g = g + (((0xFF - g) * this.blendY) >> 4);
                                b = b + (((0xFF - b) * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * this.blendY) >> 4);
                                g = g + (((0xFF - g) * this.blendY) >> 4);
                                b = b + (((0xFF - b) * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBrightDec(int bg)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            byte blendMaskType = (byte)(1 << bg);

            ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            int screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            int charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            int hofs = Memory.ReadU16(this.memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            int vofs = Memory.ReadU16(this.memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 8;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * this.blendY) >> 4);
                                g = g - ((g * this.blendY) >> 4);
                                b = b - ((b * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                int bgy = ((this.curLine + vofs) & (height - 1)) / 8;

                int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                int tileY = ((this.curLine + vofs) & 0x7) * 4;

                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << bg)) != 0)
                    {
                        int bgx = ((i + hofs) & (width - 1)) / 8;
                        int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        int tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        int x = (i + hofs) & 7;
                        int y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            int palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            uint pixelColor = Renderer.GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((this.windowCover[i] & (1 << 5)) != 0)
                            {
                                uint r = pixelColor & 0xFF;
                                uint g = (pixelColor >> 8) & 0xFF;
                                uint b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * this.blendY) >> 4);
                                g = g - ((g * this.blendY) >> 4);
                                b = b - ((b * this.blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            this.scanline[i] = pixelColor; this.blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        #endregion Text Bg
    }
}
