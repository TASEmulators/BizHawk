namespace GarboDev
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public partial class Renderer : IRenderer
    {
        private Memory memory;
        private uint[] scanline = new uint[240];
        private byte[] blend = new byte[240];
        private byte[] windowCover = new byte[240];
        private uint[] back = new uint[240 * 160];
        //private uint[] front = new uint[240 * 160];
        private const uint pitch = 240;

        // Convenience variable as I use it everywhere, set once in RenderLine
        private ushort dispCnt;

        // Window helper variables
        private byte win0x1, win0x2, win0y1, win0y2;
        private byte win1x1, win1x2, win1y1, win1y2;
        private byte win0Enabled, win1Enabled, winObjEnabled, winOutEnabled;
        private bool winEnabled;

        private byte blendSource, blendTarget;
        private byte blendA, blendB, blendY;
        private int blendType;

        private int curLine = 0;

        private static uint[] colorLUT;

        static Renderer()
        {
            colorLUT = new uint[0x10000];
            // Pre-calculate the color LUT
            for (uint i = 0; i <= 0xFFFF; i++)
            {
                uint r = (i & 0x1FU);
                uint g = (i & 0x3E0U) >> 5;
                uint b = (i & 0x7C00U) >> 10;
                r = (r << 3) | (r >> 2);
                g = (g << 3) | (g >> 2);
                b = (b << 3) | (b >> 2);
                colorLUT[i] = (r << 16) | (g << 8) | b;
            }
        }

        public Memory Memory
        {
            set { this.memory = value; }
        }

        public void Initialize(object data)
        {
        }

        public void Reset()
        {
        }

        public uint[] ShowFrame()
        {
            //Array.Copy(this.back, this.front, this.front.Length);

            //return this.front;
			return this.back;
        }

        public void RenderLine(int line)
        {
            this.curLine = line;

            // Render the line
            this.dispCnt = Memory.ReadU16(this.memory.IORam, Memory.DISPCNT);

            if ((this.dispCnt & (1 << 7)) != 0)
            {
                uint bgColor = Renderer.GbaTo32((ushort)0x7FFF);
                for (int i = 0; i < 240; i++) this.scanline[i] = bgColor;
            }
            else
            {
                this.winEnabled = false;

                if ((this.dispCnt & (1 << 13)) != 0)
                {
                    // Calculate window 0 information
                    ushort winy = Memory.ReadU16(this.memory.IORam, Memory.WIN0V);
                    this.win0y1 = (byte)(winy >> 8);
                    this.win0y2 = (byte)(winy & 0xff);
                    ushort winx = Memory.ReadU16(this.memory.IORam, Memory.WIN0H);
                    this.win0x1 = (byte)(winx >> 8);
                    this.win0x2 = (byte)(winx & 0xff);

                    if (this.win0x2 > 240 || this.win0x1 > this.win0x2)
                    {
                        this.win0x2 = 240;
                    }

                    if (this.win0y2 > 160 || this.win0y1 > this.win0y2)
                    {
                        this.win0y2 = 160;
                    }

                    this.win0Enabled = this.memory.IORam[Memory.WININ];
                    this.winEnabled = true;
                }

                if ((this.dispCnt & (1 << 14)) != 0)
                {
                    // Calculate window 1 information
                    ushort winy = Memory.ReadU16(this.memory.IORam, Memory.WIN1V);
                    this.win1y1 = (byte)(winy >> 8);
                    this.win1y2 = (byte)(winy & 0xff);
                    ushort winx = Memory.ReadU16(this.memory.IORam, Memory.WIN1H);
                    this.win1x1 = (byte)(winx >> 8);
                    this.win1x2 = (byte)(winx & 0xff);

                    if (this.win1x2 > 240 || this.win1x1 > this.win1x2)
                    {
                        this.win1x2 = 240;
                    }

                    if (this.win1y2 > 160 || this.win1y1 > this.win1y2)
                    {
                        this.win1y2 = 160;
                    }

                    this.win1Enabled = this.memory.IORam[Memory.WININ + 1];
                    this.winEnabled = true;
                }

                if ((this.dispCnt & (1 << 15)) != 0 && (this.dispCnt & (1 << 12)) != 0)
                {
                    // Object windows are enabled
                    this.winObjEnabled = this.memory.IORam[Memory.WINOUT + 1];
                    this.winEnabled = true;
                }

                if (this.winEnabled)
                {
                    this.winOutEnabled = this.memory.IORam[Memory.WINOUT];
                }

                // Calculate blending information
                ushort bldcnt = Memory.ReadU16(this.memory.IORam, Memory.BLDCNT);
                this.blendType = (bldcnt >> 6) & 0x3;
                this.blendSource = (byte)(bldcnt & 0x3F);
                this.blendTarget = (byte)((bldcnt >> 8) & 0x3F);

                ushort bldalpha = Memory.ReadU16(this.memory.IORam, Memory.BLDALPHA);
                this.blendA = (byte)(bldalpha & 0x1F);
                if (this.blendA > 0x10) this.blendA = 0x10;
                this.blendB = (byte)((bldalpha >> 8) & 0x1F);
                if (this.blendB > 0x10) this.blendB = 0x10;

                this.blendY = (byte)(this.memory.IORam[Memory.BLDY] & 0x1F);
                if (this.blendY > 0x10) this.blendY = 0x10;

                switch (this.dispCnt & 0x7)
                {
                    case 0: this.RenderMode0Line(); break;
                    case 1: this.RenderMode1Line(); break;
                    case 2: this.RenderMode2Line(); break;
                    case 3: this.RenderMode3Line(); break;
                    case 4: this.RenderMode4Line(); break;
                    case 5: this.RenderMode5Line(); break;
                }
            }

            Array.Copy(this.scanline, 0, this.back, this.curLine * Renderer.pitch, Renderer.pitch);
        }

        private void DrawBackdrop()
        {
            byte[] palette = this.memory.PaletteRam;

            // Initialize window coverage buffer if neccesary
            if (this.winEnabled)
            {
                for (int i = 0; i < 240; i++)
                {
                    this.windowCover[i] = this.winOutEnabled;
                }

                if ((this.dispCnt & (1 << 15)) != 0)
                {
                    // Sprite window
                    this.DrawSpriteWindows();
                }

                if ((this.dispCnt & (1 << 14)) != 0)
                {
                    // Window 1
                    if (this.curLine >= this.win1y1 && this.curLine < this.win1y2)
                    {
                        for (int i = this.win1x1; i < this.win1x2; i++)
                        {
                            this.windowCover[i] = this.win1Enabled;
                        }
                    }
                }

                if ((this.dispCnt & (1 << 13)) != 0)
                {
                    // Window 0
                    if (this.curLine >= this.win0y1 && this.curLine < this.win0y2)
                    {
                        for (int i = this.win0x1; i < this.win0x2; i++)
                        {
                            this.windowCover[i] = this.win0Enabled;
                        }
                    }
                }
            }

            // Draw backdrop first
            uint bgColor = Renderer.GbaTo32((ushort)(palette[0] | (palette[1] << 8)));
            uint modColor = bgColor;

            if (this.blendType == 2 && (this.blendSource & (1 << 5)) != 0)
            {
                // Brightness increase
                uint r = bgColor & 0xFF;
                uint g = (bgColor >> 8) & 0xFF;
                uint b = (bgColor >> 16) & 0xFF;
                r = r + (((0xFF - r) * this.blendY) >> 4);
                g = g + (((0xFF - g) * this.blendY) >> 4);
                b = b + (((0xFF - b) * this.blendY) >> 4);
                modColor = r | (g << 8) | (b << 16);
            }
            else if (this.blendType == 3 && (this.blendSource & (1 << 5)) != 0)
            {
                // Brightness decrease
                uint r = bgColor & 0xFF;
                uint g = (bgColor >> 8) & 0xFF;
                uint b = (bgColor >> 16) & 0xFF;
                r = r - ((r * this.blendY) >> 4);
                g = g - ((g * this.blendY) >> 4);
                b = b - ((b * this.blendY) >> 4);
                modColor = r | (g << 8) | (b << 16);
            }

            if (this.winEnabled)
            {
                for (int i = 0; i < 240; i++)
                {
                    if ((this.windowCover[i] & (1 << 5)) != 0)
                    {
                        this.scanline[i] = modColor;
                    }
                    else
                    {
                        this.scanline[i] = bgColor;
                    }
                    this.blend[i] = 1 << 5;
                }
            }
            else
            {
                for (int i = 0; i < 240; i++)
                {
                    this.scanline[i] = modColor;
                    this.blend[i] = 1 << 5;
                }
            }
        }

        private void RenderTextBg(int bg)
        {
            if (this.winEnabled)
            {
                switch (this.blendType)
                {
                    case 0:
                        this.RenderTextBgWindow(bg);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgWindowBlend(bg);
                        else
                            this.RenderTextBgWindow(bg);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgWindowBrightInc(bg);
                        else
                            this.RenderTextBgWindow(bg);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgWindowBrightDec(bg);
                        else
                            this.RenderTextBgWindow(bg);
                        break;
                }
            }
            else
            {
                switch (this.blendType)
                {
                    case 0:
                        this.RenderTextBgNormal(bg);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgBlend(bg);
                        else
                            this.RenderTextBgNormal(bg);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgBrightInc(bg);
                        else
                            this.RenderTextBgNormal(bg);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderTextBgBrightDec(bg);
                        else
                            this.RenderTextBgNormal(bg);
                        break;
                }
            }
        }

        private void RenderRotScaleBg(int bg)
        {
            if (this.winEnabled)
            {
                switch (this.blendType)
                {
                    case 0:
                        this.RenderRotScaleBgWindow(bg);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgWindowBlend(bg);
                        else
                            this.RenderRotScaleBgWindow(bg);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgWindowBrightInc(bg);
                        else
                            this.RenderRotScaleBgWindow(bg);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgWindowBrightDec(bg);
                        else
                            this.RenderRotScaleBgWindow(bg);
                        break;
                }
            }
            else
            {
                switch (this.blendType)
                {
                    case 0:
                        this.RenderRotScaleBgNormal(bg);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgBlend(bg);
                        else
                            this.RenderRotScaleBgNormal(bg);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgBrightInc(bg);
                        else
                            this.RenderRotScaleBgNormal(bg);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << bg)) != 0)
                            this.RenderRotScaleBgBrightDec(bg);
                        else
                            this.RenderRotScaleBgNormal(bg);
                        break;
                }
            }
        }

        private void DrawSprites(int pri)
        {
            if (this.winEnabled)
            {
                switch (this.blendType)
                {
                    case 0:
                        this.DrawSpritesWindow(pri);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesWindowBlend(pri);
                        else
                            this.DrawSpritesWindow(pri);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesWindowBrightInc(pri);
                        else
                            this.DrawSpritesWindow(pri);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesWindowBrightDec(pri);
                        else
                            this.DrawSpritesWindow(pri);
                        break;
                }
            }
            else
            {
                switch (this.blendType)
                {
                    case 0:
                        this.DrawSpritesNormal(pri);
                        break;
                    case 1:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesBlend(pri);
                        else
                            this.DrawSpritesNormal(pri);
                        break;
                    case 2:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesBrightInc(pri);
                        else
                            this.DrawSpritesNormal(pri);
                        break;
                    case 3:
                        if ((this.blendSource & (1 << 4)) != 0)
                            this.DrawSpritesBrightDec(pri);
                        else
                            this.DrawSpritesNormal(pri);
                        break;
                }
            }
        }

        private void RenderMode0Line()
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            for (int pri = 3; pri >= 0; pri--)
            {
                for (int i = 3; i >= 0; i--)
                {
                    if ((this.dispCnt & (1 << (8 + i))) != 0)
                    {
                        ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            this.RenderTextBg(i);
                        }
                    }
                }

                this.DrawSprites(pri);
            }
        }

        private void RenderMode1Line()
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            for (int pri = 3; pri >= 0; pri--)
            {
                if ((this.dispCnt & (1 << (8 + 2))) != 0)
                {
                    ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG2CNT);

                    if ((bgcnt & 0x3) == pri)
                    {
                        this.RenderRotScaleBg(2);
                    }
                }

                for (int i = 1; i >= 0; i--)
                {
                    if ((this.dispCnt & (1 << (8 + i))) != 0)
                    {
                        ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            this.RenderTextBg(i);
                        }
                    }
                }

                this.DrawSprites(pri);
            }
        }

        private void RenderMode2Line()
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            for (int pri = 3; pri >= 0; pri--)
            {
                for (int i = 3; i >= 2; i--)
                {
                    if ((this.dispCnt & (1 << (8 + i))) != 0)
                    {
                        ushort bgcnt = Memory.ReadU16(this.memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            this.RenderRotScaleBg(i);
                        }
                    }
                }

                this.DrawSprites(pri);
            }
        }

        private void RenderMode3Line()
        {
            ushort bg2Cnt = Memory.ReadU16(this.memory.IORam, Memory.BG2CNT);

            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            byte blendMaskType = (byte)(1 << 2);

            int bgPri = bg2Cnt & 0x3;
            for (int pri = 3; pri > bgPri; pri--)
            {
                this.DrawSprites(pri);
            }

            if ((this.dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                int x = this.memory.Bgx[0];
                int y = this.memory.Bgy[0];

                short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA);
                short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC);

                for (int i = 0; i < 240; i++)
                {
                    int ax = ((int)x) >> 8;
                    int ay = ((int)y) >> 8;

                    if (ax >= 0 && ax < 240 && ay >= 0 && ay < 160)
                    {
                        int curIdx = ((ay * 240) + ax) * 2;
                        this.scanline[i] = Renderer.GbaTo32((ushort)(vram[curIdx] | (vram[curIdx + 1] << 8)));
                        this.blend[i] = blendMaskType;
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (int pri = bgPri; pri >= 0; pri--)
            {
                this.DrawSprites(pri);
            }
        }

        private void RenderMode4Line()
        {
            ushort bg2Cnt = Memory.ReadU16(this.memory.IORam, Memory.BG2CNT);

            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            byte blendMaskType = (byte)(1 << 2);

            int bgPri = bg2Cnt & 0x3;
            for (int pri = 3; pri > bgPri; pri--)
            {
                this.DrawSprites(pri);
            }

            if ((this.dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                int baseIdx = 0;
                if ((this.dispCnt & (1 << 4)) == 1 << 4) baseIdx = 0xA000;

                int x = this.memory.Bgx[0];
                int y = this.memory.Bgy[0];

                short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA);
                short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC);

                for (int i = 0; i < 240; i++)
                {
                    int ax = ((int)x) >> 8;
                    int ay = ((int)y) >> 8;

                    if (ax >= 0 && ax < 240 && ay >= 0 && ay < 160)
                    {
                        int lookup = vram[baseIdx + (ay * 240) + ax];
                        if (lookup != 0)
                        {
                            this.scanline[i] = Renderer.GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            this.blend[i] = blendMaskType;
                        }
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (int pri = bgPri; pri >= 0; pri--)
            {
                this.DrawSprites(pri);
            }
        }

        private void RenderMode5Line()
        {
            ushort bg2Cnt = Memory.ReadU16(this.memory.IORam, Memory.BG2CNT);

            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            this.DrawBackdrop();

            byte blendMaskType = (byte)(1 << 2);

            int bgPri = bg2Cnt & 0x3;
            for (int pri = 3; pri > bgPri; pri--)
            {
                this.DrawSprites(pri);
            }

            if ((this.dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                int baseIdx = 0;
                if ((this.dispCnt & (1 << 4)) == 1 << 4) baseIdx += 160 * 128 * 2;

                int x = this.memory.Bgx[0];
                int y = this.memory.Bgy[0];

                short dx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PA);
                short dy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PC);

                for (int i = 0; i < 240; i++)
                {
                    int ax = ((int)x) >> 8;
                    int ay = ((int)y) >> 8;

                    if (ax >= 0 && ax < 160 && ay >= 0 && ay < 128)
                    {
                        int curIdx = (int)(ay * 160 + ax) * 2;

                        this.scanline[i] = Renderer.GbaTo32((ushort)(vram[baseIdx + curIdx] | (vram[baseIdx + curIdx + 1] << 8)));
                        this.blend[i] = blendMaskType;
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (int pri = bgPri; pri >= 0; pri--)
            {
                this.DrawSprites(pri);
            }
        }

        private void DrawSpriteWindows()
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((this.dispCnt & (1 << 12)) == 0) return;

            for (int oamNum = 127; oamNum >= 0; oamNum--)
            {
                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                
                // Not an object window, so continue
                if (((attr0 >> 10) & 3) != 2) continue;

                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                int x = attr1 & 0x1FF;
                int y = attr0 & 0xFF;

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
                            if ((i & 0x1ff) < 240)
                            {
                                int tx = (i - x) & 7;
                                if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    this.windowCover[i & 0x1ff] = this.winObjEnabled;
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
                            if ((i & 0x1ff) < 240)
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
                                    this.windowCover[i & 0x1ff] = this.winObjEnabled;
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

                    short rx = (short)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                    short ry = (short)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                    // Draw a rot/scale sprite
                    if ((attr0 & (1 << 13)) != 0)
                    {
                        // 256 colors
                        for (int i = x; i < x + rwidth; i++)
                        {
                            int tx = rx >> 8;
                            int ty = ry >> 8;

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height)
                            {
                                int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    this.windowCover[i & 0x1ff] = this.winObjEnabled;
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

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height)
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
                                    this.windowCover[i & 0x1ff] = this.winObjEnabled;
                                }
                            }

                            rx += dx;
                            ry += dy;
                        }
                    }
                }
            }
        }

        public static uint GbaTo32(ushort color)
        {
            // more accurate, but slower :(
            // return colorLUT[color];
            return ((color & 0x1FU) << 19) | ((color & 0x3E0U) << 6) | ((color & 0x7C00U) >> 7);
        }
    }
}
