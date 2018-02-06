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

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{		
				case 0xFF40: ret = LCDC;					break; // LCDC
				case 0xFF41: ret = STAT;					break; // STAT
				case 0xFF42: ret = scroll_y;				break; // SCY
				case 0xFF43: ret = scroll_x;				break; // SCX
				case 0xFF44: ret = LY;						break; // LY
				case 0xFF45: ret = LYC;						break; // LYC
				case 0xFF46: ret = 0xFF;					break; // DMA (not readable?) /*ret = DMA_addr; */
				case 0xFF47: ret = BGP;						break; // BGP
				case 0xFF48: ret = obj_pal_0;				break; // OBP0
				case 0xFF49: ret = obj_pal_1;				break; // OBP1
				case 0xFF4A: ret = window_y;				break; // WY
				case 0xFF4B: ret = window_x;				break; // WX
			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			switch (addr)
			{
				case 0xFF40: // LCDC
					if (LCDC.Bit(7) && !value.Bit(7))
					{
						VRAM_access_read = true;
						VRAM_access_write = true;
						OAM_access_read = true;
						OAM_access_write = true;
					}

					if (!LCDC.Bit(7) && value.Bit(7))
					{
						// don't draw for one frame after turning on
						blank_frame = true;
					}

					LCDC = value;
					break; 
				case 0xFF41: // STAT
					// writing to STAT during mode 0 or 2 causes a STAT IRQ
					if (LCDC.Bit(7))
					{
						if (((STAT & 3) == 0) || ((STAT & 3) == 1))
						{
							LYC_INT = true;
						}
					}
					STAT = (byte)((value & 0xF8) | (STAT & 7) | 0x80);
					break; 
				case 0xFF42: // SCY
					scroll_y = value;
					break; 
				case 0xFF43: // SCX
					scroll_x = value;
					// calculate the column number of the tile to start with
					x_tile = (int)Math.Floor((float)(scroll_x) / 8);
					break; 
				case 0xFF44: // LY
					LY = 0; /*reset*/
					break;
				case 0xFF45:  // LYC
					LYC = value;
					if (LY != LYC) { STAT &= 0xFB; }
					break;
				case 0xFF46: // DMA 
					DMA_addr = value;
					DMA_start = true;
					DMA_OAM_access = true;
					DMA_clock = 0;
					DMA_inc = 0;
					break; 
				case 0xFF47: // BGP
					BGP = value;
					break; 
				case 0xFF48: // OBP0
					obj_pal_0 = value;
					break; 
				case 0xFF49: // OBP1
					obj_pal_1 = value;
					break;
				case 0xFF4A: // WY
					window_y = value;
					break;
				case 0xFF4B: // WX
					window_x = value;
					break;
			}			
		}

		public void tick()
		{
			// tick DMA, note that DMA is halted when the CPU is halted
			if (DMA_start && !Core.cpu.halted)
			{
				if (DMA_clock >= 4)
				{
					DMA_OAM_access = false;
					if ((DMA_clock % 4) == 1)
					{
						// the cpu can't access memory during this time, but we still need the ppu to be able to.
						DMA_start = false;
						DMA_byte = Core.ReadMemory((ushort)((DMA_addr << 8) + DMA_inc));
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

			// the ppu only does anything if it is turned on via bit 7 of LCDC
			if (LCDC.Bit(7))
			{
				// start the next scanline
				if (cycle == 456)
				{
					// scanline callback
					if ((LY + LY_inc) == Core._scanlineCallbackLine)
					{
						if (Core._scanlineCallback != null)
						{
							Core.GetGPU();
							Core._scanlineCallback(LCDC);
						}						
					}

					cycle = 0;
					LY += LY_inc;
					no_scan = false;

					// here is where LY = LYC gets cleared (but only if LY isnt 0 as that's a special case
					if (LY_inc == 1)
					{
						LYC_INT = false;
						STAT &= 0xFB;
					}

					if (LY == 0 && LY_inc == 0)
					{
						LY_inc = 1;
						Core.in_vblank = false;
						VBL_INT = false;
						if (STAT.Bit(3)) { HBL_INT = true; }
						
						STAT &= 0xFC;

						// special note here, the y coordiate of the window is kept if the window is deactivated
						// meaning it will pick up where it left off if re-enabled later
						// so we don't reset it in the scanline loop
						window_y_tile = 0;
						window_y_tile_inc = 0;
						window_started = false;
					}

					Core.cpu.LY = LY;

					// Automatically restore access to VRAM at this time (force end drawing)
					// Who Framed Roger Rabbit seems to run into this.
					VRAM_access_write = true;
					VRAM_access_read = true;

					if (LY == 144)
					{
						Core.in_vblank = true;
					}
				}

				// exit vblank if LCD went from off to on
				if (LCD_was_off)
				{
					//VBL_INT = false;
					Core.in_vblank = false;
					LCD_was_off = false;

					// we exit vblank into mode 0 for 4 cycles 
					// but no hblank interrupt, presumably this only happens transitioning from mode 3 to 0
					STAT &= 0xFC;

					// also the LCD doesn't turn on right away

					// also, the LCD does not enter mode 2 on scanline 0 when first turned on
					no_scan = true;
					cycle = 8;
				}

				// the VBL stat is continuously asserted
				if ((LY >= 144))
				{
					if (STAT.Bit(4))
					{
						if ((cycle >= 4) && (LY == 144))
						{
							VBL_INT = true;
						}
						else if (LY > 144)
						{
							VBL_INT = true;
						}
					}

					if ((cycle == 4) && (LY == 144)) {

						HBL_INT = false;

						// set STAT mode to 1 (VBlank) and interrupt flag if it is enabled
						STAT &= 0xFC;
						STAT |= 0x01;

						if (Core.REG_FFFF.Bit(0)) { Core.cpu.FlagI = true; }
						Core.REG_FF0F |= 0x01;
					}

					if ((LY >= 144) && (cycle == 4))
					{
						// a special case of OAM mode 2 IRQ assertion, even though PPU Mode still is 1
						if (STAT.Bit(5)) { OAM_INT = true; }
					}

					if ((LY == 153) && (cycle == 8))
					{
						LY = 0;
						LY_inc = 0;
						Core.cpu.LY = LY;
					}
				}

				if (!Core.in_vblank)
				{
					if (no_scan)
					{
						// timings are slightly different if we just turned on the LCD
						// there is no mode 2  (presumably it missed the trigger)
						// mode 3 is very short, probably in some self test mode before turning on?

						if (cycle == 12)
						{
							LYC_INT = false;
							STAT &= 0xFB;

							if (LY == LYC)
							{
								// set STAT coincidence FLAG and interrupt flag if it is enabled
								STAT |= 0x04;
								if (STAT.Bit(6)) { LYC_INT = true; }
							}
						}

						if (cycle == 84)
						{

							STAT &= 0xFC;
							STAT |= 0x03;
							OAM_INT = false;

							OAM_access_read = false;
							OAM_access_write = false;
							VRAM_access_read = false;
							VRAM_access_write = false;
						}

						if (cycle == 256)
						{
							STAT &= 0xFC;
							OAM_INT = false;

							if (STAT.Bit(3)) { HBL_INT = true; }

							OAM_access_read = true;
							OAM_access_write = true;
							VRAM_access_read = true;
							VRAM_access_write = true;
						}
					}
					else
					{
						if (cycle < 80)
						{
							if (cycle == 4)
							{
								// apparently, writes can make it to OAM one cycle longer then reads
								OAM_access_write = false;

								// here mode 2 will be set to true and interrupts fired if enabled
								STAT &= 0xFC;
								STAT |= 0x2;
								if (STAT.Bit(5)) { OAM_INT = true; }

								HBL_INT = false;
							}

							// here OAM scanning is performed
							OAM_scan(cycle);
						}
						else if (cycle >= 80 && LY < 144)
						{

							if (cycle == 84)
							{
								STAT &= 0xFC;
								STAT |= 0x03;
								OAM_INT = false;
								OAM_access_write = false;
								VRAM_access_write = false;
							}

							// render the screen and handle hblank
							render(cycle - 80);
						}
					}					
				}

				if ((LY_inc == 0))
				{
					if (cycle == 12)
					{
						LYC_INT = false;
						STAT &= 0xFB;

						// Special case of LY = LYC
						if (LY == LYC)
						{
							// set STAT coincidence FLAG and interrupt flag if it is enabled
							STAT |= 0x04;
							if (STAT.Bit(6)) { LYC_INT = true; }
						}

						// also a special case of OAM mode 2 IRQ assertion, even though PPU Mode still is 1
						if (STAT.Bit(5)) { OAM_INT = true; }
					}

					if (cycle == 92) { OAM_INT = false; }
				}

				// here LY=LYC will be asserted
				if ((cycle == 4) && (LY != 0))
				{
					if (LY == LYC)
					{
						// set STAT coincidence FLAG and interrupt flag if it is enabled
						STAT |= 0x04;
						if (STAT.Bit(6)) { LYC_INT = true; }
					}
				}

				cycle++;
			}
			else
			{
				STAT &= 0xF8;

				VBL_INT = LYC_INT = HBL_INT = OAM_INT = false;

				Core.in_vblank = true;

				LCD_was_off = true;

				LY = 0;
				Core.cpu.LY = LY;

				cycle = 0;
			}

			// assert the STAT IRQ line if the line went from zero to 1
			stat_line = VBL_INT | LYC_INT | HBL_INT | OAM_INT;

			if (stat_line && !stat_line_old)
			{
				if (Core.REG_FFFF.Bit(1)) { Core.cpu.FlagI = true; }
				Core.REG_FF0F |= 0x02;
			}

			stat_line_old = stat_line;

			// process latch delays
			//latch_delay();
		}

		// might be needed, not sure yet
		public void latch_delay()
		{
			//BGP_l = BGP;
		}

		public void OAM_scan(int OAM_cycle)
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

		public void render(int render_cycle)
		{
			// we are now in STAT mode 3
			// NOTE: presumably the first necessary sprite is fetched at sprite evaulation
			// i.e. just keeping track of the lowest x-value sprite
			if (render_cycle == 0)
			{
				OAM_access_read = false;
				OAM_access_write = true;
				VRAM_access_read = false;

				// window X is latched for the scanline, mid-line changes have no effect
				window_x_latch = window_x;

				OAM_scan_index = 0;
				read_case = 0;
				internal_cycle = 0;
				pre_render = true;
				tile_inc = 0;
				pixel_counter = -8;
				sl_use_index = 0;
				fetch_sprite = false;
				fetch_sprite_01 = false;
				fetch_sprite_4 = false;
				going_to_fetch = false;
				no_sprites = false;
				evaled_sprites = 0;

				window_pre_render = false;
				if (window_started && LCDC.Bit(5))
				{
					window_y_tile_inc++;
					if (window_y_tile_inc==8)
					{
						window_y_tile_inc = 0;
						window_y_tile++;
						window_y_tile %= 32;
					}
				}
				window_started = false;

				if (SL_sprites_index == 0) { no_sprites = true; }
				// it is much easier to process sprites if we order them according to the rules of sprite priority first
				if (!no_sprites) { reorder_and_assemble_sprites(); }

			}

			// before anything else, we have to check if windowing is in effect
			if (LCDC.Bit(5) && !window_started && (LY >= window_y) && (pixel_counter >= (window_x_latch - 7)) && (window_x_latch < 167))
			{
				/*
				Console.Write(LY);
				Console.Write(" ");
				Console.Write(cycle);
				Console.Write(" ");
				Console.Write(window_y_tile_inc);
				Console.Write(" ");
				Console.Write(window_x_latch);
				Console.Write(" ");
				Console.WriteLine(pixel_counter);
				*/
				if (pixel_counter == 0 && window_x_latch <= 7)
				{
					// if the window starts at zero, we still do the first access to the BG
					// but then restart all over again at the window
					window_pre_render = true;
				}
				else
				{
					// otherwise, just restart the whole process as if starting BG again
					window_pre_render = true;
					read_case = 4;
				}
				window_counter = 0;

				window_x_tile = (int)Math.Floor((float)(pixel_counter - (window_x_latch - 7)) / 8);
				
				window_tile_inc = 0;
				window_started = true;
			}
			
			if (!pre_render && !fetch_sprite && !window_pre_render)
			{
				// start shifting data into the LCD
				if (render_counter >= (render_offset + 8))
				{
					pixel = tile_data_latch[0].Bit(7 - (render_counter % 8)) ? 1 : 0;
					pixel |= tile_data_latch[1].Bit(7 - (render_counter % 8)) ? 2 : 0;

					int ref_pixel = pixel;
					if (LCDC.Bit(0))
					{
						pixel = (BGP >> (pixel * 2)) & 3;
					}
					else
					{
						pixel = 0;
					}
						
					// now we have the BG pixel, we next need the sprite pixel
					if (!no_sprites)
					{
						bool have_sprite = false;
						int s_pixel = 0;
						int sprite_attr = 0;

						if (sprite_present_list[pixel_counter] == 1)
						{
							have_sprite = true;
							s_pixel = sprite_pixel_list[pixel_counter];
							sprite_attr = sprite_attr_list[pixel_counter];
						}

						if (have_sprite)
						{
							bool use_sprite = false;
							if (LCDC.Bit(1))
							{
								if (!sprite_attr.Bit(7))
								{
									use_sprite = true;
								}
								else if (ref_pixel == 0)
								{
									use_sprite = true;
								}

								if (!LCDC.Bit(0))
								{
									use_sprite = true;
								}
							}

							if (use_sprite)
							{
								if (sprite_attr.Bit(4))
								{
									pixel = (obj_pal_1 >> (s_pixel * 2)) & 3;
								}
								else
								{
									pixel = (obj_pal_0 >> (s_pixel * 2)) & 3;
								}							
							}
						}
					}

					// based on sprite priority and pixel values, pick a final pixel color
					Core._vidbuffer[LY * 160 + pixel_counter] = (int)Core.color_palette[pixel];
					pixel_counter++;

					if (pixel_counter == 160)
					{
						read_case = 8;
						hbl_countdown = 7;
					}
				}
				else if ((render_counter >= render_offset) && (pixel_counter < 0))
				{
					pixel_counter++;
				}
				render_counter++;
			}

			if (!fetch_sprite)
			{
				if (!pre_render)
				{
					// before we go on to read case 3, we need to know if we stall there or not
					// Gekkio's tests show that if sprites are at position 0 or 1 (mod 8) 
					// then it takes an extra cycle (1 or 2 more t-states) to process them

					if (!no_sprites && (pixel_counter < 160))
					{
						for (int i = 0; i < SL_sprites_index; i++)
						{
							if ((pixel_counter >= (SL_sprites[i * 4 + 1] - 8)) &&
								(pixel_counter < (SL_sprites[i * 4 + 1])) && 
								!evaled_sprites.Bit(i))
							{
								going_to_fetch = true;
								fetch_sprite = true;

								if ((SL_sprites[i * 4 + 1] % 8) < 2)
								{
									fetch_sprite_01 = true;
								}
								if ((SL_sprites[i * 4 + 1] % 8) > 3)
								{
									fetch_sprite_4 = true;
								}
							}
						}
					}
				}

				switch (read_case)
				{
					case 0: // read a background tile
						if ((internal_cycle % 2) == 0)
						{
							// calculate the row number of the tiles to be fetched
							y_tile = ((int)Math.Floor((float)(scroll_y + LY) / 8)) % 32;

							temp_fetch = y_tile * 32 + (x_tile + tile_inc) % 32;
							tile_byte = LCDC.Bit(3) ? Core.BG_map_2[temp_fetch] : Core.BG_map_1[temp_fetch];
						}
						else
						{
							read_case = 1;
							if (!pre_render)
							{
								tile_inc++;
								if (window_pre_render)
								{
									read_case = 4;
								}
							}						
						}
						break;

					case 1: // read from tile graphics (0)
						if ((internal_cycle % 2) == 0)
						{
							y_scroll_offset = (scroll_y + LY) % 8;

							if (LCDC.Bit(4))
							{
								tile_data[0] = Core.CHR_RAM[tile_byte * 16 + y_scroll_offset * 2];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7))
								{
									tile_byte -= 256;
								}
								tile_data[0] = Core.CHR_RAM[0x1000 + tile_byte * 16 + y_scroll_offset * 2];
							}

						}
						else
						{
							read_case = 2;
						}
						break;

					case 2: // read from tile graphics (1)
						if ((internal_cycle % 2) == 0)
						{
							y_scroll_offset = (scroll_y + LY) % 8;

							if (LCDC.Bit(4))
							{
								// if LCDC somehow changed between the two reads, make sure we have a positive number
								if (tile_byte < 0)
								{
									tile_byte += 256;
								}

								tile_data[1] = Core.CHR_RAM[tile_byte * 16 + y_scroll_offset * 2 + 1];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7) && tile_byte > 0)
								{
									tile_byte -= 256;
								}

								tile_data[1] = Core.CHR_RAM[0x1000 + tile_byte * 16 + y_scroll_offset * 2 + 1];
							}

						}
						else
						{
							if (pre_render)
							{
								// here we set up rendering
								pre_render = false;
								render_offset = scroll_x % 8;
								render_counter = 0;
								latch_counter = 0;
								read_case = 0;
							}
							else
							{
								read_case = 3;
							}
						}
						break;

					case 3: // read from sprite data
						if ((internal_cycle % 2) == 0)
						{
							// nothing to do if not fetching
						}
						else
						{
							read_case = 0;
							latch_new_data = true;
						}
						break;

					case 4: // read from window data
						if ((window_counter % 2) == 0)
						{
							temp_fetch = window_y_tile * 32 + (window_x_tile + window_tile_inc) % 32;
							tile_byte = LCDC.Bit(6) ? Core.BG_map_2[temp_fetch] : Core.BG_map_1[temp_fetch];
						}
						else
						{
							if (!window_pre_render)
							{
								window_tile_inc++;
							}
							read_case = 5;
						}
						window_counter++;
						break;

					case 5: // read from tile graphics (for the window)
						if ((window_counter % 2) == 0)
						{
							y_scroll_offset = (window_y_tile_inc) % 8;

							if (LCDC.Bit(4))
							{
								
								tile_data[0] = Core.CHR_RAM[tile_byte * 16 + y_scroll_offset * 2];
								
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7))
								{
									tile_byte -= 256;
								}
								
								tile_data[0] = Core.CHR_RAM[0x1000 + tile_byte * 16 + y_scroll_offset * 2];
							}
						}
						else
						{
							read_case = 6;
						}
						window_counter++;
						break;

					case 6: // read from tile graphics (for the window)
						if ((window_counter % 2) == 0)
						{
							y_scroll_offset = (window_y_tile_inc) % 8;
							if (LCDC.Bit(4))
							{
								// if LCDC somehow changed between the two reads, make sure we have a positive number
								if (tile_byte < 0)
								{
									tile_byte += 256;
								}

								tile_data[1] = Core.CHR_RAM[tile_byte * 16 + y_scroll_offset * 2 + 1];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7) && tile_byte > 0)
								{
									tile_byte -= 256;
								}

								tile_data[1] = Core.CHR_RAM[0x1000 + tile_byte * 16 + y_scroll_offset * 2 + 1];
							}

						}
						else
						{
							if (window_pre_render)
							{
								// here we set up rendering
								window_pre_render = false;
								render_offset = 0;
								render_counter = 0;
								latch_counter = 0;
								read_case = 4;
							}
							else
							{
								read_case = 7;

							}
						}
						window_counter++;
						break;

					case 7: // read from sprite data
						if ((window_counter % 2) == 0)
						{
							// nothing to do if not fetching
						}
						else
						{
							read_case = 4;
							latch_new_data = true;
						}
						window_counter++; 
						break;

					case 8: // done reading, we are now in phase 0

						pre_render = true;

						// the other interrupts appear to be delayed by 1 CPU cycle, so do the same here
						if (hbl_countdown > 0)
						{
							hbl_countdown--;
							if (hbl_countdown == 0)
							{
								STAT &= 0xFC;
								STAT |= 0x00;

								if (STAT.Bit(3)) { HBL_INT = true; }

								OAM_access_read = true;
								OAM_access_write = true;
								VRAM_access_read = true;
								VRAM_access_write = true;
							}
						}
							
						break;
				}
				internal_cycle++;

				if (latch_new_data)
				{
					latch_new_data = false;
					tile_data_latch[0] = tile_data[0];
					tile_data_latch[1] = tile_data[1];
				}
			}

			// every in range sprite takes 6 cycles to process
			// sprites located at x=0 still take 6 cycles to process even though they don't appear on screen
			// sprites above x=168 do not take any cycles to process however
			if (fetch_sprite)
			{
				if (going_to_fetch)
				{
					going_to_fetch = false;
					sprite_fetch_counter = 0;

					if (fetch_sprite_01)
					{
						sprite_fetch_counter += 2;
						fetch_sprite_01 = false;
					}

					if (fetch_sprite_4)
					{
						sprite_fetch_counter -= 2;
						fetch_sprite_4 = false;
					}

					int last_eval = 0;

					// at this time it is unknown what each cycle does, but we only need to accurately keep track of cycles
					for (int i = 0; i < SL_sprites_index; i++)
					{
						if ((pixel_counter >= (SL_sprites[i * 4 + 1] - 8)) &&
								(pixel_counter < (SL_sprites[i * 4 + 1])) &&
								!evaled_sprites.Bit(i))
						{
							sprite_fetch_counter += 6;
							evaled_sprites |= (1 << i);
							last_eval = SL_sprites[i * 4 + 1];
						}
					}

					// if we didn't evaluate all the sprites immediately, 2 more cycles are added to restart it
					if (evaled_sprites != (Math.Pow(2,SL_sprites_index) - 1))
					{
						if ((last_eval % 8) == 0) { sprite_fetch_counter += 3; }
						else if ((last_eval % 8) == 1) { sprite_fetch_counter += 2; }
						else if ((last_eval % 8) == 2) { sprite_fetch_counter += 3; }
						else if ((last_eval % 8) == 3) { sprite_fetch_counter += 2; }
						else if ((last_eval % 8) == 4) { sprite_fetch_counter += 3; }
						else { sprite_fetch_counter += 2; }
					}
				}
				else
				{
					sprite_fetch_counter--;
					if (sprite_fetch_counter == 0)
					{
						fetch_sprite = false;
					}
				}				
			}
		}

		public void Reset()
		{
			LCDC = 0;
			STAT = 0x80;
			scroll_y = 0;
			scroll_x = 0;
			LY = 0;
			LYC = 0;
			DMA_addr = 0;
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

		public void process_sprite()
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
		public void reorder_and_assemble_sprites()
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

		public void SyncState(Serializer ser)
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
