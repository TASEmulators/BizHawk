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

		public static readonly byte[] Internal_Graphics = { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, // 0				0x00
															0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x3C, // 1				0x01
															0x3C, 0x66, 0x0C, 0x18, 0x30, 0x60, 0x7E, // 2				0x02
															0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C, // 3				0x03
															0xCC, 0xCC, 0xCC, 0xFE, 0x0C, 0x0C, 0x0C, // 4				0x04
															0x7E, 0x60, 0x60, 0x3C, 0x60, 0x66, 0x3C, // 5				0x05
															0x3C, 0x66, 0x60, 0x7C, 0x66, 0x66, 0x3C, // 6				0x06
															0xFE, 0x06, 0x0C, 0x18, 0x30, 0x60, 0xC0, // 7				0x07
															0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, // 8				0x08
															0x3C, 0x66, 0x66, 0x3E, 0x02, 0x66, 0x3C, // 9				0x09
															0x00, 0x18, 0x18, 0x00, 0x18, 0x18, 0x00, // :				0x0A
															0x18, 0x7E, 0x58, 0x7E, 0x1A, 0x7E, 0x18, // $				0x0B
															0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //  				0x0C
															0x3C, 0x66, 0x0C, 0x18, 0x18, 0x00, 0x18, // ?				0x0D
															0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, // L				0x0E
															0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60, // P				0x0F
															0x00, 0x18, 0x18, 0x7E, 0x18, 0x18, 0x00, // +				0x10
															0xC6, 0xC6, 0xC6, 0xD6, 0xFE, 0xEE, 0xC6, // W				0x11
															0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x7E, // E				0x12
															0xFC, 0xC6, 0xC6, 0xFC, 0xD8, 0xCC, 0xC6, // R				0x13
															0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, // T				0x14
															0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x7C, // U				0x15
															0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C, // I				0x16
															0x7C, 0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x7C, // O				0x17
															0x7C, 0xC6, 0xC6, 0xC6, 0xD7, 0xCC, 0x76, // Q				0x18
															0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C, // S				0x19
															0x7C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x7C, // D				0x1A
															0xFE, 0xC0, 0xC0, 0xF8, 0xC0, 0xC0, 0xC0, // F				0x1B
															0x7C, 0xC6, 0xC0, 0xC0, 0xCE, 0xC6, 0x7E, // G				0x1C
															0xC6, 0xC6, 0xC6, 0xFE, 0xC6, 0xC6, 0xC6, // H				0x1D
															0x06, 0x06, 0x06, 0x06, 0x06, 0xC6, 0x7C, // J				0x1E
															0xC6, 0xCC, 0xD8, 0xF0, 0xD8, 0xCC, 0xC6, // K				0x1F
															0x38, 0x6C, 0xC6, 0xC6, 0xF7, 0xC6, 0xC6, // A				0x20
															0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E, // Z				0x21
															0xC6, 0xC6, 0x6C, 0x38, 0x6C, 0xC6, 0xC6, // X				0x22
															0x7C, 0xC6, 0xC0, 0xC0, 0xC0, 0xC6, 0x7C, // C				0x23
															0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x6C, 0x38, // V				0x24
															0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C, // B				0x25
															0xC6, 0xEE, 0xFE, 0xD6, 0xC6, 0xC6, 0xC6, // M				0x26
															0x00, 0x00, 0x00, 0x00, 0x00, 0x38, 0x38, // .				0x27
															0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00, // -				0x28
															0x00, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x00, // x				0x29
															0x00, 0x18, 0x00, 0x7E, 0x00, 0x18, 0x00, // (div)			0x2A
															0x00, 0x00, 0x7E, 0x00, 0x7E, 0x00, 0x00, // =				0x2B
															0x66, 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, // Y				0x2C
															0xC6, 0xE6, 0xF6, 0xFE, 0xDE, 0xCE, 0xC6, // N				0x2D
															0x03, 0x06, 0xC0, 0x18, 0x30, 0x60, 0xC0, // /				0x2E
															0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, // (box)			0x2F
															0xCE, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xCE, // 10				0x30
															0x00, 0x00, 0x3C, 0x7E, 0x7E, 0x7E, 0x3C, // (ball)			0x31
															0x38, 0x38, 0x30, 0x3C, 0x30, 0x30, 0x38, // (person R)		0x32
															0x38, 0x38, 0x30, 0x3C, 0x30, 0x68, 0x4C, // (runner R)		0x33
															0x38, 0x38, 0x18, 0x78, 0x18, 0x2C, 0x64, // (runner L)		0x34
															0x38, 0x38, 0x18, 0x78, 0x18, 0x18, 0x38, // (person L)		0x35
															0x00, 0x18, 0xC0, 0xF7, 0xC0, 0x18, 0x00, // (arrow R)		0x36
															0x18, 0x3C, 0x7E, 0xFF, 0xFF, 0x18, 0x18, // (tree)			0x37
															0x01, 0x03, 0x07, 0x0F, 0x1F, 0x3F, 0x7F, // (ramp R)		0x38
															0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, // (ramp L)		0x39
															0x38, 0x38, 0x12, 0xFE, 0xB8, 0x28, 0x6C, // (person F)		0x3A
															0xC0, 0x60, 0x30, 0x18, 0x0C, 0x06, 0x03, // \				0x3B
															0x00, 0x00, 0x18, 0x10, 0x10, 0xF7, 0x7C, // (boat 1)		0x3C
															0x00, 0x03, 0x63, 0xFF, 0xFF, 0x18, 0x08, // (plane)		0x3D
															0x00, 0x00, 0x00, 0x01, 0x38, 0xFF, 0x7E, // (boat 2)		0x3E
															0x00, 0x00, 0x00, 0x54, 0x54, 0xFF, 0x7E, // (boat 3 unk)	0x3F
															};

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
