using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	public class PPU
	{
		public O2Hawk Core { get; set; }

		public uint[] BG_palette = new uint[32];
		public uint[] OBJ_palette = new uint[32];

		public bool HDMA_active;
		public bool clear_screen;

		// register variables
		public byte LCDC;
		public byte STAT;
		public byte scroll_y;
		public byte scroll_x;
		public byte LY;
		public byte LY_actual;
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
		public bool going_to_fetch;
		public bool first_fetch;
		public int sprite_fetch_counter;
		public byte[] sprite_attr_list = new byte[160];
		public byte[] sprite_pixel_list = new byte[160];
		public byte[] sprite_present_list = new byte[160];
		public int temp_fetch;
		public int tile_inc;
		public bool pre_render;
		public bool pre_render_2;
		public byte[] tile_data = new byte[3];
		public byte[] tile_data_latch = new byte[3];
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
		public bool window_latch;
		public int consecutive_sprite;
		public int last_eval;

		public int total_counter;
		// windowing state
		public int window_counter;
		public bool window_pre_render;
		public bool window_started;
		public bool window_is_reset;
		public int window_tile_inc;
		public int window_y_tile;
		public int window_x_tile;
		public int window_y_tile_inc;
		public int window_x_latch;
		public int window_y_latch;

		public int hbl_countdown;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{

			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{

		}

		public void tick()
		{

		}

		// might be needed, not sure yet
		public void latch_delay()
		{

		}

		public void render(int render_cycle)
		{

		}

		public void process_sprite()
		{

		}

		// normal DMA moves twice as fast in double speed mode on GBC
		// So give it it's own function so we can seperate it from PPU tick
		public void DMA_tick()
		{

		}

		public void OAM_scan(int OAM_cycle)
		{

		}

		public void Reset()
		{

		}

		// order sprites according to x coordinate
		// note that for sprites of equal x coordinate, priority goes to first on the list
		public void reorder_and_assemble_sprites()
		{

		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(BG_palette), ref BG_palette, false);
			ser.Sync(nameof(OBJ_palette), ref OBJ_palette, false);
			ser.Sync(nameof(HDMA_active), ref HDMA_active);
			ser.Sync(nameof(clear_screen), ref clear_screen);

			ser.Sync(nameof(LCDC), ref LCDC);
			ser.Sync(nameof(STAT), ref STAT);
			ser.Sync(nameof(scroll_y), ref scroll_y);
			ser.Sync(nameof(scroll_x), ref scroll_x);
			ser.Sync(nameof(LY), ref LY);
			ser.Sync(nameof(LY_actual), ref LY_actual);
			ser.Sync(nameof(LY_inc), ref LY_inc);
			ser.Sync(nameof(LYC), ref LYC);
			ser.Sync(nameof(DMA_addr), ref DMA_addr);
			ser.Sync(nameof(BGP), ref BGP);
			ser.Sync(nameof(obj_pal_0), ref obj_pal_0);
			ser.Sync(nameof(obj_pal_1), ref obj_pal_1);
			ser.Sync(nameof(window_y), ref window_y);
			ser.Sync(nameof(window_x), ref window_x);
			ser.Sync(nameof(DMA_start), ref DMA_start);
			ser.Sync(nameof(DMA_clock), ref DMA_clock);
			ser.Sync(nameof(DMA_inc), ref DMA_inc);
			ser.Sync(nameof(DMA_byte), ref DMA_byte);

			ser.Sync(nameof(cycle), ref cycle);
			ser.Sync(nameof(LYC_INT), ref LYC_INT);
			ser.Sync(nameof(HBL_INT), ref HBL_INT);
			ser.Sync(nameof(VBL_INT), ref VBL_INT);
			ser.Sync(nameof(OAM_INT), ref OAM_INT);
			ser.Sync(nameof(stat_line), ref stat_line);
			ser.Sync(nameof(stat_line_old), ref stat_line_old);
			ser.Sync(nameof(LCD_was_off), ref LCD_was_off);		
			ser.Sync(nameof(OAM_scan_index), ref OAM_scan_index);
			ser.Sync(nameof(SL_sprites_index), ref SL_sprites_index);
			ser.Sync(nameof(SL_sprites), ref SL_sprites, false);
			ser.Sync(nameof(write_sprite), ref write_sprite);
			ser.Sync(nameof(no_scan), ref no_scan);

			ser.Sync(nameof(DMA_OAM_access), ref DMA_OAM_access);
			ser.Sync(nameof(OAM_access_read), ref OAM_access_read);
			ser.Sync(nameof(OAM_access_write), ref OAM_access_write);
			ser.Sync(nameof(VRAM_access_read), ref VRAM_access_read);
			ser.Sync(nameof(VRAM_access_write), ref VRAM_access_write);

			ser.Sync(nameof(read_case), ref read_case);
			ser.Sync(nameof(internal_cycle), ref internal_cycle);
			ser.Sync(nameof(y_tile), ref y_tile);
			ser.Sync(nameof(y_scroll_offset), ref y_scroll_offset);
			ser.Sync(nameof(x_tile), ref x_tile);
			ser.Sync(nameof(x_scroll_offset), ref x_scroll_offset);
			ser.Sync(nameof(tile_byte), ref tile_byte);
			ser.Sync(nameof(sprite_fetch_cycles), ref sprite_fetch_cycles);
			ser.Sync(nameof(fetch_sprite), ref fetch_sprite);
			ser.Sync(nameof(going_to_fetch), ref going_to_fetch);
			ser.Sync(nameof(first_fetch), ref first_fetch);
			ser.Sync(nameof(sprite_fetch_counter), ref sprite_fetch_counter);
			ser.Sync(nameof(sprite_attr_list), ref sprite_attr_list, false);
			ser.Sync(nameof(sprite_pixel_list), ref sprite_pixel_list, false);
			ser.Sync(nameof(sprite_present_list), ref sprite_present_list, false);		
			ser.Sync(nameof(temp_fetch), ref temp_fetch);
			ser.Sync(nameof(tile_inc), ref tile_inc);
			ser.Sync(nameof(pre_render), ref pre_render);
			ser.Sync(nameof(pre_render_2), ref pre_render_2);
			ser.Sync(nameof(tile_data), ref tile_data, false);
			ser.Sync(nameof(tile_data_latch), ref tile_data_latch, false);
			ser.Sync(nameof(latch_counter), ref latch_counter);
			ser.Sync(nameof(latch_new_data), ref latch_new_data);
			ser.Sync(nameof(render_counter), ref render_counter);
			ser.Sync(nameof(render_offset), ref render_offset);
			ser.Sync(nameof(pixel_counter), ref pixel_counter);
			ser.Sync(nameof(pixel), ref pixel);
			ser.Sync(nameof(sprite_data), ref sprite_data, false);
			ser.Sync(nameof(sl_use_index), ref sl_use_index);
			ser.Sync(nameof(sprite_sel), ref sprite_sel, false);
			ser.Sync(nameof(no_sprites), ref no_sprites);
			ser.Sync(nameof(evaled_sprites), ref evaled_sprites);
			ser.Sync(nameof(SL_sprites_ordered), ref SL_sprites_ordered, false);
			ser.Sync(nameof(sprite_ordered_index), ref sprite_ordered_index);
			ser.Sync(nameof(blank_frame), ref blank_frame);
			ser.Sync(nameof(window_latch), ref window_latch);
			ser.Sync(nameof(consecutive_sprite), ref consecutive_sprite);
			ser.Sync(nameof(last_eval), ref last_eval);

			ser.Sync(nameof(window_counter), ref window_counter);
			ser.Sync(nameof(window_pre_render), ref window_pre_render);
			ser.Sync(nameof(window_started), ref window_started);
			ser.Sync(nameof(window_is_reset), ref window_is_reset);
			ser.Sync(nameof(window_tile_inc), ref window_tile_inc);
			ser.Sync(nameof(window_y_tile), ref window_y_tile);
			ser.Sync(nameof(window_x_tile), ref window_x_tile);
			ser.Sync(nameof(window_y_tile_inc), ref window_y_tile_inc);
			ser.Sync(nameof(window_x_latch), ref window_x_latch);
			ser.Sync(nameof(window_y_latch), ref window_y_latch);

			ser.Sync(nameof(hbl_countdown), ref hbl_countdown);
		}
	}
}
