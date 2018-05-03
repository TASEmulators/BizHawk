using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class PPU
	{
		public GBHawk Core { get; set; }

		public uint[] BG_palette = new uint[32];
		public uint[] OBJ_palette = new uint[32];


		public bool HDMA_active;

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
		public bool first_fetch;
		public int sprite_fetch_counter;
		public byte[] sprite_attr_list = new byte[160];
		public byte[] sprite_pixel_list = new byte[160];
		public byte[] sprite_present_list = new byte[160];
		public int temp_fetch;
		public int tile_inc;
		public bool pre_render;
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

		public virtual void process_sprite()
		{

		}

		// normal DMA moves twice as fast in double speed mode on GBC
		// So give it it's own function so we can seperate it from PPU tick
		public virtual void DMA_tick()
		{

		}

		public virtual void OAM_scan(int OAM_cycle)
		{

		}


		public virtual void Reset()
		{

		}

		// order sprites according to x coordinate
		// note that for sprites of equal x coordinate, priority goes to first on the list
		public virtual void reorder_and_assemble_sprites()
		{

		}

		public virtual void SyncState(Serializer ser)
		{

			ser.Sync("BG_palette", ref BG_palette, false);
			ser.Sync("OBJ_palette", ref OBJ_palette, false);
			ser.Sync("HDMA_active", ref HDMA_active);

			ser.Sync("LCDC", ref LCDC);
			ser.Sync("STAT", ref STAT);
			ser.Sync("scroll_y", ref scroll_y);
			ser.Sync("scroll_x", ref scroll_x);
			ser.Sync("LY", ref LY);
			ser.Sync("LY_actual", ref LY_actual);
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
			ser.Sync("first_fetch", ref first_fetch);
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
			ser.Sync("window_is_reset", ref window_is_reset);
			ser.Sync("window_tile_inc", ref window_tile_inc);
			ser.Sync("window_y_tile", ref window_y_tile);
			ser.Sync("window_x_tile", ref window_x_tile);
			ser.Sync("window_y_tile_inc", ref window_y_tile_inc);
			ser.Sync("window_x_latch", ref window_x_latch);
		}
	}
}
