#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class MemoryManager;
	
	class PPU
	{
	public:
		#pragma region PPU

		PPU()
		{

		}

		MemoryManager* mem_ctrl;

		// pointers not stated
		bool* FlagI = nullptr;
		bool* in_vblank = nullptr;
		bool* cpu_halted = nullptr;
		uint8_t* cpu_LY = nullptr;
		uint8_t* REG_FFFF = nullptr;
		uint8_t* REG_FF0F = nullptr;
		uint8_t* _scanlineCallbackLine = nullptr;
		uint8_t* OAM = nullptr;
		uint8_t* VRAM = nullptr;
		uint32_t* _vidbuffer = nullptr;
		uint32_t* color_palette = nullptr;
		

		uint32_t BG_palette[32] = {};
		uint32_t OBJ_palette[32] = {};

		bool HDMA_active;
		bool clear_screen;

		// register variables
		uint8_t LCDC;
		uint8_t STAT;
		uint8_t scroll_y;
		uint8_t scroll_x;
		uint8_t LY;
		uint8_t LY_actual;
		uint8_t LY_inc;
		uint8_t LYC;
		uint8_t DMA_addr;
		uint8_t BGP;
		uint8_t obj_pal_0;
		uint8_t obj_pal_1;
		uint8_t window_y;
		uint8_t window_x;
		bool DMA_start;
		uint32_t DMA_clock;
		uint32_t DMA_inc;
		uint8_t DMA_uint8_t;

		// state variables
		uint32_t cycle;
		bool LYC_INT;
		bool HBL_INT;
		bool VBL_INT;
		bool OAM_INT;
		bool LCD_was_off;
		bool stat_line;
		bool stat_line_old;
		// OAM scan
		bool DMA_OAM_access;
		bool OAM_access_read;
		bool OAM_access_write;
		uint32_t OAM_scan_index;
		uint32_t SL_sprites_index;
		uint32_t SL_sprites[40] = {};
		uint32_t write_sprite;
		bool no_scan;
		// render
		bool VRAM_access_read;
		bool VRAM_access_write;
		uint32_t read_case;
		uint32_t internal_cycle;
		uint32_t y_tile;
		uint32_t y_scroll_offset;
		uint32_t x_tile;
		uint32_t x_scroll_offset;
		uint32_t tile_byte;
		uint32_t sprite_fetch_cycles;
		bool fetch_sprite;
		bool going_to_fetch;
		bool first_fetch;
		uint32_t sprite_fetch_counter;
		uint8_t sprite_attr_list[160] = {};
		uint8_t sprite_pixel_list[160] = {};
		uint8_t sprite_present_list[160] = {};
		uint32_t temp_fetch;
		uint32_t tile_inc;
		bool pre_render;
		bool pre_render_2;
		uint8_t tile_data[3] = {};
		uint8_t tile_data_latch[3] = {};
		uint32_t latch_counter;
		bool latch_new_data;
		uint32_t render_counter;
		uint32_t render_offset;
		uint32_t pixel_counter;
		uint32_t pixel;
		uint8_t sprite_data[2] = {};
		uint8_t sprite_sel[2] = {};
		uint32_t sl_use_index;
		bool no_sprites;
		uint32_t SL_sprites_ordered[40] = {}; // (x_end, data_low, data_high, attr)
		uint32_t evaled_sprites;
		uint32_t sprite_ordered_index;
		bool blank_frame;
		bool window_latch;
		uint32_t consecutive_sprite;
		uint32_t last_eval;

		uint32_t total_counter;
		// windowing state
		uint32_t window_counter;
		bool window_pre_render;
		bool window_started;
		bool window_is_reset;
		uint32_t window_tile_inc;
		uint32_t window_y_tile;
		uint32_t window_x_tile;
		uint32_t window_y_tile_inc;
		uint32_t window_x_latch;
		uint32_t window_y_latch;

		uint32_t hbl_countdown;

		uint8_t ReadMemory(uint32_t);

		virtual uint8_t ReadReg(int addr)
		{
			return 0;
		}

		virtual void WriteReg(int addr, uint8_t value)
		{

		}

		virtual void tick()
		{

		}

		// might be needed, not sure yet
		virtual void latch_delay()
		{

		}

		virtual void render(int render_cycle)
		{

		}

		virtual void process_sprite()
		{

		}

		// normal DMA moves twice as fast in double speed mode on GBC
		// So give it it's own function so we can seperate it from PPU tick
		virtual void DMA_tick()
		{

		}

		virtual void OAM_scan(int OAM_cycle)
		{

		}

		virtual void Reset()
		{

		}

		// order sprites according to x coordinate
		// note that for sprites of equal x coordinate, priority goes to first on the list
		virtual void reorder_and_assemble_sprites()
		{

		}

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			for (int i = 0; i < 32; i++) { saver = int_saver(BG_palette[i], saver); }
			for (int i = 0; i < 32; i++) { saver = int_saver(OBJ_palette[i], saver); }
			for (int i = 0; i < 40; i++) { saver = int_saver(SL_sprites[i], saver); }

			for (int i = 0; i < 160; i++) { saver = byte_saver(sprite_attr_list[i], saver); }
			for (int i = 0; i < 160; i++) { saver = byte_saver(sprite_pixel_list[i], saver); }
			for (int i = 0; i < 160; i++) { saver = byte_saver(sprite_present_list[i], saver); }
			for (int i = 0; i < 3; i++) { saver = byte_saver(tile_data[i], saver); }
			for (int i = 0; i < 3; i++) { saver = byte_saver(tile_data_latch[i], saver); }
			for (int i = 0; i < 2; i++) { saver = byte_saver(sprite_data[i], saver); }
			for (int i = 0; i < 2; i++) { saver = byte_saver(sprite_sel[i], saver); }
			for (int i = 0; i < 40; i++) { saver = int_saver(SL_sprites_ordered[i], saver); }

			saver = bool_saver(HDMA_active, saver);
			saver = bool_saver(clear_screen, saver);

			saver = byte_saver(LCDC, saver);
			saver = byte_saver(STAT, saver);
			saver = byte_saver(scroll_y, saver);
			saver = byte_saver(scroll_x, saver);
			saver = byte_saver(LY, saver);
			saver = byte_saver(LY_actual, saver);
			saver = byte_saver(LY_inc, saver);
			saver = byte_saver(LYC, saver);
			saver = byte_saver(DMA_addr, saver);
			saver = byte_saver(BGP, saver);
			saver = byte_saver(obj_pal_0, saver);
			saver = byte_saver(obj_pal_1, saver);
			saver = byte_saver(window_y, saver);
			saver = byte_saver(window_x, saver);
			saver = bool_saver(DMA_start, saver);
			saver = int_saver(DMA_clock, saver);
			saver = int_saver(DMA_inc, saver);
			saver = byte_saver(DMA_uint8_t, saver);

			saver = int_saver(cycle, saver);
			saver = bool_saver(LYC_INT, saver);
			saver = bool_saver(HBL_INT, saver);
			saver = bool_saver(VBL_INT, saver);
			saver = bool_saver(OAM_INT, saver);
			saver = bool_saver(stat_line, saver);
			saver = bool_saver(stat_line_old, saver);
			saver = bool_saver(LCD_was_off, saver);
			saver = int_saver(OAM_scan_index, saver);
			saver = int_saver(SL_sprites_index, saver);
			saver = int_saver(write_sprite, saver);
			saver = bool_saver(no_scan, saver);

			saver = bool_saver(DMA_OAM_access, saver);
			saver = bool_saver(OAM_access_read, saver);
			saver = bool_saver(OAM_access_write, saver);
			saver = bool_saver(VRAM_access_read, saver);
			saver = bool_saver(VRAM_access_write, saver);

			saver = int_saver(read_case, saver);
			saver = int_saver(internal_cycle, saver);
			saver = int_saver(y_tile, saver);
			saver = int_saver(y_scroll_offset, saver);
			saver = int_saver(x_tile, saver);
			saver = int_saver(x_scroll_offset, saver);
			saver = int_saver(tile_byte, saver);
			saver = int_saver(sprite_fetch_cycles, saver);
			saver = bool_saver(fetch_sprite, saver);
			saver = bool_saver(going_to_fetch, saver);
			saver = bool_saver(first_fetch, saver);
			saver = int_saver(sprite_fetch_counter, saver);

			saver = int_saver(temp_fetch, saver);
			saver = int_saver(tile_inc, saver);
			saver = bool_saver(pre_render, saver);
			saver = bool_saver(pre_render_2, saver);
			saver = int_saver(latch_counter, saver);
			saver = bool_saver(latch_new_data, saver);
			saver = int_saver(render_counter, saver);
			saver = int_saver(render_offset, saver);
			saver = int_saver(pixel_counter, saver);
			saver = int_saver(pixel, saver);
			saver = int_saver(sl_use_index, saver);
			saver = bool_saver(no_sprites, saver);
			saver = int_saver(evaled_sprites, saver);
			saver = int_saver(sprite_ordered_index, saver);
			saver = bool_saver(blank_frame, saver);
			saver = bool_saver(window_latch, saver);
			saver = int_saver(consecutive_sprite, saver);
			saver = int_saver(last_eval, saver);

			saver = int_saver(window_counter, saver);
			saver = bool_saver(window_pre_render, saver);
			saver = bool_saver(window_started, saver);
			saver = bool_saver(window_is_reset, saver);
			saver = int_saver(window_tile_inc, saver);
			saver = int_saver(window_y_tile, saver);
			saver = int_saver(window_x_tile, saver);
			saver = int_saver(window_y_tile_inc, saver);
			saver = int_saver(window_x_latch, saver);
			saver = int_saver(window_y_latch, saver);

			saver = int_saver(hbl_countdown, saver);

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			for (int i = 0; i < 32; i++) { loader = int_loader(&BG_palette[i], loader); }
			for (int i = 0; i < 32; i++) { loader = int_loader(&OBJ_palette[i], loader); }
			for (int i = 0; i < 40; i++) { loader = int_loader(&SL_sprites[i], loader); }

			for (int i = 0; i < 160; i++) { loader = byte_loader(&sprite_attr_list[i], loader); }
			for (int i = 0; i < 160; i++) { loader = byte_loader(&sprite_pixel_list[i], loader); }
			for (int i = 0; i < 160; i++) { loader = byte_loader(&sprite_present_list[i], loader); }
			for (int i = 0; i < 3; i++) { loader = byte_loader(&tile_data[i], loader); }
			for (int i = 0; i < 3; i++) { loader = byte_loader(&tile_data_latch[i], loader); }
			for (int i = 0; i < 2; i++) { loader = byte_loader(&sprite_data[i], loader); }
			for (int i = 0; i < 2; i++) { loader = byte_loader(&sprite_sel[i], loader); }
			for (int i = 0; i < 40; i++) { loader = int_loader(&SL_sprites_ordered[i], loader); }

			loader = bool_loader(&HDMA_active, loader);
			loader = bool_loader(&clear_screen, loader);

			loader = byte_loader(&LCDC, loader);
			loader = byte_loader(&STAT, loader);
			loader = byte_loader(&scroll_y, loader);
			loader = byte_loader(&scroll_x, loader);
			loader = byte_loader(&LY, loader);
			loader = byte_loader(&LY_actual, loader);
			loader = byte_loader(&LY_inc, loader);
			loader = byte_loader(&LYC, loader);
			loader = byte_loader(&DMA_addr, loader);
			loader = byte_loader(&BGP, loader);
			loader = byte_loader(&obj_pal_0, loader);
			loader = byte_loader(&obj_pal_1, loader);
			loader = byte_loader(&window_y, loader);
			loader = byte_loader(&window_x, loader);
			loader = bool_loader(&DMA_start, loader);
			loader = int_loader(&DMA_clock, loader);
			loader = int_loader(&DMA_inc, loader);
			loader = byte_loader(&DMA_uint8_t, loader);

			loader = int_loader(&cycle, loader);
			loader = bool_loader(&LYC_INT, loader);
			loader = bool_loader(&HBL_INT, loader);
			loader = bool_loader(&VBL_INT, loader);
			loader = bool_loader(&OAM_INT, loader);
			loader = bool_loader(&stat_line, loader);
			loader = bool_loader(&stat_line_old, loader);
			loader = bool_loader(&LCD_was_off, loader);
			loader = int_loader(&OAM_scan_index, loader);
			loader = int_loader(&SL_sprites_index, loader);
			loader = int_loader(&write_sprite, loader);
			loader = bool_loader(&no_scan, loader);

			loader = bool_loader(&DMA_OAM_access, loader);
			loader = bool_loader(&OAM_access_read, loader);
			loader = bool_loader(&OAM_access_write, loader);
			loader = bool_loader(&VRAM_access_read, loader);
			loader = bool_loader(&VRAM_access_write, loader);

			loader = int_loader(&read_case, loader);
			loader = int_loader(&internal_cycle, loader);
			loader = int_loader(&y_tile, loader);
			loader = int_loader(&y_scroll_offset, loader);
			loader = int_loader(&x_tile, loader);
			loader = int_loader(&x_scroll_offset, loader);
			loader = int_loader(&tile_byte, loader);
			loader = int_loader(&sprite_fetch_cycles, loader);
			loader = bool_loader(&fetch_sprite, loader);
			loader = bool_loader(&going_to_fetch, loader);
			loader = bool_loader(&first_fetch, loader);
			loader = int_loader(&sprite_fetch_counter, loader);

			loader = int_loader(&temp_fetch, loader);
			loader = int_loader(&tile_inc, loader);
			loader = bool_loader(&pre_render, loader);
			loader = bool_loader(&pre_render_2, loader);
			loader = int_loader(&latch_counter, loader);
			loader = bool_loader(&latch_new_data, loader);
			loader = int_loader(&render_counter, loader);
			loader = int_loader(&render_offset, loader);
			loader = int_loader(&pixel_counter, loader);
			loader = int_loader(&pixel, loader);
			loader = int_loader(&sl_use_index, loader);
			loader = bool_loader(&no_sprites, loader);
			loader = int_loader(&evaled_sprites, loader);
			loader = int_loader(&sprite_ordered_index, loader);
			loader = bool_loader(&blank_frame, loader);
			loader = bool_loader(&window_latch, loader);
			loader = int_loader(&consecutive_sprite, loader);
			loader = int_loader(&last_eval, loader);

			loader = int_loader(&window_counter, loader);
			loader = bool_loader(&window_pre_render, loader);
			loader = bool_loader(&window_started, loader);
			loader = bool_loader(&window_is_reset, loader);
			loader = int_loader(&window_tile_inc, loader);
			loader = int_loader(&window_y_tile, loader);
			loader = int_loader(&window_x_tile, loader);
			loader = int_loader(&window_y_tile_inc, loader);
			loader = int_loader(&window_x_latch, loader);
			loader = int_loader(&window_y_latch, loader);

			loader = int_loader(&hbl_countdown, loader);
			
			return loader;
		}

		uint8_t* bool_saver(bool to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save ? 1 : 0); saver++;

			return saver;
		}

		uint8_t* byte_saver(uint8_t to_save, uint8_t* saver)
		{
			*saver = to_save; saver++;

			return saver;
		}

		uint8_t* int_saver(uint32_t to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save & 0xFF); saver++; *saver = (uint8_t)((to_save >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 16) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* bool_loader(bool* to_load, uint8_t* loader)
		{
			to_load[0] = *to_load == 1; loader++;

			return loader;
		}

		uint8_t* byte_loader(uint8_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++;

			return loader;
		}

		uint8_t* int_loader(uint32_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (*loader << 8); loader++;
			to_load[0] |= (*loader << 16); loader++; to_load[0] |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}
