using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS.NewVic
{
    public partial class Vic
    {
        private const int TOTAL_RASTERS = 0x138;
        private const int SCREEN_FREQ = 50;
        private const int DISPLAY_X = 0x180;
        private const int DISPLAY_Y = 0x110;

        private const int FIRST_DISP_LINE = 0x10;
        private const int LAST_DISP_LINE = 0x11F;
        private const int FIRST_DMA_LINE = 0x30;
        private const int LAST_DMA_LINE = 0xF7;
        private const int ROW25_YSTART = 0x33;
        private const int ROW25_YSTOP = 0xFB;
        private const int ROW24_YSTART = 0x37;
        private const int ROW24_YSTOP = 0xF7;
        private const int COL40_XSTART = 0x20;
        private const int COL40_XSTOP = 0x160;
        private const int COL38_XSTART = 0x27;
        private const int COL38_XSTOP = 0x157;

        private readonly int[] colors = new int[256];

        private static readonly int[] palette =
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

        private static readonly int[] ExpTable =
        {
            0x0000, 0x0003, 0x000C, 0x000F, 0x0030, 0x0033, 0x003C, 0x003F,
            0x00C0, 0x00C3, 0x00CC, 0x00CF, 0x00F0, 0x00F3, 0x00FC, 0x00FF,
            0x0300, 0x0303, 0x030C, 0x030F, 0x0330, 0x0333, 0x033C, 0x033F,
            0x03C0, 0x03C3, 0x03CC, 0x03CF, 0x03F0, 0x03F3, 0x03FC, 0x03FF,
            0x0C00, 0x0C03, 0x0C0C, 0x0C0F, 0x0C30, 0x0C33, 0x0C3C, 0x0C3F,
            0x0CC0, 0x0CC3, 0x0CCC, 0x0CCF, 0x0CF0, 0x0CF3, 0x0CFC, 0x0CFF,
            0x0F00, 0x0F03, 0x0F0C, 0x0F0F, 0x0F30, 0x0F33, 0x0F3C, 0x0F3F,
            0x0FC0, 0x0FC3, 0x0FCC, 0x0FCF, 0x0FF0, 0x0FF3, 0x0FFC, 0x0FFF,
            0x3000, 0x3003, 0x300C, 0x300F, 0x3030, 0x3033, 0x303C, 0x303F,
            0x30C0, 0x30C3, 0x30CC, 0x30CF, 0x30F0, 0x30F3, 0x30FC, 0x30FF,
            0x3300, 0x3303, 0x330C, 0x330F, 0x3330, 0x3333, 0x333C, 0x333F,
            0x33C0, 0x33C3, 0x33CC, 0x33CF, 0x33F0, 0x33F3, 0x33FC, 0x33FF,
            0x3C00, 0x3C03, 0x3C0C, 0x3C0F, 0x3C30, 0x3C33, 0x3C3C, 0x3C3F,
            0x3CC0, 0x3CC3, 0x3CCC, 0x3CCF, 0x3CF0, 0x3CF3, 0x3CFC, 0x3CFF,
            0x3F00, 0x3F03, 0x3F0C, 0x3F0F, 0x3F30, 0x3F33, 0x3F3C, 0x3F3F,
            0x3FC0, 0x3FC3, 0x3FCC, 0x3FCF, 0x3FF0, 0x3FF3, 0x3FFC, 0x3FFF,
            0xC000, 0xC003, 0xC00C, 0xC00F, 0xC030, 0xC033, 0xC03C, 0xC03F,
            0xC0C0, 0xC0C3, 0xC0CC, 0xC0CF, 0xC0F0, 0xC0F3, 0xC0FC, 0xC0FF,
            0xC300, 0xC303, 0xC30C, 0xC30F, 0xC330, 0xC333, 0xC33C, 0xC33F,
            0xC3C0, 0xC3C3, 0xC3CC, 0xC3CF, 0xC3F0, 0xC3F3, 0xC3FC, 0xC3FF,
            0xCC00, 0xCC03, 0xCC0C, 0xCC0F, 0xCC30, 0xCC33, 0xCC3C, 0xCC3F,
            0xCCC0, 0xCCC3, 0xCCCC, 0xCCCF, 0xCCF0, 0xCCF3, 0xCCFC, 0xCCFF,
            0xCF00, 0xCF03, 0xCF0C, 0xCF0F, 0xCF30, 0xCF33, 0xCF3C, 0xCF3F,
            0xCFC0, 0xCFC3, 0xCFCC, 0xCFCF, 0xCFF0, 0xCFF3, 0xCFFC, 0xCFFF,
            0xF000, 0xF003, 0xF00C, 0xF00F, 0xF030, 0xF033, 0xF03C, 0xF03F,
            0xF0C0, 0xF0C3, 0xF0CC, 0xF0CF, 0xF0F0, 0xF0F3, 0xF0FC, 0xF0FF,
            0xF300, 0xF303, 0xF30C, 0xF30F, 0xF330, 0xF333, 0xF33C, 0xF33F,
            0xF3C0, 0xF3C3, 0xF3CC, 0xF3CF, 0xF3F0, 0xF3F3, 0xF3FC, 0xF3FF,
            0xFC00, 0xFC03, 0xFC0C, 0xFC0F, 0xFC30, 0xFC33, 0xFC3C, 0xFC3F,
            0xFCC0, 0xFCC3, 0xFCCC, 0xFCCF, 0xFCF0, 0xFCF3, 0xFCFC, 0xFCFF,
            0xFF00, 0xFF03, 0xFF0C, 0xFF0F, 0xFF30, 0xFF33, 0xFF3C, 0xFF3F,
            0xFFC0, 0xFFC3, 0xFFCC, 0xFFCF, 0xFFF0, 0xFFF3, 0xFFFC, 0xFFFF
        };

        private static readonly int[] MultiExpTable =
        {
            0x0000, 0x0005, 0x000A, 0x000F, 0x0050, 0x0055, 0x005A, 0x005F,
            0x00A0, 0x00A5, 0x00AA, 0x00AF, 0x00F0, 0x00F5, 0x00FA, 0x00FF,
            0x0500, 0x0505, 0x050A, 0x050F, 0x0550, 0x0555, 0x055A, 0x055F,
            0x05A0, 0x05A5, 0x05AA, 0x05AF, 0x05F0, 0x05F5, 0x05FA, 0x05FF,
            0x0A00, 0x0A05, 0x0A0A, 0x0A0F, 0x0A50, 0x0A55, 0x0A5A, 0x0A5F,
            0x0AA0, 0x0AA5, 0x0AAA, 0x0AAF, 0x0AF0, 0x0AF5, 0x0AFA, 0x0AFF,
            0x0F00, 0x0F05, 0x0F0A, 0x0F0F, 0x0F50, 0x0F55, 0x0F5A, 0x0F5F,
            0x0FA0, 0x0FA5, 0x0FAA, 0x0FAF, 0x0FF0, 0x0FF5, 0x0FFA, 0x0FFF,
            0x5000, 0x5005, 0x500A, 0x500F, 0x5050, 0x5055, 0x505A, 0x505F,
            0x50A0, 0x50A5, 0x50AA, 0x50AF, 0x50F0, 0x50F5, 0x50FA, 0x50FF,
            0x5500, 0x5505, 0x550A, 0x550F, 0x5550, 0x5555, 0x555A, 0x555F,
            0x55A0, 0x55A5, 0x55AA, 0x55AF, 0x55F0, 0x55F5, 0x55FA, 0x55FF,
            0x5A00, 0x5A05, 0x5A0A, 0x5A0F, 0x5A50, 0x5A55, 0x5A5A, 0x5A5F,
            0x5AA0, 0x5AA5, 0x5AAA, 0x5AAF, 0x5AF0, 0x5AF5, 0x5AFA, 0x5AFF,
            0x5F00, 0x5F05, 0x5F0A, 0x5F0F, 0x5F50, 0x5F55, 0x5F5A, 0x5F5F,
            0x5FA0, 0x5FA5, 0x5FAA, 0x5FAF, 0x5FF0, 0x5FF5, 0x5FFA, 0x5FFF,
            0xA000, 0xA005, 0xA00A, 0xA00F, 0xA050, 0xA055, 0xA05A, 0xA05F,
            0xA0A0, 0xA0A5, 0xA0AA, 0xA0AF, 0xA0F0, 0xA0F5, 0xA0FA, 0xA0FF,
            0xA500, 0xA505, 0xA50A, 0xA50F, 0xA550, 0xA555, 0xA55A, 0xA55F,
            0xA5A0, 0xA5A5, 0xA5AA, 0xA5AF, 0xA5F0, 0xA5F5, 0xA5FA, 0xA5FF,
            0xAA00, 0xAA05, 0xAA0A, 0xAA0F, 0xAA50, 0xAA55, 0xAA5A, 0xAA5F,
            0xAAA0, 0xAAA5, 0xAAAA, 0xAAAF, 0xAAF0, 0xAAF5, 0xAAFA, 0xAAFF,
            0xAF00, 0xAF05, 0xAF0A, 0xAF0F, 0xAF50, 0xAF55, 0xAF5A, 0xAF5F,
            0xAFA0, 0xAFA5, 0xAFAA, 0xAFAF, 0xAFF0, 0xAFF5, 0xAFFA, 0xAFFF,
            0xF000, 0xF005, 0xF00A, 0xF00F, 0xF050, 0xF055, 0xF05A, 0xF05F,
            0xF0A0, 0xF0A5, 0xF0AA, 0xF0AF, 0xF0F0, 0xF0F5, 0xF0FA, 0xF0FF,
            0xF500, 0xF505, 0xF50A, 0xF50F, 0xF550, 0xF555, 0xF55A, 0xF55F,
            0xF5A0, 0xF5A5, 0xF5AA, 0xF5AF, 0xF5F0, 0xF5F5, 0xF5FA, 0xF5FF,
            0xFA00, 0xFA05, 0xFA0A, 0xFA0F, 0xFA50, 0xFA55, 0xFA5A, 0xFA5F,
            0xFAA0, 0xFAA5, 0xFAAA, 0xFAAF, 0xFAF0, 0xFAF5, 0xFAFA, 0xFAFF,
            0xFF00, 0xFF05, 0xFF0A, 0xFF0F, 0xFF50, 0xFF55, 0xFF5A, 0xFF5F,
            0xFFA0, 0xFFA5, 0xFFAA, 0xFFAF, 0xFFF0, 0xFFF5, 0xFFFA, 0xFFFF
        };

        public Func<int, int> ReadMemory;
        public Func<int, int> ReadColorMemory;

        private readonly int[] mx = new int[8];
        private readonly int[] my = new int[8];
        private int mx8;
        private int ctrl1;
        private int ctrl2;
        private int lpx;
        private int lpy;
        private int me;
        private int mxe;
        private int mye;
        private int mdp;
        private int mmc;
        private int vbase;
        private int irq_flag;
        private int irq_mask;
        private int clx_spr;
        private int clx_bgr;
        private int ec;
        private int b0c;
        private int b1c;
        private int b2c;
        private int b3c;
        private int mm0;
        private int mm1;
        private readonly int[] sc = new int[8];
        private int ec_color;
        private int b0c_color;
        private int b1c_color;
        private int b2c_color;
        private int b3c_color;
        private int mm0_color;
        private int mm1_color;
        private readonly int[] spr_color = new int[8];
        private readonly int[] matrix_line = new int[40];
        private readonly int[] color_line = new int[40];
        private int raster_x;
        private int raster_y;
        private int irq_raster;
        private int dy_start;
        private int dy_stop;
        private int rc;
        private int vc;
        private int vc_base;
        private int x_scroll;
        private int y_scroll;
        private int cia_vabase;
        private int cycle;
        private int display_idx;
        private int ml_index;
        private int skip_counter;
        private readonly int[] mc = new int[8];
        private readonly int[] mc_base = new int[8];
        private readonly int[] spr_coll_buf = new int[0x180];
        private int fore_mask_ptr;
        private readonly int[] fore_mask_buf = new int[0x180/8];
        private bool display_state;
        private bool border_on;
        private bool frame_skipped;
        private bool bad_lines_enabled;
        private bool lp_triggered;
        private bool is_bad_line;
        private bool draw_this_line;
        private bool ud_border_on;
        private bool vblanking;
        private readonly bool[] border_on_sample = new bool[5];
        private readonly int[] border_color_sample = new int[DISPLAY_X/8];
        private int matrix_base;
        private int char_base;
        private int bitmap_base;
        private int ref_cnt;
        private int spr_exp_y;
        private int spr_dma_on;
        private int spr_disp_on;
        private int spr_draw;
        private readonly int[] spr_ptr = new int[8];
        private int gfx_data;
        private int char_data;
        private int color_data;
        private int last_char_data;
        private readonly int[][] spr_data = new int[8][];
        private readonly int[][] spr_draw_data = new int[8][];
        private int first_ba_cycle;
        private int ret;
        private int new_irq_raster;
        private bool ba;
        private int cpucycle;
        private bool aec;
        private int fetch_adr;
        private int draw_color;
        private int[] output_frame_buffer;
        private int chunky_ptr;
        private int output_frame_buffer_ptr;
        private int[] draw_colors = new int[4];
        private int draw_graphics_data;
        private int memset8_index;

        public Vic()
        {
            for (var i = 0; i < 0xF; i++)
            {
                for (var j = 0; j < 0xF; j++)
                {
                    colors[(i << 4) | j] = palette[j];
                }
            }
            HardReset();
        }

        public void HardReset()
        {
            matrix_base = 0;
            char_base = 0;
            bitmap_base = 0;
            mx8 = 0;
            ctrl1 = 0;
            ctrl2 = 0;
            lpx = 0;
            lpy = 0;
            me = 0;
            mxe = 0;
            mye = 0;
            mdp = 0;
            mmc = 0;
            vbase = 0;
            irq_flag = 0;
            irq_mask = 0;
            clx_spr = 0;
            clx_bgr = 0;
            cia_vabase = 0;
            ec = 0;
            b0c = 0;
            b1c = 0;
            b2c = 0;
            b3c = 0;
            mm0 = 0;
            mm1 = 0;
            for (var i = 0; i < 8; i++)
            {
                mx[i] = 0;
                my[i] = 0;
                sc[i] = 0;
                for (var j = 0; j < 4; j++)
                {
                    spr_data[i][j] = 0;
                    spr_draw_data[i][j] = 0;
                }
                mc[i] = 63;
                spr_ptr[i] = 0;
                spr_color[i] = colors[0];
            }
            raster_y = TOTAL_RASTERS - 1;
            rc = 7;
            irq_raster = 0;
            vc = 0;
            vc_base = 0;
            x_scroll = 0;
            y_scroll = 0;
            dy_start = ROW24_YSTART;
            dy_stop = ROW24_YSTOP;
            ml_index = 0;
            cycle = 1;
            display_idx = 0;
            display_state = false;
            border_on = false;
            ud_border_on = false;
            vblanking = false;
            lp_triggered = false;
            draw_this_line = false;
            spr_dma_on = 0;
            spr_disp_on = 0;
            frame_skipped = false;
            skip_counter = 1;
            for (var i = 0; i < 0x180; i++)
            {
                spr_coll_buf[i] = 0;
            }
            for (var i = 0; i < 0x180/8; i++)
            {
                fore_mask_buf[i] = 0;
            }
            ec_color = colors[0];
            b0c_color = colors[0];
            b1c_color = colors[0];
            b2c_color = colors[0];
            b3c_color = colors[0];
            mm0_color = colors[0];
            mm1_color = colors[0];
        }

        private void raster_irq()
        {
            irq_flag |= 0x01;
            if ((irq_mask & 0x01) != 0)
            {
                irq_flag |= 0x80;
            }
        }

        private void TriggerLightpen()
        {
            if (lp_triggered) return;

            lp_triggered = true;

            lpx = raster_x >> 1;
            lpy = raster_y;

            irq_flag |= 0x08;
            if ((irq_mask & 0x08) != 0)
            {
                irq_flag |= 0x80;
            }
        }

        private void MatrixAccess()
        {
            if (ba) return;
            if (cpucycle - first_ba_cycle < 3)
            {
                matrix_line[ml_index] = 0xFF;
                color_line[ml_index] = 0xFF;
            }
            else
            {
                fetch_adr = (vc & 0x03FF) | matrix_base;
                matrix_line[ml_index] = ReadMemory(fetch_adr);
                color_line[ml_index] = ReadColorMemory(fetch_adr);
            }
        }

        private void GraphicsAccess()
        {
            if (!display_state)
            {
                gfx_data = ReadMemory((ctrl1 & 0x40) != 0 ? 0x39FF : 0x3FFF);
                char_data = 0;
                color_data = 0;
            }
            else
            {
                if ((ctrl1 & 0x20) != 0)
                    fetch_adr = ((vc & 0x03ff) << 3) | bitmap_base | rc;
                else
                    fetch_adr = (matrix_line[ml_index] << 3) | char_base | rc;
                if ((ctrl1 & 0x40) != 0)
                    fetch_adr &= 0xf9ff;
                gfx_data = ReadMemory(fetch_adr);
                char_data = matrix_line[ml_index];
                color_data = color_line[ml_index];
                ml_index++;
                vc++;
            }
        }

        private void DrawBackground()
        {
            if (!draw_this_line)
            {
                return;
            }

            switch (display_idx)
            {
                case 0:
                case 1:
                case 3:
                    draw_color = b0c_color;
                    break;
                case 2:
                    draw_color = colors[last_char_data];
                    break;
                case 4:
                    if ((last_char_data & 0x80) != 0)
                    {
                        draw_color = (last_char_data & 0x40) != 0
                            ? b3c_color
                            : b2c_color;
                        break;
                    }
                    draw_color = (last_char_data & 0x40) != 0
                        ? b1c_color
                        : b0c_color;
                    break;
                default:
                    draw_color = colors[0];
                    break;
            }

            MemSet8(chunky_ptr, draw_color);
        }

        private void MemSet8(int index, int color)
        {
            memset8_index = 8;
            while (--memset8_index > 0)
            {
                output_frame_buffer[index++] = color;
            }
        }

        private void draw_std()
        {
            fore_mask_buf[fore_mask_ptr] |= gfx_data >> x_scroll;
            fore_mask_buf[fore_mask_ptr + 1] |= gfx_data << (7 - x_scroll);

            draw_graphics_data = gfx_data;
            output_frame_buffer[output_frame_buffer_ptr + 7] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 6] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 5] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 4] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 3] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 2] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 1] = draw_colors[draw_graphics_data & 1]; draw_graphics_data >>= 1;
            output_frame_buffer[output_frame_buffer_ptr + 0] = draw_colors[draw_graphics_data];
        }

        private void draw_multi()
        {
            fore_mask_buf[fore_mask_ptr] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) >> x_scroll;
            fore_mask_buf[fore_mask_ptr + 1] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) << (8 - x_scroll);

            draw_graphics_data = gfx_data;
            output_frame_buffer[output_frame_buffer_ptr + 7] = output_frame_buffer[output_frame_buffer_ptr + 6] = draw_colors[draw_graphics_data & 3]; draw_graphics_data >>= 2;
            output_frame_buffer[output_frame_buffer_ptr + 5] = output_frame_buffer[output_frame_buffer_ptr + 4] = draw_colors[draw_graphics_data & 3]; draw_graphics_data >>= 2;
            output_frame_buffer[output_frame_buffer_ptr + 3] = output_frame_buffer[output_frame_buffer_ptr + 2] = draw_colors[draw_graphics_data & 3]; draw_graphics_data >>= 2;
            output_frame_buffer[output_frame_buffer_ptr + 1] = output_frame_buffer[output_frame_buffer_ptr + 0] = draw_colors[draw_graphics_data & 3];
        }

        private void DrawGraphics()
        {
            if (!draw_this_line)
            {
                return;
            }

            if (ud_border_on)
            {
                DrawBackground();
                return;
            }

            output_frame_buffer_ptr = chunky_ptr + x_scroll;
            switch (display_idx)
            {
                case 0:
                    draw_colors[0] = b0c_color;
                    draw_colors[1] = colors[color_data];
                    draw_std();
                    return;
                case 1:
                    if ((color_data & 8) != 0)
                    {
                        draw_colors[0] = b0c_color;
                        draw_colors[1] = b1c_color;
                        draw_colors[2] = b2c_color;
                        draw_colors[3] = colors[color_data & 7];
                        draw_multi();
                        return;
                    }
                    draw_colors[0] = b0c_color;
                    draw_colors[1] = colors[color_data];
                    draw_std();
                    return;
                case 2:
                    draw_colors[0] = colors[char_data];
                    draw_colors[1] = colors[char_data >> 4];
                    draw_std();
                    return;
                case 3:
                    draw_colors[0] = b0c_color;
                    draw_colors[1] = colors[char_data >> 4];
                    draw_colors[2] = colors[char_data];
                    draw_colors[3] = colors[color_data];
                    draw_multi();
                    return;
                case 4:
                    if ((char_data & 0x80) != 0)
                    {
                        draw_colors[0] = (char_data & 0x40) != 0
                            ? b3c_color
                            : b2c_color;
                    }
                    else
                    {
                        draw_colors[0] = (char_data & 0x40) != 0
                            ? b1c_color
                            : b0c_color;
                    }
                    draw_colors[1] = colors[color_data];
                    draw_std();
                    return;
                case 5:
                    MemSet8(output_frame_buffer_ptr, colors[0]);
                    if ((color_data & 8) != 0)
                    {
                        fore_mask_buf[fore_mask_ptr] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) >> x_scroll;
                        fore_mask_buf[fore_mask_ptr + 1] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) <<
                                                            (8 - x_scroll);
                    }
                    else
                    {
                        fore_mask_buf[fore_mask_ptr] |= gfx_data >> x_scroll;
                        fore_mask_buf[fore_mask_ptr + 1] |= gfx_data << (7 - x_scroll);
                    }
                    return;
                case 6:
                    MemSet8(output_frame_buffer_ptr, colors[0]);
                    fore_mask_buf[fore_mask_ptr] |= gfx_data >> x_scroll;
                    fore_mask_buf[fore_mask_ptr + 1] |= gfx_data << (7 - x_scroll);
                    return;
                case 7:
                    MemSet8(output_frame_buffer_ptr, colors[0]);
                    fore_mask_buf[fore_mask_ptr] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) >> x_scroll;
                    fore_mask_buf[fore_mask_ptr + 1] |= ((gfx_data & 0xaa) | (gfx_data & 0xaa) >> 1) << (8 - x_scroll);
                    return;
            }
        }
    }
}
