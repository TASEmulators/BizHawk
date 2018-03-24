using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class PPU
	{
		public GBHawk Core { get; set; }

		// register variables
		public byte LCDC;
		public byte STAT;
		public byte scroll_y;
		public byte scroll_x;
		public byte LY;
		public byte LY_inc;
		public byte LYC;
		public byte DMA_addr;
		public byte BGP;
		public byte obj_pal_0;
		public byte obj_pal_1;
		public byte window_y;
		public byte window_x;
		public bool DMA_start;
		public int DMA_clock;
		public int DMA_inc;
		public byte DMA_byte;

		// state variables
		public int cycle;
		public bool LYC_INT;
		public bool HBL_INT;
		public bool VBL_INT;
		public bool OAM_INT;
		public bool LCD_was_off;
		public bool stat_line;
		public bool stat_line_old;
		public int hbl_countdown;
		// OAM scan
		public bool DMA_OAM_access;
		public bool OAM_access_read;
		public bool OAM_access_write;
		public int OAM_scan_index;
		public int SL_sprites_index;
		public int[] SL_sprites = new int[40];
		public int write_sprite;
		public bool no_scan;
		// render
		public bool VRAM_access_read;
		public bool VRAM_access_write;
		public int read_case;
		public int internal_cycle;
		public int y_tile;
		public int y_scroll_offset;
		public int x_tile;
		public int x_scroll_offset;
		public int tile_byte;
		public int sprite_fetch_cycles;
		public bool fetch_sprite;
		public bool fetch_sprite_01;
		public bool fetch_sprite_4;
		public bool going_to_fetch;
		public int sprite_fetch_counter;
		public byte[] sprite_attr_list = new byte[160];
		public byte[] sprite_pixel_list = new byte[160];
		public byte[] sprite_present_list = new byte[160];
		public int temp_fetch;
		public int tile_inc;
		public bool pre_render;
		public byte[] tile_data = new byte[2];
		public byte[] tile_data_latch = new byte[2];
		public int latch_counter;
		public bool latch_new_data;
		public int render_counter;
		public int render_offset;
		public int pixel_counter;
		public int pixel;
		public byte[] sprite_data = new byte[2];
		public byte[] sprite_sel = new byte[2];
		public int sl_use_index;
		public bool no_sprites;
		public int[] SL_sprites_ordered = new int[40]; // (x_end, data_low, data_high, attr)
		public int evaled_sprites;
		public int sprite_ordered_index;
		public bool blank_frame;

		// windowing state
		public int window_counter;
		public bool window_pre_render;
		public bool window_started;
		public int window_tile_inc;
		public int window_y_tile;
		public int window_x_tile;
		public int window_y_tile_inc;
		public int window_x_latch;

		public virtual byte ReadReg(int addr)
		{
			return 0;
		}

		public virtual void WriteReg(int addr, byte value)
		{

		}

		public virtual void tick()
		{

		}

		// might be needed, not sure yet
		public virtual void latch_delay()
		{

		}

		public virtual void render(int render_cycle)
		{

		}

		// normal DMA moves twice as fast in double speed mode on GBC
		// So give it it's own function so we can seperate it from PPU tick
		public virtual void DMA_tick()
		{
			// Note that DMA is halted when the CPU is halted
			if (DMA_start && !Core.cpu.halted)
			{
				if (DMA_clock >= 4)
				{
					DMA_OAM_access = false;
					if ((DMA_clock % 4) == 1)
					{
						// the cpu can't access memory during this time, but we still need the ppu to be able to.
						DMA_start = false;
						// Gekkio reports that A14 being high on DMA transfers always represent WRAM accesses
						// So transfers nominally from higher memory areas are actually still from there (i.e. FF -> DF)
						byte DMA_actual = DMA_addr;
						if (DMA_addr > 0xDF) { DMA_actual &= 0xDF; }
						DMA_byte = Core.ReadMemory((ushort)((DMA_actual << 8) + DMA_inc));
						DMA_start = true;
					}
					else if ((DMA_clock % 4) == 3)
					{
						Core.OAM[DMA_inc] = DMA_byte;

						if (DMA_inc < (0xA0 - 1)) { DMA_inc++; }
					}
				}

				DMA_clock++;

				if (DMA_clock == 648)
				{
					DMA_start = false;
					DMA_OAM_access = true;
				}
			}
		}

		public virtual void OAM_scan(int OAM_cycle)
		{
			// we are now in STAT mode 2
			// TODO: maybe stat mode 2 flags are set at cycle 0 on visible scanlines?
			if (OAM_cycle == 0)
			{
				OAM_access_read = false;

				OAM_scan_index = 0;
				SL_sprites_index = 0;
				write_sprite = 0;
			}

			// the gameboy has 80 cycles to scan through 40 sprites, picking out the first 10 it finds to draw
			// the following is a guessed at implmenentation based on how NES does it, it's probably pretty close
			if (OAM_cycle < 10)
			{
				// start by clearing the sprite table (probably just clears X on hardware, but let's be safe here.)
				SL_sprites[OAM_cycle * 4] = 0;
				SL_sprites[OAM_cycle * 4 + 1] = 0;
				SL_sprites[OAM_cycle * 4 + 2] = 0;
				SL_sprites[OAM_cycle * 4 + 3] = 0;
			}
			else
			{
				if (write_sprite == 0)
				{
					if (OAM_scan_index < 40)
					{
						ushort temp = DMA_OAM_access ? Core.OAM[OAM_scan_index * 4] : (ushort)0xFF;
						// (sprite Y - 16) equals LY, we have a sprite
						if ((temp - 16) <= LY &&
							((temp - 16) + 8 + (LCDC.Bit(2) ? 8 : 0)) > LY)
						{
							// always pick the first 10 in range sprites
							if (SL_sprites_index < 10)
							{
								SL_sprites[SL_sprites_index * 4] = temp;

								write_sprite = 1;
							}
							else
							{
								// if we already have 10 sprites, there's nothing to do, increment the index
								OAM_scan_index++;
							}
						}
						else
						{
							OAM_scan_index++;
						}
					}
				}
				else
				{
					ushort temp2 = DMA_OAM_access ? Core.OAM[OAM_scan_index * 4 + write_sprite] : (ushort)0xFF;
					SL_sprites[SL_sprites_index * 4 + write_sprite] = temp2;
					write_sprite++;

					if (write_sprite == 4)
					{
						write_sprite = 0;
						SL_sprites_index++;
						OAM_scan_index++;
					}
				}
			}
		}


		public virtual void Reset()
		{
			LCDC = 0;
			STAT = 0x80;
			scroll_y = 0;
			scroll_x = 0;
			LY = 0;
			LYC = 0;
			DMA_addr = 0xFF;
			BGP = 0xFF;
			obj_pal_0 = 0xFF;
			obj_pal_1 = 0xFF;
			window_y = 0x0;
			window_x = 0x0;
			window_x_latch = 0xFF;
			LY_inc = 1;
			no_scan = false;
			OAM_access_read = true;
			VRAM_access_read = true;
			OAM_access_write = true;
			VRAM_access_write = true;
			DMA_OAM_access = true;

			cycle = 0;
			LYC_INT = false;
			HBL_INT = false;
			VBL_INT = false;
			OAM_INT = false;

			stat_line = false;
			stat_line_old = false;

			window_counter = 0;
			window_pre_render = false;
			window_started = false;
			window_tile_inc = 0;
			window_y_tile = 0;
			window_x_tile = 0;
			window_y_tile_inc = 0;
	}

		public virtual void process_sprite()
		{
			int y;

			if (SL_sprites[sl_use_index * 4 + 3].Bit(6))
			{
				if (LCDC.Bit(2))
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					y = 15 - y;
					sprite_sel[0] = Core.CHR_RAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2];
					sprite_sel[1] = Core.CHR_RAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2 + 1];
				}
				else
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					y = 7 - y;
					sprite_sel[0] = Core.CHR_RAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2];
					sprite_sel[1] = Core.CHR_RAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2 + 1];
				}
			}
			else
			{
				if (LCDC.Bit(2))
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					sprite_sel[0] = Core.CHR_RAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2];
					sprite_sel[1] = Core.CHR_RAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2 + 1];
				}
				else
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					sprite_sel[0] = Core.CHR_RAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2];
					sprite_sel[1] = Core.CHR_RAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2 + 1];
				}
			}

			if (SL_sprites[sl_use_index * 4 + 3].Bit(5))
			{
				int b0, b1, b2, b3, b4, b5, b6, b7 = 0;
				for (int i = 0; i < 2; i++)
				{
					b0 = (sprite_sel[i] & 0x01) << 7;
					b1 = (sprite_sel[i] & 0x02) << 5;
					b2 = (sprite_sel[i] & 0x04) << 3;
					b3 = (sprite_sel[i] & 0x08) << 1;
					b4 = (sprite_sel[i] & 0x10) >> 1;
					b5 = (sprite_sel[i] & 0x20) >> 3;
					b6 = (sprite_sel[i] & 0x40) >> 5;
					b7 = (sprite_sel[i] & 0x80) >> 7;

					sprite_sel[i] = (byte)(b0 | b1 | b2 | b3 | b4 | b5 | b6 | b7);
				}
			}
		}

		// order sprites according to x coordinate
		// note that for sprites of equal x coordinate, priority goes to first on the list
		public virtual void reorder_and_assemble_sprites()
		{
			sprite_ordered_index = 0;
			
			for (int i = 0; i < 256; i++)
			{
				for (int j = 0; j < SL_sprites_index; j++)
				{
					if (SL_sprites[j * 4 + 1] == i)
					{
						sl_use_index = j;
						process_sprite();
						SL_sprites_ordered[sprite_ordered_index * 4] = SL_sprites[j * 4 + 1];
						SL_sprites_ordered[sprite_ordered_index * 4 + 1] = sprite_sel[0];
						SL_sprites_ordered[sprite_ordered_index * 4 + 2] = sprite_sel[1];
						SL_sprites_ordered[sprite_ordered_index * 4 + 3] = SL_sprites[j * 4 + 3];
						sprite_ordered_index++;
					}
				}
			}

			bool have_pixel = false;
			byte s_pixel = 0;
			byte sprite_attr = 0;

			for (int i = 0; i < 160; i++)
			{
				have_pixel = false;
				for (int j = 0; j < SL_sprites_index; j++)
				{
					if ((i >= (SL_sprites_ordered[j * 4] - 8)) &&
						(i < SL_sprites_ordered[j * 4]) &&
						!have_pixel)
					{
						// we can use the current sprite, so pick out a pixel for it
						int t_index = i - (SL_sprites_ordered[j * 4] - 8);

						t_index = 7 - t_index;

						sprite_data[0] = (byte)((SL_sprites_ordered[j * 4 + 1] >> t_index) & 1);
						sprite_data[1] = (byte)(((SL_sprites_ordered[j * 4 + 2] >> t_index) & 1) << 1);

						s_pixel = (byte)(sprite_data[0] + sprite_data[1]);
						sprite_attr = (byte)SL_sprites_ordered[j * 4 + 3];

						// pixel color of 0 is transparent, so if this is the case we dont have a pixel
						if (s_pixel != 0)
						{
							have_pixel = true;
						}
					}
				}

				if (have_pixel)
				{
					sprite_present_list[i] = 1;
					sprite_pixel_list[i] = s_pixel;
					sprite_attr_list[i] = sprite_attr;
				}
				else
				{
					sprite_present_list[i] = 0;
				}
			}
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync("LCDC", ref LCDC);
			ser.Sync("STAT", ref STAT);
			ser.Sync("scroll_y", ref scroll_y);
			ser.Sync("scroll_x", ref scroll_x);
			ser.Sync("LY", ref LY);
			ser.Sync("LYinc", ref LY_inc);
			ser.Sync("LYC", ref LYC);
			ser.Sync("DMA_addr", ref DMA_addr);
			ser.Sync("BGP", ref BGP);
			ser.Sync("obj_pal_0", ref obj_pal_0);
			ser.Sync("obj_pal_1", ref obj_pal_1);
			ser.Sync("window_y", ref window_y);
			ser.Sync("window_x", ref window_x);
			ser.Sync("DMA_start", ref DMA_start);
			ser.Sync("DMA_clock", ref DMA_clock);
			ser.Sync("DMA_inc", ref DMA_inc);
			ser.Sync("DMA_byte", ref DMA_byte);

			ser.Sync("cycle", ref cycle);
			ser.Sync("LYC_INT", ref LYC_INT);
			ser.Sync("HBL_INT", ref HBL_INT);
			ser.Sync("VBL_INT", ref VBL_INT);
			ser.Sync("OAM_INT", ref OAM_INT);
			ser.Sync("stat_line", ref stat_line);
			ser.Sync("stat_line_old", ref stat_line_old);
			ser.Sync("hbl_countdown", ref hbl_countdown);
			ser.Sync("LCD_was_off", ref LCD_was_off);		
			ser.Sync("OAM_scan_index", ref OAM_scan_index);
			ser.Sync("SL_sprites_index", ref SL_sprites_index);
			ser.Sync("SL_sprites", ref SL_sprites, false);
			ser.Sync("write_sprite", ref write_sprite);
			ser.Sync("no_scan", ref no_scan);

			ser.Sync("DMA_OAM_access", ref DMA_OAM_access);
			ser.Sync("OAM_access_read", ref OAM_access_read);
			ser.Sync("OAM_access_write", ref OAM_access_write);
			ser.Sync("VRAM_access_read", ref VRAM_access_read);
			ser.Sync("VRAM_access_write", ref VRAM_access_write);

			ser.Sync("read_case", ref read_case);
			ser.Sync("internal_cycle", ref internal_cycle);
			ser.Sync("y_tile", ref y_tile);
			ser.Sync("y_scroll_offset", ref y_scroll_offset);
			ser.Sync("x_tile", ref x_tile);
			ser.Sync("x_scroll_offset", ref x_scroll_offset);
			ser.Sync("tile_byte", ref tile_byte);
			ser.Sync("sprite_fetch_cycles", ref sprite_fetch_cycles);
			ser.Sync("fetch_sprite", ref fetch_sprite);
			ser.Sync("fetch_sprite_01", ref fetch_sprite_01);
			ser.Sync("fetch_sprite_4", ref fetch_sprite_4);
			ser.Sync("going_to_fetch", ref going_to_fetch);
			ser.Sync("sprite_fetch_counter", ref sprite_fetch_counter);
			ser.Sync("sprite_attr_list", ref sprite_attr_list, false);
			ser.Sync("sprite_pixel_list", ref sprite_pixel_list, false);
			ser.Sync("sprite_present_list", ref sprite_present_list, false);		
			ser.Sync("temp_fetch", ref temp_fetch);
			ser.Sync("tile_inc", ref tile_inc);
			ser.Sync("pre_render", ref pre_render);
			ser.Sync("tile_data", ref tile_data, false);
			ser.Sync("tile_data_latch", ref tile_data_latch, false);
			ser.Sync("latch_counter", ref latch_counter);
			ser.Sync("latch_new_data", ref latch_new_data);
			ser.Sync("render_counter", ref render_counter);
			ser.Sync("render_offset", ref render_offset);
			ser.Sync("pixel_counter", ref pixel_counter);
			ser.Sync("pixel", ref pixel);
			ser.Sync("sprite_data", ref sprite_data, false);
			ser.Sync("sl_use_index", ref sl_use_index);
			ser.Sync("sprite_sel", ref sprite_sel, false);
			ser.Sync("no_sprites", ref no_sprites);
			ser.Sync("evaled_sprites", ref evaled_sprites);
			ser.Sync("SL_sprites_ordered", ref SL_sprites_ordered, false);
			ser.Sync("sprite_ordered_index", ref sprite_ordered_index);
			ser.Sync("blank_frame", ref blank_frame);

			ser.Sync("window_counter", ref window_counter);
			ser.Sync("window_pre_render", ref window_pre_render);
			ser.Sync("window_started", ref window_started);
			ser.Sync("window_tile_inc", ref window_tile_inc);
			ser.Sync("window_y_tile", ref window_y_tile);
			ser.Sync("window_x_tile", ref window_x_tile);
			ser.Sync("window_y_tile_inc", ref window_y_tile_inc);
			ser.Sync("window_x_latch", ref window_x_latch);
		}
	}
}
