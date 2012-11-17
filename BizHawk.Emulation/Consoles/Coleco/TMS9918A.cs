using System;
using BizHawk.Emulation.CPUs.Z80;

namespace BizHawk.Emulation.Consoles.Coleco
{
    public sealed class TMS9918A : IVideoProvider
    {
        byte[] VRAM = new byte[0x4000];
        byte[] Registers = new byte[8];
        byte StatusByte;

        bool   VdpWaitingForLatchByte;
        byte   VdpLatch;
        ushort VdpAddress;
        byte   VdpBuffer;
        VdpCommand vdpCommand; // TODO remove?

        int TmsMode;

        public bool Mode1Bit              { get { return (Registers[1] & 16) > 0; } }
        public bool Mode2Bit              { get { return (Registers[0] & 2)  > 0; } }
        public bool Mode3Bit              { get { return (Registers[1] & 8)  > 0; } }

        public bool EnableDoubledSprites  { get { return (Registers[1] & 1)  > 0; } }
        public bool EnableLargeSprites    { get { return (Registers[1] & 2)  > 0; } }
        public bool EnableInterrupts      { get { return (Registers[1] & 32) > 0; } }
        public bool DisplayOn             { get { return (Registers[1] & 64) > 0; } }
        // TODO, is 4/16K bit used?

        //int NameTableBase;
        int ColorTableBase;
        int PatternGeneratorBase;
        int SpritePatternGeneratorBase;
        int TmsPatternNameTableBase;
        int TmsSpriteAttributeBase;

        public void ExecuteFrame()
        {
            for (int scanLine = 0; scanLine < 262; scanLine++)
            {
                RenderScanline(scanLine);

                if (scanLine == 192)
                {
                    // TODO interrupt pending thing
                    if (EnableInterrupts)
                        Cpu.NonMaskableInterrupt = true;
                    //Console.WriteLine("Set NMI / VSYNC");
                }

                Cpu.ExecuteCycles(228);
            }

        }

        public void WriteVdpControl(byte value)
        {
            if (VdpWaitingForLatchByte)
            {
                VdpLatch = value;
                VdpWaitingForLatchByte = false;
                VdpAddress = (ushort)((VdpAddress & 0xFF00) | value);
                return;
            }

            VdpWaitingForLatchByte = true;
            VdpAddress = (ushort)(((value & 63) << 8) | VdpLatch);
            switch (value & 0xC0)
            {
                case 0x00: // read VRAM
                    vdpCommand = VdpCommand.VramRead;
                    VdpBuffer = VRAM[VdpAddress & 0x3FFF];
                    VdpAddress++;
                    break;
                case 0x40: // write VRAM
                    vdpCommand = VdpCommand.VramWrite;
                    break;
                case 0x80: // VDP register write
                    vdpCommand = VdpCommand.RegisterWrite;
                    int reg = value & 0x0F;
                    WriteRegister(reg, VdpLatch);
                    break;
            }
        }

        public void WriteVdpData(byte value)
        {
            VdpWaitingForLatchByte = true;
            VdpBuffer = value;
            
            // Write VRAM and update pre-computed pattern buffer. 
            //UpdatePatternBuffer((ushort)(VdpAddress & 0x3FFF), value);
            //Console.WriteLine("VRAM[{0:X4}] = {1:X2}", VdpAddress & 0x3FFF, value);
            VRAM[VdpAddress & 0x3FFF] = value;
            
            VdpAddress++;
        }

        void WriteRegister(int reg, byte data)
        {
            if (reg >= 8) return;

            Console.WriteLine("Write register {0} : {1:X2}", reg, data);
            Registers[reg] = data;
            switch (reg)
            {
                case 0: // Mode Control Register 1
                    CheckVideoMode();
                    break;
                case 1: // Mode Control Register 2
                    CheckVideoMode();
                    //Cpu.NonMaskableInterrupt = (EnableInterrupts && VIntPending);
                    break;
                case 2: // Name Table Base Address
                    //NameTableBase = CalcNameTableBase();
                    TmsPatternNameTableBase = (Registers[2] << 10) & 0x3C00;
                    break;
                case 3: // Color Table Base Address
                    ColorTableBase = (Registers[3] << 6) & 0x3FC0;
                    break;
                case 4: // Pattern Generator Base Address
                    PatternGeneratorBase = (Registers[4] << 11) & 0x3800;
                    break;
                case 5: // Sprite Attribute Table Base Address
                    // ??? should I move from my property to precalculated?
                    TmsSpriteAttributeBase = (Registers[5] << 7) & 0x3F80;
                    break;
                case 6: // Sprite Pattern Generator Base Adderss 
                    SpritePatternGeneratorBase = (Registers[6] << 11) & 0x3800;
                    break;
            }
        }

        public byte ReadVdpStatus()
        {
            VdpWaitingForLatchByte = true;
            byte returnValue = StatusByte;
            StatusByte &= 0x1F;
            //VIntPending = false;
            Cpu.NonMaskableInterrupt = false;

            //Console.WriteLine("Clear NMI / read status");
            return returnValue;
        }

        public byte ReadData()
        {
            VdpWaitingForLatchByte = true;
            byte value = VdpBuffer;
            VdpBuffer = VRAM[VdpAddress & 0x3FFF];
            VdpAddress++;
            return value;
        }

        void CheckVideoMode()
        {
                 if (Mode1Bit) TmsMode = 1;
            else if (Mode2Bit) TmsMode = 2;
            else if (Mode3Bit) TmsMode = 3;
            else               TmsMode = 0;

            Console.WriteLine("video mode {0}", TmsMode);
        }

        void RenderScanline(int scanLine)
        {
            if (scanLine >= 192)
                return;

            if (TmsMode == 2)
            {
                RenderBackgroundM2(scanLine);
                RenderTmsSprites(scanLine);
            }
            else if (TmsMode == 0)
            {
                RenderBackgroundM0(scanLine);
                RenderTmsSprites(scanLine);
            }
        }

        void RenderBackgroundM0(int scanLine)
        {
            if (DisplayOn == false)
            {
                Array.Clear(FrameBuffer, scanLine * 256, 256);
                return;
            }

            int yc = scanLine / 8;
            int yofs = scanLine % 8;
            int FrameBufferOffset = scanLine * 256;
            int PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
            int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

            for (int xc = 0; xc < 32; xc++)
            {
                int pn = VRAM[PatternNameOffset++];
                int pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
                int colorEntry = VRAM[ColorTableBase + (pn / 8)];
                int fgIndex = (colorEntry >> 4) & 0x0F;
                int bgIndex = colorEntry & 0x0F;
                int fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
                int bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

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

        void RenderBackgroundM2(int scanLine)
        {
            if (DisplayOn == false)
            {
                Array.Clear(FrameBuffer, scanLine * 256, 256);
                return;
            }

            int yrow = scanLine / 8;
            int yofs = scanLine % 8;
            int FrameBufferOffset = scanLine * 256;
            int PatternNameOffset = TmsPatternNameTableBase + (yrow * 32);
            int PatternGeneratorOffset = (((Registers[4] & 4) << 11) & 0x2000);// +((yrow / 8) * 0x100);
            int ColorOffset = (ColorTableBase & 0x2000);// +((yrow / 8) * 0x100);
            int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

            for (int xc = 0; xc < 32; xc++)
            {
                int pn = VRAM[PatternNameOffset++] + ((yrow / 8) * 0x100);
                int pv = VRAM[PatternGeneratorOffset + (pn * 8) + yofs];
                int colorEntry = VRAM[ColorOffset + (pn * 8) + yofs];
                int fgIndex = (colorEntry >> 4) & 0x0F;
                int bgIndex = colorEntry & 0x0F;
                int fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
                int bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

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

        byte[] ScanlinePriorityBuffer = new byte[256];
        byte[] SpriteCollisionBuffer  = new byte[256];

        void RenderTmsSprites(int scanLine)
        {
            if (DisplayOn == false) return;

            Array.Clear(ScanlinePriorityBuffer, 0, 256);
            Array.Clear(SpriteCollisionBuffer, 0, 256);

            bool Double = EnableDoubledSprites;
            bool LargeSprites = EnableLargeSprites;

            int SpriteSize = 8;
            if (LargeSprites) SpriteSize *= 2;
            if (Double) SpriteSize *= 2;
            int OneCellSize = Double ? 16 : 8;

            int NumSpritesOnScanline = 0;
            for (int i = 0; i < 32; i++)
            {
                int SpriteBase = TmsSpriteAttributeBase + (i * 4);
                int y = VRAM[SpriteBase++];
                int x = VRAM[SpriteBase++];
                int Pattern = VRAM[SpriteBase++];
                int Color = VRAM[SpriteBase];

                if (y == 208) break; // terminator sprite
                if (y > 224) y -= 256; // sprite Y wrap
                y++; // inexplicably, sprites start on Y+1
                if (y > scanLine || y + SpriteSize <= scanLine) continue; // sprite is not on this scanline
                if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

                if (++NumSpritesOnScanline == 5)
                {
                    StatusByte |= (byte)i; // set 5th sprite index
                    StatusByte |= 0x40; // set overflow bit
                    break;
                }

                if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-byte alignment
                int SpriteLine = scanLine - y;
                if (Double) SpriteLine /= 2;

                byte pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

                for (int xp = 0; xp < SpriteSize && x + xp < 256; xp++)
                {
                    if (x + xp < 0) continue;
                    if (LargeSprites && xp == OneCellSize)
                        pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

                    if ((pv & (1 << (7 - (xp & 7)))) > 0)
                    {
                        // todo sprite collision
                        if (Color != 0 && ScanlinePriorityBuffer[x + xp] == 0)
                        {
                            ScanlinePriorityBuffer[x + xp] = 1;
                            FrameBuffer[(scanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
                        }
                    }
                }
            }
        }

        Z80A Cpu;
        public TMS9918A(Z80A cpu)
        {
            this.Cpu = cpu;
        }

        public int[] FrameBuffer = new int[256 * 192];
        public int[] GetVideoBuffer()   { return FrameBuffer; }

        public int VirtualWidth         { get { return 256; } }
        public int BufferWidth          { get { return 256; } }
        public int BufferHeight         { get { return 192; } }
        public int BackgroundColor      { get { return 0; } }

        enum VdpCommand { VramRead, VramWrite, RegisterWrite }

        int[] PaletteTMS9918 = new int[] 
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
    }
}