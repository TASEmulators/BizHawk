using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS.NewVic
{
    public partial class Vic
    {
        public int Peek(int addr)
        {
            addr &= 0x3F;
            return ReadRegister(addr);
        }

        public int Read(int addr)
        {
            addr &= 0x3F;
            switch (addr)
            {
                case 0x1E:
                    ret = clx_spr;
                    clx_spr = 0;
                    return ret;
                case 0x1F:
                    ret = clx_bgr;
                    clx_bgr = 0;
                    return ret;
                default:
                    return ReadRegister(addr);
            }
        }

        private int ReadRegister(int addr)
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
                    return mx[addr >> 1];
                case 0x01:
                case 0x03:
                case 0x05:
                case 0x07:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                    return my[addr >> 1];
                case 0x10:
                    return mx8;
                case 0x11:
                    return (ctrl1 & 0x7F) | ((raster_y & 0x100) >> 1);
                case 0x12:
                    return raster_y & 0xFF;
                case 0x13:
                    return lpx;
                case 0x14:
                    return lpy;
                case 0x15:
                    return me;
                case 0x16:
                    return ctrl2 | 0xC0;
                case 0x17:
                    return mye;
                case 0x18:
                    return vbase | 0x01;
                case 0x19:
                    return irq_flag | 0x70;
                case 0x1A:
                    return irq_mask | 0xF0;
                case 0x1B:
                    return mdp;
                case 0x1C:
                    return mmc;
                case 0x1D:
                    return mxe;
                case 0x1E:
                    return clx_spr;
                case 0x1F:
                    return clx_bgr;
                case 0x20:
                    return ec | 0xF0;
                case 0x21:
                    return b0c | 0xF0;
                case 0x22:
                    return b1c | 0xF0;
                case 0x23:
                    return b2c | 0xF0;
                case 0x24:
                    return b3c | 0xF0;
                case 0x25:
                    return mm0 | 0xF0;
                case 0x26:
                    return mm1 | 0xF0;
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                    return sc[addr - 0x27] | 0xF0;
                default:
                    return 0xFF;
            }
        }

        public void Poke(int addr, int val)
        {
            addr &= 0x3F;
            WriteRegister(addr, val);
        }

        public void Write(int addr, int val)
        {
            addr &= 0x3F;
            switch (addr)
            {
                case 0x11:
                    ctrl1 = val;
                    y_scroll = val & 7;
                    new_irq_raster = (irq_raster & 0xFF) | ((val & 0x80) << 1);
                    if (irq_raster != new_irq_raster && raster_y == new_irq_raster)
                    {
                        raster_irq();
                    }
                    irq_raster = new_irq_raster;

                    if ((val & 8) != 0)
                    {
                        dy_start = ROW25_YSTART;
                        dy_stop = ROW25_YSTOP;
                    }
                    else
                    {
                        dy_start = ROW24_YSTART;
                        dy_stop = ROW24_YSTOP;
                    }

                    if (raster_y == 0x30 && (val & 0x10) != 0)
                    {
                        bad_lines_enabled = true;
                    }

                    is_bad_line = raster_y >= FIRST_DMA_LINE && raster_y <= LAST_DMA_LINE &&
                                  ((raster_y & 7) == y_scroll) && bad_lines_enabled;
                    display_idx = ((ctrl1 & 0x60) | (ctrl2 & 0x10)) >> 4;
                    break;
                case 0x12:
                    new_irq_raster = (irq_raster & 0xFF00) | val;
                    if (irq_raster != new_irq_raster && raster_y == new_irq_raster)
                    {
                        raster_irq();
                    }
                    irq_raster = new_irq_raster;
                    break;
                default:
                    WriteRegister(addr, val);
                    break;
            }
        }

        private void WriteRegister(int addr, int val)
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
                    mx[addr >> 1] = (mx[addr >> 1] & 0xFF00) | val;
                    break;
                case 0x01:
                case 0x03:
                case 0x05:
                case 0x07:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                    my[addr >> 1] = val;
                    break;
                case 0x10:
                    mx8 = val;
                    mx[0] = mx[0] & 0xFF | ((val & 0x01) << 8);
                    mx[1] = mx[1] & 0xFF | ((val & 0x02) << 7);
                    mx[2] = mx[2] & 0xFF | ((val & 0x04) << 6);
                    mx[3] = mx[3] & 0xFF | ((val & 0x08) << 5);
                    mx[4] = mx[4] & 0xFF | ((val & 0x10) << 4);
                    mx[5] = mx[5] & 0xFF | ((val & 0x20) << 3);
                    mx[6] = mx[6] & 0xFF | ((val & 0x40) << 2);
                    mx[7] = mx[7] & 0xFF | ((val & 0x80) << 1);
                    break;
                case 0x11:
                    ctrl1 = val;
                    y_scroll = val & 7;
                    irq_raster = (irq_raster & 0xFF) | ((val & 0x80) << 1);

                    if ((val & 8) != 0)
                    {
                        dy_start = ROW25_YSTART;
                        dy_stop = ROW25_YSTOP;
                    }
                    else
                    {
                        dy_start = ROW24_YSTART;
                        dy_stop = ROW24_YSTOP;
                    }

                    if (raster_y == 0x30 && (val & 0x10) != 0)
                    {
                        bad_lines_enabled = true;
                    }

                    is_bad_line = raster_y >= FIRST_DMA_LINE && raster_y <= LAST_DMA_LINE &&
                                  ((raster_y & 7) == y_scroll) && bad_lines_enabled;
                    display_idx = ((ctrl1 & 0x60) | (ctrl2 & 0x10)) >> 4;
                    break;
                case 0x12:
                    irq_raster = (irq_raster & 0xFF00) | val;
                    break;
                case 0x15:
                    me = val;
                    break;
                case 0x16:
                    ctrl2 = val;
                    x_scroll = val & 7;
                    display_idx = ((ctrl1 & 0x60) | (ctrl2 & 0x10)) >> 4;
                    break;
                case 0x17:
                    mye = val;
                    spr_exp_y |= ~val;
                    break;
                case 0x18:
                    vbase = val;
                    matrix_base = (val & 0xf0) << 6;
                    char_base = (val & 0x0e) << 10;
                    bitmap_base = (val & 0x08) << 10;
                    break;
                case 0x19:
                    irq_flag = irq_flag & ~val & 0x0f;
                    if ((irq_flag & irq_mask) != 0)
                    {
                        irq_flag |= 0x80;
                    }
                    break;
                case 0x1A:
                    irq_mask = val & 0x0f;
                    if ((irq_flag & irq_mask) != 0)
                    {
                        irq_flag |= 0x80;
                    }
                    else {
                        irq_flag &= 0x7f;
                    }
                    break;
                case 0x1B:
                    mdp = val;
                    break;
                case 0x1C:
                    mmc = val;
                    break;
                case 0x1D:
                    mxe = val;
                    break;
                case 0x20:
                    ec = val;
                    ec_color = colors[ec];
                    break;
                case 0x21:
                    b0c = val;
                    b0c_color = colors[b0c];
                    break;
                case 0x22:
                    b1c = val;
                    b1c_color = colors[b1c];
                    break;
                case 0x23:
                    b2c = val;
                    b2c_color = colors[b2c];
                    break;
                case 0x24:
                    b3c = val;
                    b3c_color = colors[b3c];
                    break;
                case 0x25:
                    mm0 = val;
                    mm0_color = colors[mm0];
                    break;
                case 0x26:
                    mm1 = val;
                    mm1_color = colors[mm1];
                    break;
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                    sc[addr - 0x27] = val;
                    spr_color[addr - 0x27] = colors[val];
                    break;
            }
        }
    }
}
