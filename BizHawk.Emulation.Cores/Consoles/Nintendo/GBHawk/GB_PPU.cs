using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class GB_PPU : PPU
	{
		public override byte ReadReg(int addr)
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
				case 0xFF46: ret = DMA_addr;				break; // DMA 
				case 0xFF47: ret = BGP;						break; // BGP
				case 0xFF48: ret = obj_pal_0;				break; // OBP0
				case 0xFF49: ret = obj_pal_1;				break; // OBP1
				case 0xFF4A: ret = window_y;				break; // WY
				case 0xFF4B: ret = window_x;				break; // WX
			}

			return ret;
		}

		public override void WriteReg(int addr, byte value)
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

		public override void tick()
		{
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
		public override void latch_delay()
		{
			//BGP_l = BGP;
		}

		public override void render(int render_cycle)
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
	}
}
