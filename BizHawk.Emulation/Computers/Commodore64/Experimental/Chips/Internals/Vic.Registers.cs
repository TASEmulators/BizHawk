using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        public int Peek(int addr)
        {
            switch (addr)
            {
                case 0x00: return sprites[0].X & 0xFF;
                case 0x01: return sprites[1].X & 0xFF;
                case 0x02: return sprites[2].X & 0xFF;
                case 0x03: return sprites[3].X & 0xFF;
                case 0x04: return sprites[4].X & 0xFF;
                case 0x05: return sprites[5].X & 0xFF;
                case 0x06: return sprites[6].X & 0xFF;
                case 0x07: return sprites[7].X & 0xFF;
                case 0x08: return sprites[0].Y;
                case 0x09: return sprites[1].Y;
                case 0x0A: return sprites[2].Y;
                case 0x0B: return sprites[3].Y;
                case 0x0C: return sprites[4].Y;
                case 0x0D: return sprites[5].Y;
                case 0x0E: return sprites[6].Y;
                case 0x0F: return sprites[7].Y;
                case 0x10: return (
                    ((sprites[0].X & 0x80) >> 7) |
                    ((sprites[1].X & 0x80) >> 6) |
                    ((sprites[2].X & 0x80) >> 5) |
                    ((sprites[3].X & 0x80) >> 4) |
                    ((sprites[4].X & 0x80) >> 3) |
                    ((sprites[5].X & 0x80) >> 2) |
                    ((sprites[6].X & 0x80) >> 1) |
                    (sprites[7].X & 0x80)
                    );
                case 0x11: return (
                    yScroll |
                    (rowSelect ? 0x08 : 0x00) |
                    (displayEnable ? 0x10 : 0x00) |
                    (bitmapMode ? 0x20 : 0x00) |
                    (extraColorMode ? 0x40 : 0x00) |
                    ((rasterY & 0x100) >> 1)
                    );
                case 0x12: return (rasterY & 0xFF);
                case 0x13: return lightPenX;
                case 0x14: return lightPenY;
                case 0x15: return (
                    (sprites[0].Enabled ? 0x01 : 0x00) |
                    (sprites[1].Enabled ? 0x02 : 0x00) |
                    (sprites[2].Enabled ? 0x04 : 0x00) |
                    (sprites[3].Enabled ? 0x08 : 0x00) |
                    (sprites[4].Enabled ? 0x10 : 0x00) |
                    (sprites[5].Enabled ? 0x20 : 0x00) |
                    (sprites[6].Enabled ? 0x40 : 0x00) |
                    (sprites[7].Enabled ? 0x80 : 0x00)
                    );
                case 0x16: return (
                    xScroll |
                    (columnSelect ? 0x08 : 0x00) |
                    (multiColorMode ? 0x10 : 0x00) |
                    (reset ? 0x20 : 0x00) |
                    0xC0
                    );
                case 0x17: return (
                    (sprites[0].ExpandY ? 0x01 : 0x00) |
                    (sprites[1].ExpandY ? 0x02 : 0x00) |
                    (sprites[2].ExpandY ? 0x04 : 0x00) |
                    (sprites[3].ExpandY ? 0x08 : 0x00) |
                    (sprites[4].ExpandY ? 0x10 : 0x00) |
                    (sprites[5].ExpandY ? 0x20 : 0x00) |
                    (sprites[6].ExpandY ? 0x40 : 0x00) |
                    (sprites[7].ExpandY ? 0x80 : 0x00)
                    );
                case 0x18: return (
                    (videoMemory >> 6) |
                    (characterBitmap >> 10)
                    );
                case 0x19: return (
                    (rasterInterrupt ? 0x01 : 0x00) |
                    (dataCollisionInterrupt ? 0x02 : 0x00) |
                    (spriteCollisionInterrupt ? 0x04 : 0x00) |
                    (lightPenInterrupt ? 0x08 : 0x00) |
                    0x70 |
                    (irq ? 0x80 : 0x00)
                    );
                case 0x1A: return (
                    (rasterInterruptEnable ? 0x01 : 0x00) |
                    (dataCollisionInterruptEnable ? 0x02 : 0x00) |
                    (spriteCollisionInterruptEnable ? 0x04 : 0x00) |
                    (lightPenInterruptEnable ? 0x08 : 0x00) |
                    0xF0
                    );
                case 0x1B: return (
                    (sprites[0].Priority ? 0x01 : 0x00) |
                    (sprites[1].Priority ? 0x02 : 0x00) |
                    (sprites[2].Priority ? 0x04 : 0x00) |
                    (sprites[3].Priority ? 0x08 : 0x00) |
                    (sprites[4].Priority ? 0x10 : 0x00) |
                    (sprites[5].Priority ? 0x20 : 0x00) |
                    (sprites[6].Priority ? 0x40 : 0x00) |
                    (sprites[7].Priority ? 0x80 : 0x00)
                    );
                case 0x1C: return (
                    (sprites[0].Multicolor ? 0x01 : 0x00) |
                    (sprites[1].Multicolor ? 0x02 : 0x00) |
                    (sprites[2].Multicolor ? 0x04 : 0x00) |
                    (sprites[3].Multicolor ? 0x08 : 0x00) |
                    (sprites[4].Multicolor ? 0x10 : 0x00) |
                    (sprites[5].Multicolor ? 0x20 : 0x00) |
                    (sprites[6].Multicolor ? 0x40 : 0x00) |
                    (sprites[7].Multicolor ? 0x80 : 0x00)
                    );
                case 0x1D: return (
                    (sprites[0].ExpandX ? 0x01 : 0x00) |
                    (sprites[1].ExpandX ? 0x02 : 0x00) |
                    (sprites[2].ExpandX ? 0x04 : 0x00) |
                    (sprites[3].ExpandX ? 0x08 : 0x00) |
                    (sprites[4].ExpandX ? 0x10 : 0x00) |
                    (sprites[5].ExpandX ? 0x20 : 0x00) |
                    (sprites[6].ExpandX ? 0x40 : 0x00) |
                    (sprites[7].ExpandX ? 0x80 : 0x00)
                    );
                case 0x1E: return (
                    (sprites[0].SpriteCollision ? 0x01 : 0x00) |
                    (sprites[1].SpriteCollision ? 0x02 : 0x00) |
                    (sprites[2].SpriteCollision ? 0x04 : 0x00) |
                    (sprites[3].SpriteCollision ? 0x08 : 0x00) |
                    (sprites[4].SpriteCollision ? 0x10 : 0x00) |
                    (sprites[5].SpriteCollision ? 0x20 : 0x00) |
                    (sprites[6].SpriteCollision ? 0x40 : 0x00) |
                    (sprites[7].SpriteCollision ? 0x80 : 0x00)
                    );
                case 0x1F: return (
                    (sprites[0].DataCollision ? 0x01 : 0x00) |
                    (sprites[1].DataCollision ? 0x02 : 0x00) |
                    (sprites[2].DataCollision ? 0x04 : 0x00) |
                    (sprites[3].DataCollision ? 0x08 : 0x00) |
                    (sprites[4].DataCollision ? 0x10 : 0x00) |
                    (sprites[5].DataCollision ? 0x20 : 0x00) |
                    (sprites[6].DataCollision ? 0x40 : 0x00) |
                    (sprites[7].DataCollision ? 0x80 : 0x00)
                    );
                case 0x20: return borderColor | 0xF0;
                case 0x21: return backgroundColor[0] | 0xF0;
                case 0x22: return backgroundColor[1] | 0xF0;
                case 0x23: return backgroundColor[2] | 0xF0;
                case 0x24: return backgroundColor[3] | 0xF0;
                case 0x25: return spriteMultiColor[0] | 0xF0;
                case 0x26: return spriteMultiColor[1] | 0xF0;
                case 0x27: return sprites[0].Color | 0xF0;
                case 0x28: return sprites[1].Color | 0xF0;
                case 0x29: return sprites[2].Color | 0xF0;
                case 0x2A: return sprites[3].Color | 0xF0;
                case 0x2B: return sprites[4].Color | 0xF0;
                case 0x2C: return sprites[5].Color | 0xF0;
                case 0x2D: return sprites[6].Color | 0xF0;
                case 0x2E: return sprites[7].Color | 0xF0;
                default: return 0xFF;
            }
        }

        public void Poke(int addr, int val)
        {
            switch (addr)
            {
                case 0x00: sprites[0].X = (sprites[0].X & 0x100 | val); return;
                case 0x01: sprites[1].X = (sprites[1].X & 0x100 | val); return;
                case 0x02: sprites[2].X = (sprites[2].X & 0x100 | val); return;
                case 0x03: sprites[3].X = (sprites[3].X & 0x100 | val); return;
                case 0x04: sprites[4].X = (sprites[4].X & 0x100 | val); return;
                case 0x05: sprites[5].X = (sprites[5].X & 0x100 | val); return;
                case 0x06: sprites[6].X = (sprites[6].X & 0x100 | val); return;
                case 0x07: sprites[7].X = (sprites[7].X & 0x100 | val); return;
                case 0x08: sprites[0].Y = val; return;
                case 0x09: sprites[1].Y = val; return;
                case 0x0A: sprites[2].Y = val; return;
                case 0x0B: sprites[3].Y = val; return;
                case 0x0C: sprites[4].Y = val; return;
                case 0x0D: sprites[5].Y = val; return;
                case 0x0E: sprites[6].Y = val; return;
                case 0x0F: sprites[7].Y = val; return;
                case 0x10: 
                    sprites[0].X = (sprites[0].X & 0xFF) | ((val & 0x01) << 8);
                    sprites[1].X = (sprites[1].X & 0xFF) | ((val & 0x02) << 7);
                    sprites[2].X = (sprites[2].X & 0xFF) | ((val & 0x04) << 6);
                    sprites[3].X = (sprites[3].X & 0xFF) | ((val & 0x08) << 5);
                    sprites[4].X = (sprites[4].X & 0xFF) | ((val & 0x10) << 4);
                    sprites[5].X = (sprites[5].X & 0xFF) | ((val & 0x20) << 3);
                    sprites[6].X = (sprites[6].X & 0xFF) | ((val & 0x40) << 2);
                    sprites[7].X = (sprites[7].X & 0xFF) | ((val & 0x80) << 1);
                    return;
                case 0x11:
                    yScroll = (val & 0x07);
                    rowSelect = ((val & 0x08) != 0);
                    displayEnable = ((val & 0x10) != 0);
                    bitmapMode = ((val & 0x20) != 0);
                    extraColorMode = ((val & 0x40) != 0);
                    rasterY = (rasterY & 0xFF) | ((val & 0x80) << 1);
                    return;
                case 0x12: rasterY = (rasterY & 0x100) | val; return;
                case 0x13: lightPenX = val; return;
                case 0x14: lightPenY = val; return;
                case 0x15:
                    sprites[0].Enabled = ((val & 0x01) != 0);
                    sprites[1].Enabled = ((val & 0x02) != 0);
                    sprites[2].Enabled = ((val & 0x04) != 0);
                    sprites[3].Enabled = ((val & 0x08) != 0);
                    sprites[4].Enabled = ((val & 0x10) != 0);
                    sprites[5].Enabled = ((val & 0x20) != 0);
                    sprites[6].Enabled = ((val & 0x40) != 0);
                    sprites[7].Enabled = ((val & 0x80) != 0);
                    return;
                case 0x16:
                    xScroll = (val & 0x07);
                    columnSelect = ((val & 0x08) != 0);
                    multiColorMode = ((val & 0x08) != 0);
                    reset = ((val & 0x08) != 0);
                    return;
                case 0x17:
                    sprites[0].ExpandY = ((val & 0x01) != 0);
                    sprites[1].ExpandY = ((val & 0x02) != 0);
                    sprites[2].ExpandY = ((val & 0x04) != 0);
                    sprites[3].ExpandY = ((val & 0x08) != 0);
                    sprites[4].ExpandY = ((val & 0x10) != 0);
                    sprites[5].ExpandY = ((val & 0x20) != 0);
                    sprites[6].ExpandY = ((val & 0x40) != 0);
                    sprites[7].ExpandY = ((val & 0x80) != 0);
                    return;
                case 0x18:
                    videoMemory = (val & 0xF0) << 6;
                    characterBitmap = (val & 0x0E) << 10;
                    return;
                case 0x19:
                    rasterInterrupt = ((val & 0x01) != 0);
                    dataCollisionInterrupt = ((val & 0x02) != 0);
                    spriteCollisionInterrupt = ((val & 0x04) != 0);
                    lightPenInterrupt = ((val & 0x08) != 0);
                    irq = ((val & 0x80) != 0);
                    return;
                case 0x1A:
                    rasterInterruptEnable = ((val & 0x01) != 0);
                    dataCollisionInterruptEnable = ((val & 0x02) != 0);
                    spriteCollisionInterruptEnable = ((val & 0x04) != 0);
                    lightPenInterruptEnable = ((val & 0x08) != 0);
                    return;
                case 0x1B:
                    sprites[0].Priority = ((val & 0x01) != 0);
                    sprites[1].Priority = ((val & 0x02) != 0);
                    sprites[2].Priority = ((val & 0x04) != 0);
                    sprites[3].Priority = ((val & 0x08) != 0);
                    sprites[4].Priority = ((val & 0x10) != 0);
                    sprites[5].Priority = ((val & 0x20) != 0);
                    sprites[6].Priority = ((val & 0x40) != 0);
                    sprites[7].Priority = ((val & 0x80) != 0);
                    return;
                case 0x1C:
                    sprites[0].Multicolor = ((val & 0x01) != 0);
                    sprites[1].Multicolor = ((val & 0x02) != 0);
                    sprites[2].Multicolor = ((val & 0x04) != 0);
                    sprites[3].Multicolor = ((val & 0x08) != 0);
                    sprites[4].Multicolor = ((val & 0x10) != 0);
                    sprites[5].Multicolor = ((val & 0x20) != 0);
                    sprites[6].Multicolor = ((val & 0x40) != 0);
                    sprites[7].Multicolor = ((val & 0x80) != 0);
                    return;
                case 0x1D:
                    sprites[0].ExpandX = ((val & 0x01) != 0);
                    sprites[1].ExpandX = ((val & 0x02) != 0);
                    sprites[2].ExpandX = ((val & 0x04) != 0);
                    sprites[3].ExpandX = ((val & 0x08) != 0);
                    sprites[4].ExpandX = ((val & 0x10) != 0);
                    sprites[5].ExpandX = ((val & 0x20) != 0);
                    sprites[6].ExpandX = ((val & 0x40) != 0);
                    sprites[7].ExpandX = ((val & 0x80) != 0);
                    return;
                case 0x1E:
                    sprites[0].SpriteCollision = ((val & 0x01) != 0);
                    sprites[1].SpriteCollision = ((val & 0x02) != 0);
                    sprites[2].SpriteCollision = ((val & 0x04) != 0);
                    sprites[3].SpriteCollision = ((val & 0x08) != 0);
                    sprites[4].SpriteCollision = ((val & 0x10) != 0);
                    sprites[5].SpriteCollision = ((val & 0x20) != 0);
                    sprites[6].SpriteCollision = ((val & 0x40) != 0);
                    sprites[7].SpriteCollision = ((val & 0x80) != 0);
                    return;
                case 0x1F:
                    sprites[0].DataCollision = ((val & 0x01) != 0);
                    sprites[1].DataCollision = ((val & 0x02) != 0);
                    sprites[2].DataCollision = ((val & 0x04) != 0);
                    sprites[3].DataCollision = ((val & 0x08) != 0);
                    sprites[4].DataCollision = ((val & 0x10) != 0);
                    sprites[5].DataCollision = ((val & 0x20) != 0);
                    sprites[6].DataCollision = ((val & 0x40) != 0);
                    sprites[7].DataCollision = ((val & 0x80) != 0);
                    return;
                case 0x20: borderColor = val & 0x0F; return;
                case 0x21: backgroundColor[0] = val & 0x0F; return;
                case 0x22: backgroundColor[1] = val & 0x0F; return;
                case 0x23: backgroundColor[2] = val & 0x0F; return;
                case 0x24: backgroundColor[3] = val & 0x0F; return;
                case 0x25: spriteMultiColor[0] = val & 0x0F; return;
                case 0x26: spriteMultiColor[1] = val & 0x0F; return;
                case 0x27: sprites[0].Color = val & 0x0F; return;
                case 0x28: sprites[1].Color = val & 0x0F; return;
                case 0x29: sprites[2].Color = val & 0x0F; return;
                case 0x2A: sprites[3].Color = val & 0x0F; return;
                case 0x2B: sprites[4].Color = val & 0x0F; return;
                case 0x2C: sprites[5].Color = val & 0x0F; return;
                case 0x2D: sprites[6].Color = val & 0x0F; return;
                case 0x2E: sprites[7].Color = val & 0x0F; return;
                default: return;
            }
        }

        public int Read(int addr)
        {
            int result;
            addr &= 0x3F;

            switch (addr)
            {
                case 0x1E:
                case 0x1F:
                    result = Peek(addr);
                    Poke(addr, 0);
                    return result;
                default:
                    return Peek(addr & 0x3F);
            }
        }

        public void Write(int addr, int val)
        {
            addr &= 0x3F;
            val &= 0xFF;
            switch (addr)
            {
                case 0x19:
                    if ((val & 0x01) != 0)
                        rasterInterrupt = false;
                    if ((val & 0x02) != 0)
                        dataCollisionInterrupt = false;
                    if ((val & 0x04) != 0)
                        spriteCollisionInterrupt = false;
                    if ((val & 0x08) != 0)
                        lightPenInterrupt = false;
                    return;
                case 0x1E:
                case 0x1F:
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
                    return;
                default:
                    Poke(addr, val);
                    return;
            }
        }
    }
}
