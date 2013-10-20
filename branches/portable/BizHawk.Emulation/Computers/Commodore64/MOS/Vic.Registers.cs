using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        public byte Peek(int addr)
        {
            return ReadRegister((addr & 0x3F));
        }

        public void Poke(int addr, byte val)
        {
            WriteRegister((addr & 0x3F), val);
        }

        public byte Read(int addr)
        {
            byte result;
            addr &= 0x3F;

            switch (addr)
            {
                case 0x1E:
                case 0x1F:
                    // reading clears these
                    result = ReadRegister(addr);
                    WriteRegister(addr, 0);
                    break;
                default:
                    result = ReadRegister((addr & 0x3F));
                    break;
            }
            return result;
        }

        private byte ReadRegister(int addr)
        {
            byte result = 0xFF; //unused bit value

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
                    result = (byte)(sprites[addr >> 1].x & 0xFF);
                    break;
                case 0x01:
                case 0x03:
                case 0x05:
                case 0x07:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                    result = (byte)(sprites[addr >> 1].y & 0xFF);
                    break;
                case 0x10:
                    result = (byte)(
                        ((sprites[0].x >> 8) & 0x01) |
                        ((sprites[1].x >> 7) & 0x02) |
                        ((sprites[2].x >> 6) & 0x04) |
                        ((sprites[3].x >> 5) & 0x08) |
                        ((sprites[4].x >> 4) & 0x10) |
                        ((sprites[5].x >> 3) & 0x20) |
                        ((sprites[6].x >> 2) & 0x40) |
                        ((sprites[7].x >> 1) & 0x80)
                        );
                    break;
                case 0x11:
                    result = (byte)(
                        (yScroll & 0x7) |
                        (rowSelect ? 0x08 : 0x00) |
                        (displayEnable ? 0x10 : 0x00) |
                        (bitmapMode ? 0x20 : 0x00) |
                        (extraColorMode ? 0x40 : 0x00) |
                        ((rasterLine & 0x100) >> 1)
                        );
                    break;
                case 0x12:
                    result = (byte)(rasterLine & 0xFF);
                    break;
                case 0x13:
                    result = (byte)(lightPenX & 0xFF);
                    break;
                case 0x14:
                    result = (byte)(lightPenY & 0xFF);
                    break;
                case 0x15:
                    result = (byte)(
                        (sprites[0].enable ? 0x01 : 0x00) |
                        (sprites[1].enable ? 0x02 : 0x00) |
                        (sprites[2].enable ? 0x04 : 0x00) |
                        (sprites[3].enable ? 0x08 : 0x00) |
                        (sprites[4].enable ? 0x10 : 0x00) |
                        (sprites[5].enable ? 0x20 : 0x00) |
                        (sprites[6].enable ? 0x40 : 0x00) |
                        (sprites[7].enable ? 0x80 : 0x00)
                        );
                    break;
                case 0x16:
                    result &= 0xC0;
                    result |= (byte)(
                        (xScroll & 0x7) |
                        (columnSelect ? 0x08 : 0x00) |
                        (multicolorMode ? 0x10 : 0x00)
                        );
                    break;
                case 0x17:
                    result = (byte)(
                        (sprites[0].yExpand ? 0x01 : 0x00) |
                        (sprites[1].yExpand ? 0x02 : 0x00) |
                        (sprites[2].yExpand ? 0x04 : 0x00) |
                        (sprites[3].yExpand ? 0x08 : 0x00) |
                        (sprites[4].yExpand ? 0x10 : 0x00) |
                        (sprites[5].yExpand ? 0x20 : 0x00) |
                        (sprites[6].yExpand ? 0x40 : 0x00) |
                        (sprites[7].yExpand ? 0x80 : 0x00)
                        );
                    break;
                case 0x18:
                    result &= 0x01;
                    result |= (byte)(
                        ((pointerVM & 0x3C00) >> 6) |
                        ((pointerCB & 0x7) << 1)
                        );
                    break;
                case 0x19:
                    result &= 0x70;
                    result |= (byte)(
                        (intRaster ? 0x01 : 0x00) |
                        (intSpriteDataCollision ? 0x02 : 0x00) |
                        (intSpriteCollision ? 0x04 : 0x00) |
                        (intLightPen ? 0x08 : 0x00) |
                        (pinIRQ ? 0x00 : 0x80)
                        );
                    break;
                case 0x1A:
                    result &= 0xF0;
                    result |= (byte)(
                        (enableIntRaster ? 0x01 : 0x00) |
                        (enableIntSpriteDataCollision ? 0x02 : 0x00) |
                        (enableIntSpriteCollision ? 0x04 : 0x00) |
                        (enableIntLightPen ? 0x08 : 0x00)
                        );
                    break;
                case 0x1B:
                    result = (byte)(
                        (sprites[0].priority ? 0x01 : 0x00) |
                        (sprites[1].priority ? 0x02 : 0x00) |
                        (sprites[2].priority ? 0x04 : 0x00) |
                        (sprites[3].priority ? 0x08 : 0x00) |
                        (sprites[4].priority ? 0x10 : 0x00) |
                        (sprites[5].priority ? 0x20 : 0x00) |
                        (sprites[6].priority ? 0x40 : 0x00) |
                        (sprites[7].priority ? 0x80 : 0x00)
                        );
                    break;
                case 0x1C:
                    result = (byte)(
                        (sprites[0].multicolor ? 0x01 : 0x00) |
                        (sprites[1].multicolor ? 0x02 : 0x00) |
                        (sprites[2].multicolor ? 0x04 : 0x00) |
                        (sprites[3].multicolor ? 0x08 : 0x00) |
                        (sprites[4].multicolor ? 0x10 : 0x00) |
                        (sprites[5].multicolor ? 0x20 : 0x00) |
                        (sprites[6].multicolor ? 0x40 : 0x00) |
                        (sprites[7].multicolor ? 0x80 : 0x00)
                        );
                    break;
                case 0x1D:
                    result = (byte)(
                        (sprites[0].xExpand ? 0x01 : 0x00) |
                        (sprites[1].xExpand ? 0x02 : 0x00) |
                        (sprites[2].xExpand ? 0x04 : 0x00) |
                        (sprites[3].xExpand ? 0x08 : 0x00) |
                        (sprites[4].xExpand ? 0x10 : 0x00) |
                        (sprites[5].xExpand ? 0x20 : 0x00) |
                        (sprites[6].xExpand ? 0x40 : 0x00) |
                        (sprites[7].xExpand ? 0x80 : 0x00)
                        );
                    break;
                case 0x1E:
                    result = (byte)(
                        (sprites[0].collideSprite ? 0x01 : 0x00) |
                        (sprites[1].collideSprite ? 0x02 : 0x00) |
                        (sprites[2].collideSprite ? 0x04 : 0x00) |
                        (sprites[3].collideSprite ? 0x08 : 0x00) |
                        (sprites[4].collideSprite ? 0x10 : 0x00) |
                        (sprites[5].collideSprite ? 0x20 : 0x00) |
                        (sprites[6].collideSprite ? 0x40 : 0x00) |
                        (sprites[7].collideSprite ? 0x80 : 0x00)
                        );
                    break;
                case 0x1F:
                    result = (byte)(
                        (sprites[0].collideData ? 0x01 : 0x00) |
                        (sprites[1].collideData ? 0x02 : 0x00) |
                        (sprites[2].collideData ? 0x04 : 0x00) |
                        (sprites[3].collideData ? 0x08 : 0x00) |
                        (sprites[4].collideData ? 0x10 : 0x00) |
                        (sprites[5].collideData ? 0x20 : 0x00) |
                        (sprites[6].collideData ? 0x40 : 0x00) |
                        (sprites[7].collideData ? 0x80 : 0x00)
                        );
                    break;
                case 0x20:
                    result &= 0xF0;
                    result |= (byte)(borderColor & 0x0F);
                    break;
                case 0x21:
                    result &= 0xF0;
                    result |= (byte)(backgroundColor0 & 0x0F);
                    break;
                case 0x22:
                    result &= 0xF0;
                    result |= (byte)(backgroundColor1 & 0x0F);
                    break;
                case 0x23:
                    result &= 0xF0;
                    result |= (byte)(backgroundColor2 & 0x0F);
                    break;
                case 0x24:
                    result &= 0xF0;
                    result |= (byte)(backgroundColor3 & 0x0F);
                    break;
                case 0x25:
                    result &= 0xF0;
                    result |= (byte)(spriteMulticolor0 & 0x0F);
                    break;
                case 0x26:
                    result &= 0xF0;
                    result |= (byte)(spriteMulticolor1 & 0x0F);
                    break;
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                    result &= 0xF0;
                    result |= (byte)(sprites[addr - 0x27].color & 0xF);
                    break;
                default:
                    // not connected
                    break;
            }

            return result;
        }

        public void Write(int addr, byte val)
        {
            addr &= 0x3F;
            switch (addr)
            {
                case 0x19:
                    // interrupts are cleared by writing a 1
                    if ((val & 0x01) != 0)
                        intRaster = false;
                    if ((val & 0x02) != 0)
                        intSpriteDataCollision = false;
                    if ((val & 0x04) != 0)
                        intSpriteCollision = false;
                    if ((val & 0x08) != 0)
                        intLightPen = false;
                    UpdatePins();
                    break;
                case 0x1A:
                    WriteRegister(addr, val);
                    break;
                case 0x1E:
                case 0x1F:
                    // can't write to these
                    break;
                case 0x2F:
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    // not connected
                    break;
                default:
                    WriteRegister(addr, val);
                    break;
            }
        }

        private void WriteRegister(int addr, byte val)
        {
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
                    sprites[addr >> 1].x &= 0x100;
                    sprites[addr >> 1].x |= val;
                    break;
                case 0x01:
                case 0x03:
                case 0x05:
                case 0x07:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                    sprites[addr >> 1].y = val;
                    break;
                case 0x10:
                    sprites[0].x = (sprites[0].x & 0xFF) | ((val & 0x01) << 8);
                    sprites[1].x = (sprites[1].x & 0xFF) | ((val & 0x02) << 7);
                    sprites[2].x = (sprites[2].x & 0xFF) | ((val & 0x04) << 6);
                    sprites[3].x = (sprites[3].x & 0xFF) | ((val & 0x08) << 5);
                    sprites[4].x = (sprites[4].x & 0xFF) | ((val & 0x10) << 4);
                    sprites[5].x = (sprites[5].x & 0xFF) | ((val & 0x20) << 3);
                    sprites[6].x = (sprites[6].x & 0xFF) | ((val & 0x40) << 2);
                    sprites[7].x = (sprites[7].x & 0xFF) | ((val & 0x80) << 1);
                    break;
                case 0x11:
                    yScroll = (val & 0x07);
                    rowSelect = ((val & 0x08) != 0);
                    displayEnable = ((val & 0x10) != 0);
                    bitmapMode = ((val & 0x20) != 0);
                    extraColorMode = ((val & 0x40) != 0);
                    rasterInterruptLine &= 0xFF;
                    rasterInterruptLine |= (val & 0x80) << 1;
                    UpdateBorder();
                    UpdateVideoMode();
                    break;
                case 0x12:
                    rasterInterruptLine &= 0x100;
                    rasterInterruptLine |= val;
                    break;
                case 0x13:
                    lightPenX = val;
                    break;
                case 0x14:
                    lightPenY = val;
                    break;
                case 0x15:
                    sprites[0].enable = ((val & 0x01) != 0);
                    sprites[1].enable = ((val & 0x02) != 0);
                    sprites[2].enable = ((val & 0x04) != 0);
                    sprites[3].enable = ((val & 0x08) != 0);
                    sprites[4].enable = ((val & 0x10) != 0);
                    sprites[5].enable = ((val & 0x20) != 0);
                    sprites[6].enable = ((val & 0x40) != 0);
                    sprites[7].enable = ((val & 0x80) != 0);
                    break;
                case 0x16:
                    xScroll = (val & 0x07);
                    columnSelect = ((val & 0x08) != 0);
                    multicolorMode = ((val & 0x10) != 0);
                    UpdateBorder();
                    UpdateVideoMode();
                    break;
                case 0x17:
                    sprites[0].yExpand = ((val & 0x01) != 0);
                    sprites[1].yExpand = ((val & 0x02) != 0);
                    sprites[2].yExpand = ((val & 0x04) != 0);
                    sprites[3].yExpand = ((val & 0x08) != 0);
                    sprites[4].yExpand = ((val & 0x10) != 0);
                    sprites[5].yExpand = ((val & 0x20) != 0);
                    sprites[6].yExpand = ((val & 0x40) != 0);
                    sprites[7].yExpand = ((val & 0x80) != 0);
                    break;
                case 0x18:
                    pointerVM = ((val << 6) & 0x3C00);
                    pointerCB = ((val >> 1) & 0x7);
                    break;
                case 0x19:
                    intRaster = ((val & 0x01) != 0);
                    intSpriteDataCollision = ((val & 0x02) != 0);
                    intSpriteCollision = ((val & 0x04) != 0);
                    intLightPen = ((val & 0x08) != 0);
                    UpdatePins();
                    break;
                case 0x1A:
                    enableIntRaster = ((val & 0x01) != 0);
                    enableIntSpriteDataCollision = ((val & 0x02) != 0);
                    enableIntSpriteCollision = ((val & 0x04) != 0);
                    enableIntLightPen = ((val & 0x08) != 0);
                    UpdatePins();
                    break;
                case 0x1B:
                    sprites[0].priority = ((val & 0x01) != 0);
                    sprites[1].priority = ((val & 0x02) != 0);
                    sprites[2].priority = ((val & 0x04) != 0);
                    sprites[3].priority = ((val & 0x08) != 0);
                    sprites[4].priority = ((val & 0x10) != 0);
                    sprites[5].priority = ((val & 0x20) != 0);
                    sprites[6].priority = ((val & 0x40) != 0);
                    sprites[7].priority = ((val & 0x80) != 0);
                    break;
                case 0x1C:
                    sprites[0].multicolor = ((val & 0x01) != 0);
                    sprites[1].multicolor = ((val & 0x02) != 0);
                    sprites[2].multicolor = ((val & 0x04) != 0);
                    sprites[3].multicolor = ((val & 0x08) != 0);
                    sprites[4].multicolor = ((val & 0x10) != 0);
                    sprites[5].multicolor = ((val & 0x20) != 0);
                    sprites[6].multicolor = ((val & 0x40) != 0);
                    sprites[7].multicolor = ((val & 0x80) != 0);
                    break;
                case 0x1D:
                    sprites[0].xExpand = ((val & 0x01) != 0);
                    sprites[1].xExpand = ((val & 0x02) != 0);
                    sprites[2].xExpand = ((val & 0x04) != 0);
                    sprites[3].xExpand = ((val & 0x08) != 0);
                    sprites[4].xExpand = ((val & 0x10) != 0);
                    sprites[5].xExpand = ((val & 0x20) != 0);
                    sprites[6].xExpand = ((val & 0x40) != 0);
                    sprites[7].xExpand = ((val & 0x80) != 0);
                    break;
                case 0x1E:
                    sprites[0].collideSprite = ((val & 0x01) != 0);
                    sprites[1].collideSprite = ((val & 0x02) != 0);
                    sprites[2].collideSprite = ((val & 0x04) != 0);
                    sprites[3].collideSprite = ((val & 0x08) != 0);
                    sprites[4].collideSprite = ((val & 0x10) != 0);
                    sprites[5].collideSprite = ((val & 0x20) != 0);
                    sprites[6].collideSprite = ((val & 0x40) != 0);
                    sprites[7].collideSprite = ((val & 0x80) != 0);
                    break;
                case 0x1F:
                    sprites[0].collideData = ((val & 0x01) != 0);
                    sprites[1].collideData = ((val & 0x02) != 0);
                    sprites[2].collideData = ((val & 0x04) != 0);
                    sprites[3].collideData = ((val & 0x08) != 0);
                    sprites[4].collideData = ((val & 0x10) != 0);
                    sprites[5].collideData = ((val & 0x20) != 0);
                    sprites[6].collideData = ((val & 0x40) != 0);
                    sprites[7].collideData = ((val & 0x80) != 0);
                    break;
                case 0x20:
                    borderColor = (val & 0xF);
                    break;
                case 0x21:
                    backgroundColor0 = (val & 0xF);
                    break;
                case 0x22:
                    backgroundColor1 = (val & 0xF);
                    break;
                case 0x23:
                    backgroundColor2 = (val & 0xF);
                    break;
                case 0x24:
                    backgroundColor3 = (val & 0xF);
                    break;
                case 0x25:
                    spriteMulticolor0 = (val & 0xF);
                    break;
                case 0x26:
                    spriteMulticolor1 = (val & 0xF);
                    break;
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                    sprites[addr - 0x27].color = (val & 0xF);
                    break;
                default:
                    break;
            }
        }
    }
}
