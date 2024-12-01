using BizHawk.Common.NumberExtensions;

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
				case 0xFF4A: ret = window_y_read;			break; // WY
				case 0xFF4B: ret = window_x_read;			break; // WX
			}

			return ret;
		}

		public override void WriteReg(int addr, byte value)
		{
			//Console.WriteLine((addr - 0xFF40) + " " + value + " " + LY + " " + cycle + " " + LCDC.Bit(7));			
			switch (addr)
			{
				case 0xFF40: // LCDC
					if (LCDC.Bit(7) && !value.Bit(7))
					{
						VRAM_access_read = true;
						VRAM_access_write = true;
						OAM_access_read = true;
						OAM_access_write = true;

						clear_screen = true;
					}

					if (!LCDC.Bit(7) && value.Bit(7))
					{
						// don't draw for one frame after turning on
						blank_frame = true;
					}				
					LCDC = value;
					//Console.WriteLine(LY + " " + cycle);
					break; 
				case 0xFF41: // STAT
					// writing to STAT during mode 0 or 1 causes a STAT IRQ
					// this appears to be a glitchy LYC compare
					//Console.WriteLine("stat " + " " + STAT + " " + value + " " + LY + " " + cycle + " " + Core.REG_FF0F);

					if (!value.Bit(6)) { LYC_INT = false; }

					if (LCDC.Bit(7))
					{
						if (((STAT & 3) == 0) || ((STAT & 3) == 1))
						{
							LYC_INT = true;
							//if (Core.REG_FFFF.Bit(1)) { Core.cpu.FlagI = true; }
							//Core.REG_FF0F |= 0x02;
						}
						else
						{
							if (value.Bit(6))
							{
								if (LY == LYC) { LYC_INT = true; }
								else { LYC_INT = false; }
							}
						}
					}
					STAT = (byte)((value & 0xF8) | (STAT & 7) | 0x80);

					if (!STAT.Bit(4)) { VBL_INT = false; }
					if (!STAT.Bit(3)) { HBL_INT = false; }
					break;
				case 0xFF42: // SCY
					scroll_y = value;
					break;
				case 0xFF43: // SCX
					scroll_x = value;
					break;
				case 0xFF44: // LY
					// writing to LY has no effect, confirmed by gambatte test roms
					break;
				case 0xFF45:  // LYC
					LYC = value;
					if (LCDC.Bit(7))
					{
						if (LY != LYC) { STAT &= 0xFB; LYC_INT = false; }
						else { STAT |= 0x4; LYC_INT = true; }

						// special case: the transition from 153 -> 0 acts strange
						// the comparison to 153 expects to be true for longer then the value of LY expects to be 153
						// this appears to be fixed in CGB
						if ((LY_inc == 0) && cycle == 8)
						{
							if (153 != LYC) { STAT &= 0xFB; LYC_INT = false; }
							else { STAT |= 0x4; LYC_INT = true; }
						}
					}				
					break;
				case 0xFF46: // DMA 
					DMA_addr = value;
					DMA_start = true;
					if (!DMA_bus_control) {DMA_OAM_access = true; }
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
					window_y_read = window_y;
					if (!window_started && (!LCDC.Bit(7) || (value > LY))) 
					{
						window_y_latch = window_y;
						window_y_tile = 0;
						window_y_tile_inc = 0;
					}
					break;
				case 0xFF4B: // WX
					window_x = value;
					window_x_read = window_x;
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
							Core._scanlineCallback(LCDC);
						}						
					}

					cycle = 0;
					LY += LY_inc;
					Core.cpu.LY = LY;

					no_scan = false;

					if (LY == 0 && LY_inc == 0)
					{
						LY_inc = 1;
						in_vbl = false;
						Core.in_vblank = false;

						STAT &= 0xFC;

						// special note here, the y coordiate of the window is kept if the window is deactivated
						// meaning it will pick up where it left off if re-enabled later
						// so we don't reset it in the scanline loop
						window_y_tile = 0;
						window_y_latch = window_y;
						window_y_tile_inc = 0;
						window_started = false;
						if (!LCDC.Bit(5)) { window_is_reset = true; }
					}

					// Automatically restore access to VRAM at this time (force end drawing)
					// Who Framed Roger Rabbit seems to run into this.
					VRAM_access_write = true;
					VRAM_access_read = true;

					if (LY == 144)
					{
						in_vbl = true;
						Core.in_vblank = true;
					}
				}

				// exit vblank if LCD went from off to on
				if (LCD_was_off)
				{
					//VBL_INT = false;
					in_vbl = false;
					Core.in_vblank = false;
					LCD_was_off = false;

					// we exit vblank into mode 0 for 4 cycles 
					// but no hblank interrupt, presumably this only happens transitioning from mode 3 to 0
					STAT &= 0xFC;
					glitch_state = true;
					LY_inc = 1;

					// also the LCD doesn't turn on right away
					// also, the LCD does not enter mode 2 on scanline 0 when first turned on
					no_scan = true;
					cycle = 8;
				}

				// Timing note: LYC=143 does not by itself block mode1 stat int. But, the glitchy mode 2 stat check combined with 
				// LYC=143 does block it

				// the VBL stat is continuously asserted
				if (in_vbl)
				{
					// glitchy check of mode 2
					if (LY == 144)
					{
						if (cycle <= 4)
						{
							if (!STAT.Bit(5)) { VBL_INT = false; }
						}

						if ((cycle >= 2) && (cycle < 4))
						{
							// there is an edge case where a VBL INT is triggered if STAT bit 5 is set
							if (STAT.Bit(5)) { VBL_INT = true; }
						}

						if (cycle >= 4)
						{
							if (STAT.Bit(4)) { VBL_INT = true; }
							else { VBL_INT = false; }
						}
					}
					else
					{
						// mode 1 is asserted continuously
						if (STAT.Bit(4)) { VBL_INT = true; }
						else { VBL_INT = false; }
					}

					if ((cycle == 4) && (LY == 144))
					{
						HBL_INT = false;						

						// set STAT mode to 1 (VBlank) and interrupt flag if it is enabled
						STAT &= 0xFC;
						STAT |= 0x01;

						if (Core.REG_FFFF.Bit(0)) { Core.cpu.FlagI = true; }
						Core.REG_FF0F |= 0x01;
					}

					if ((cycle == 6) && (LY == 153))
					{
						LY = 0;
						LY_inc = 0;
						Core.cpu.LY = LY;
					}
				}
				else
				{
					if (no_scan)
					{
						// timings are slightly different if we just turned on the LCD
						// there is no mode 2  (presumably it missed the trigger)
						if (cycle < 85)
						{
							if (cycle == 8)
							{
								// clear the sprite table
								for (int k = 0; k < 10; k++)
								{
									SL_sprites[k * 4] = 0;
									SL_sprites[k * 4 + 1] = 0;
									SL_sprites[k * 4 + 2] = 0;
									SL_sprites[k * 4 + 3] = 0;
								}

								if (LY != LYC)
								{
									LYC_INT = false;
									STAT &= 0xFB;
								}

								if ((LY == LYC) && !STAT.Bit(2))
								{
									// set STAT coincidence FLAG and interrupt flag if it is enabled
									STAT |= 0x04;
									if (STAT.Bit(6)) { LYC_INT = true; }
								}
							}

							if (cycle == 84)
							{
								OAM_access_read = false;

								rendering_complete = false;
							}
						}
						else if (!rendering_complete)
						{
							if (cycle == 86)
							{
								STAT &= 0xFC;
								STAT |= 0x03;
								OAM_INT = false;
								glitch_state = false;

								OAM_access_write = false;
								VRAM_access_read = false;
								VRAM_access_write = false;
								rendering_complete = false;
							}

							// render the screen and handle hblank
							render(cycle - 85);
						}
					}
					else
					{
						if (cycle < 83)
						{
							if (cycle <= 4)
							{
								if ((cycle == 2) && (LY != 0))
								{
									HBL_INT = false;

									if (STAT.Bit(5)) { OAM_INT = true; }
								}

								// the last few cycles of mode 1 still trigger mode 1 int
								if (cycle < 4)
								{
									if (STAT.Bit(4) && ((STAT & 3) == 1)) { VBL_INT = true; }
									else { VBL_INT = false; }
								}
								else
								{
									// apparently, writes can make it to OAM one cycle longer then reads
									OAM_access_write = false;

									// here mode 2 will be set to true and interrupts fired if enabled
									STAT &= 0xFC;
									STAT |= 0x2;

									if (LY == 0)
									{
										VBL_INT = false;
										if (STAT.Bit(5)) { OAM_INT = true; }
									}
								}
							}

							if (cycle == 80)
							{
								OAM_access_read = false;
								OAM_access_write = true;
								VRAM_access_read = false;
								rendering_complete = false;
							}
							else if (cycle < 80)
							{
								// here OAM scanning is performed
								OAM_scan(cycle);
							} 						
						}
						else if (!rendering_complete)
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
							render(cycle - 83);											
						}
					}			
				}

				if (LY_inc == 0)
				{
					if (cycle == 10)
					{
						LYC_INT = false;
						STAT &= 0xFB;
					}
					else if (cycle == 12)
					{
						// Special case of LY = LYC
						if ((LY == LYC) && !STAT.Bit(2))
						{
							// set STAT coincidence FLAG and interrupt flag if it is enabled
							STAT |= 0x04;
							if (STAT.Bit(6)) { LYC_INT = true; }
						}
					}
				}

				// here LY=LYC will be asserted or cleared (but only if LY isnt 0 as that's a special case)
				if ((cycle == 2) && (LY != 0))
				{
					if (LY_inc == 1)
					{
						LYC_INT = false;
						STAT &= 0xFB;
					}
				}
				else if ((cycle == 4) && (LY != 0))
				{
					if ((LY == LYC) && !STAT.Bit(2))
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
				STAT &= 0xFC;

				VBL_INT = LYC_INT = HBL_INT = OAM_INT = false;

				in_vbl = true;
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
			
		}

		public override void render(int render_cycle)
		{
			// we are now in STAT mode 3
			// NOTE: presumably the first necessary sprite is fetched at sprite evaulation
			// i.e. just keeping track of the lowest x-value sprite
			if (render_cycle == 0)
			{
				OAM_scan_index = 0;
				read_case = 0;
				internal_cycle = 0;
				pre_render = true;
				was_pre_render = true;
				pre_render_2 = true;
				tile_inc = 0;
				pixel_counter = -8;
				sl_use_index = 0;
				fetch_sprite = false;
				going_to_fetch = false;
				first_fetch = true;
				consecutive_sprite = -scroll_offset + 8;
				no_sprites = false;
				evaled_sprites = 0;
				window_pre_render = false;
				window_latch = LCDC.Bit(5);

				total_counter = 0;

				window_started = false;

				if (SL_sprites_index == 0) { no_sprites = true; }
				// it is much easier to process sprites if we order them according to the rules of sprite priority first
				if (!no_sprites) { reorder_and_assemble_sprites(); }
			}

			// before anything else, we have to check if windowing is in effect
			if (window_latch && !window_started && (LY >= window_y_latch) && (pixel_counter >= (window_x_latch - 7)) && (window_x_latch < 167))
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

				if (window_x_latch == 0)
				{
					// if the window starts at zero, we still do the first access to the BG
					// but then restart all over again at the window
					if ((scroll_offset % 8) == 0)
					{
						read_case = 4;
					}
					else
					{
						read_case = 9;
					}
				}
				else
				{
					read_case = 4;
				}

				window_pre_render = true;

				window_counter = 0;
				render_counter = 0;

				window_x_tile = (int)Math.Floor((float)(pixel_counter - (window_x_latch - 7)) / 8);
				
				window_tile_inc = 0;
				window_started = true;
				window_is_reset = false;

				// don't evaluate sprites until pre-render for window is over
				pre_render = true;
				was_pre_render = true;
				pre_render_2 = true;
			}

			if (!no_sprites && !pre_render_2 && (pixel_counter < 160) && LCDC.Bit(1))
			{
				// hardware tests show that window takes effect before sprites when actiavted on the same pixel
				// Also, on DMG only, this process only runs if sprites are on in the LCDC (on GBC it always runs)
				for (int i = 0; i < SL_sprites_index; i++)
				{
					if (!evaled_sprites.Bit(i) && pixel_counter - SL_sprites[4 * i + 1] is >= -8 and < 0)
					{
						going_to_fetch = true;
						fetch_sprite = true;
					}
				}
			}

			if (!fetch_sprite)
			{
				switch (read_case)
				{
					case 0: // read a background tile
						if ((internal_cycle % 2) == 1)
						{
							read_case_prev = 0;

							// calculate the row number of the tiles to be fetched
							y_tile = ((scroll_y + LY) >> 3) % 32;
							x_tile = scroll_x >> 3;

							temp_fetch = y_tile * 32 + (x_tile + tile_inc) % 32;

							bus_address = 0x1800 + (LCDC.Bit(3) ? 1 : 0) * 0x400 + temp_fetch;
							tile_byte = Core.VRAM[bus_address];

							read_case = 1;

							if (!pre_render)
							{
								tile_inc++;
							}
						}
						break;

					case 1: // read from tile graphics (0)
						if ((internal_cycle % 2) == 1)
						{
							read_case_prev = 1;

							y_scroll_offset = (scroll_y + LY) % 8;

							if (LCDC.Bit(4))
							{
								bus_address = tile_byte * 16 + y_scroll_offset * 2;
								tile_data[0] = Core.VRAM[bus_address];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7))
								{
									tile_byte -= 256;
								}

								bus_address = 0x1000 + tile_byte * 16 + y_scroll_offset * 2;
								tile_data[0] = Core.VRAM[bus_address];
							}

							read_case = 2;
						}
						break;

					case 2: // read from tile graphics (1)
						if ((internal_cycle % 2) == 1)
						{
							read_case_prev = 2;

							y_scroll_offset = (scroll_y + LY) % 8;

							if (LCDC.Bit(4))
							{
								// if LCDC somehow changed between the two reads, make sure we have a positive number
								if (tile_byte < 0)
								{
									tile_byte += 256;
								}

								bus_address = tile_byte * 16 + y_scroll_offset * 2 + 1;
								tile_data[1] = Core.VRAM[bus_address];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7) && tile_byte > 0)
								{
									tile_byte -= 256;
								}

								bus_address = 0x1000 + tile_byte * 16 + y_scroll_offset * 2 + 1;
								tile_data[1] = Core.VRAM[bus_address];
							}

							if (pre_render)
							{
								// here we set up rendering
								pre_render = false;
								pre_render_2 = false;

								// window X is latched for the scanline, mid-line changes have no effect
								window_x_latch = window_x;

								// x scroll is latched here
								render_offset = scroll_offset = scroll_x % 8;

								// sprite scroll offset could change depending on window usage
								sprite_scroll_offset = scroll_offset;

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

					case 3: // read from tile data
						if ((internal_cycle % 2) == 1)
						{
							read_case_prev = 3;
							// What's on the bus?
							read_case = 0;
							latch_new_data = true;
						}
						break;

					case 4: // read from window data
						if ((window_counter % 2) == 1)
						{
							read_case_prev = 4;

							temp_fetch = window_y_tile * 32 + (window_x_tile + window_tile_inc) % 32;

							bus_address = 0x1800 + (LCDC.Bit(6) ? 1 : 0) * 0x400 + temp_fetch;
							tile_byte = Core.VRAM[bus_address];

							window_tile_inc++;

							read_case = 5;
						}
						window_counter++;
						break;

					case 5: // read from tile graphics (for the window)
						if ((window_counter % 2) == 1)
						{
							read_case_prev = 5;

							y_scroll_offset = (window_y_tile_inc) % 8;

							if (LCDC.Bit(4))
							{
								bus_address = tile_byte * 16 + y_scroll_offset * 2;
								tile_data[0] = Core.VRAM[bus_address];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7))
								{
									tile_byte -= 256;
								}

								bus_address = 0x1000 + tile_byte * 16 + y_scroll_offset * 2;
								tile_data[0] = Core.VRAM[bus_address];
							}

							read_case = 6;
						}
						window_counter++;
						break;

					case 6: // read from tile graphics (for the window)
						if ((window_counter % 2) == 1)
						{
							read_case_prev = 6;

							y_scroll_offset = (window_y_tile_inc) % 8;
							if (LCDC.Bit(4))
							{
								// if LCDC somehow changed between the two reads, make sure we have a positive number
								if (tile_byte < 0)
								{
									tile_byte += 256;
								}

								bus_address = tile_byte * 16 + y_scroll_offset * 2 + 1;
								tile_data[1] = Core.VRAM[bus_address];
							}
							else
							{
								// same as before except now tile byte represents a signed byte
								if (tile_byte.Bit(7) && tile_byte > 0)
								{
									tile_byte -= 256;
								}

								bus_address = 0x1000 + tile_byte * 16 + y_scroll_offset * 2 + 1;
								tile_data[1] = Core.VRAM[bus_address];
							}

							if (window_pre_render)
							{
								// here we set up rendering
								// unlike for the normal background case, there is no pre-render period for the window
								// so start shifting in data to the screen right away
								pre_render = false;
								pre_render_2 = false;
								first_fetch = true;

								if (window_x_latch <= 7)
								{
									if (scroll_offset == 0)
									{
										read_case = 4;
									}
									else
									{
										read_case = 8 + scroll_offset;
									}
									render_counter = 8 - scroll_offset;

									render_offset = 7 - window_x_latch;

									sprite_scroll_offset = (8 - (window_x_latch + 8 - 7) % 8) % 8;
								}
								else
								{
									render_offset = 0;
									read_case = 4;
									render_counter = 8;

									sprite_scroll_offset = (8 - (window_x_latch - 7) % 8) % 8;
								}

								latch_counter = 0;
								latch_new_data = true;
								window_pre_render = false;
							}
							else
							{
								read_case = 7;
							}
						}
						window_counter++;
						break;

					case 7: // read from tile data (window)
						if ((window_counter % 2) == 1)
						{
							read_case_prev = 7;
							// What's on the bus?
							read_case = 4;
							latch_new_data = true;
						}
						window_counter++;
						break;

					case 8: // done reading, we are now in phase 0
						pre_render = true;
						was_pre_render = true;

						VRAM_access_read = true;
						VRAM_access_write = true;
						OAM_access_read = true;
						OAM_access_write = true;

						read_case = 18;
						break;

					case 9:
						// this is a degenerate case for starting the window at 0
						// kevtris' timing doc indicates an additional normal BG access
						// but this information is thrown away, so it's faster to do this then constantly check
						// for it in read case 0
						read_case = 4;
						break;
					case 10:
					case 11:
					case 12:
					case 13:
					case 14:
					case 15:
					case 16:
					case 17:
						read_case--;
						break;
					case 18:
						rendering_complete = true;
						break;
				}

				if (!was_pre_render)
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
							pixel = BGP & 3;
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
						Core.vid_buffer[LY * 160 + pixel_counter] = color_palette[pixel];
						pixel_counter++;

						if (pixel_counter == 160)
						{
							read_case = 8;
							// hbl_countdown = 1;

							STAT &= 0xFC;
							STAT |= 0x00;

							if (STAT.Bit(3)) { HBL_INT = true; }

							// TODO: If Window is turned on midscanline what happens? When is this check done exactly?
							if ((window_started && window_latch) || (window_is_reset && !window_latch && (LY > window_y_latch)))
							{
								window_y_tile_inc++;
								if (window_y_tile_inc == 8)
								{
									window_y_tile_inc = 0;
									window_y_tile++;
									window_y_tile %= 32;
								}
							}
						}
					}
					else if (pixel_counter < 0)
					{
						pixel_counter++;
					}
					render_counter++;
				}

				internal_cycle++;

				if (latch_new_data)
				{
					latch_new_data = false;
					tile_data_latch[0] = tile_data[0];
					tile_data_latch[1] = tile_data[1];
				}

				was_pre_render = pre_render;
			}

			// every in range sprite takes 6 cycles to process
			// sprites located at x=0 still take 6 cycles to process even though they don't appear on screen
			// sprites above x=168 do not take any cycles to process however
			if (fetch_sprite)
			{
				if (going_to_fetch)
				{
					going_to_fetch = false;

					last_eval = 0;

					// at this time it is unknown what each cycle does, but we only need to accurately keep track of cycles
					for (int i = 0; i < SL_sprites_index; i++)
					{
						if (!evaled_sprites.Bit(i) && pixel_counter - SL_sprites[4 * i + 1] is >= -8 and < 0)
						{
							sprite_fetch_counter += 6;
							evaled_sprites |= (1 << i);
							last_eval = SL_sprites[i * 4 + 1];
						}
					}

					// x scroll offsets the penalty table
					// there is no penalty if the next sprites to be fetched are within the currentfetch block (8 pixels)
					if (first_fetch || (last_eval >= consecutive_sprite))
					{
						if (((last_eval + sprite_scroll_offset) % 8) == 0) { sprite_fetch_counter += 5; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 1) { sprite_fetch_counter += 4; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 2) { sprite_fetch_counter += 3; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 3) { sprite_fetch_counter += 2; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 4) { sprite_fetch_counter += 1; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 5) { sprite_fetch_counter += 0; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 6) { sprite_fetch_counter += 0; }
						else if (((last_eval + sprite_scroll_offset) % 8) == 7) { sprite_fetch_counter += 0; }

						consecutive_sprite = (int)Math.Floor((double)(last_eval + sprite_scroll_offset) / 8) * 8 + 8 - sprite_scroll_offset;

						// special case exists here for sprites at zero with non-zero x-scroll. Not sure exactly the reason for it.
						if (last_eval == 0)
						{
							if (sprite_scroll_offset <= 5)
							{
								sprite_fetch_counter += sprite_scroll_offset;
							}
							else
							{
								sprite_fetch_counter += 5;
							}
						}
					}

					sprite_fetch_counter -= 1;

					total_counter += sprite_fetch_counter;

					first_fetch = false;
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

		public override void DMA_tick()
		{
			if (DMA_clock >= 4)
			{
				DMA_bus_control = true;
				DMA_OAM_access = false;
				if ((DMA_clock % 4) == 1)
				{
					// the cpu can't access memory during this time, but we still need the ppu to be able to.
					DMA_bus_control = false;
					// Gekkio reports that A14 being high on DMA transfers always represent WRAM accesses
					// So transfers nominally from higher memory areas are actually still from there (i.e. FF -> DF)
					byte DMA_actual = DMA_addr;
					if (DMA_addr > 0xDF) { DMA_actual &= 0xDF; }
					DMA_byte = Core.ReadMemory((ushort)((DMA_actual << 8) + DMA_inc));
					DMA_bus_control = true;
				}
				else if ((DMA_clock % 4) == 3)
				{
					Core.OAM[DMA_inc] = DMA_byte;

					if (DMA_inc < 0x9F) { DMA_inc++; }
					else { DMA_clock = -6; }
				}
			}

			DMA_clock++;

			if (DMA_clock == -1)
			{
				DMA_start = false;
				DMA_bus_control = false;
				DMA_OAM_access = true;
			}
		}

		public override void process_sprite()
		{
			int y;

			if (SL_sprites[sl_use_index * 4 + 3].Bit(6))
			{
				if (LCDC.Bit(2))
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					y = 15 - y;
					sprite_sel[0] = Core.VRAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2];
					sprite_sel[1] = Core.VRAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2 + 1];
				}
				else
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					y = 7 - y;
					sprite_sel[0] = Core.VRAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2];
					sprite_sel[1] = Core.VRAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2 + 1];
				}
			}
			else
			{
				if (LCDC.Bit(2))
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					sprite_sel[0] = Core.VRAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2];
					sprite_sel[1] = Core.VRAM[(SL_sprites[sl_use_index * 4 + 2] & 0xFE) * 16 + y * 2 + 1];
				}
				else
				{
					y = LY - (SL_sprites[sl_use_index * 4] - 16);
					sprite_sel[0] = Core.VRAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2];
					sprite_sel[1] = Core.VRAM[SL_sprites[sl_use_index * 4 + 2] * 16 + y * 2 + 1];
				}
			}

			if (SL_sprites[sl_use_index * 4 + 3].Bit(5))
			{
				for (int i = 0; i < 2; i++)
				{
					sprite_sel[i] = (byte)(((sprite_sel[i] & 0x01) << 7) |
										   ((sprite_sel[i] & 0x02) << 5) |
										   ((sprite_sel[i] & 0x04) << 3) |
										   ((sprite_sel[i] & 0x08) << 1) |
										   ((sprite_sel[i] & 0x10) >> 1) |
										   ((sprite_sel[i] & 0x20) >> 3) |
										   ((sprite_sel[i] & 0x40) >> 5) |
										   ((sprite_sel[i] & 0x80) >> 7));
				}
			}
		}

		// order sprites according to x coordinate
		// note that for sprites of equal x coordinate, priority goes to first on the list
		public override void reorder_and_assemble_sprites()
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

			byte s_pixel = 0;
			byte sprite_attr = 0;

			int low_bound = 0;
			int high_bound = 0;
			int t_index = 0;
			
			for (int i = 0; i < 160; i++)
			{
				sprite_present_list[i] = 0;
			}

			for (int i = (SL_sprites_index - 1); i >= 0; i--)
			{
				if ((SL_sprites_ordered[i * 4] > 0) && ((SL_sprites_ordered[i * 4] - 8) < 160))
				{
					low_bound = (SL_sprites_ordered[i * 4] >= 8) ? 0 : (8 - SL_sprites_ordered[i * 4]);
					high_bound = ((SL_sprites_ordered[i * 4] - 8) <= 152) ? 7 : (159 - (SL_sprites_ordered[i * 4] - 8));

					for (int j = low_bound; j <= high_bound; j++)
					{
						t_index = 7 - j;

						sprite_data[0] = (byte)((SL_sprites_ordered[i * 4 + 1] >> t_index) & 1);
						sprite_data[1] = (byte)(((SL_sprites_ordered[i * 4 + 2] >> t_index) & 1) << 1);

						s_pixel = (byte)(sprite_data[0] + sprite_data[1]);
						sprite_attr = (byte)SL_sprites_ordered[i * 4 + 3];

						// pixel color of 0 is transparent, so if this is the case we don't have a pixel
						if (s_pixel != 0)
						{
							sprite_present_list[SL_sprites_ordered[i * 4] - (8 - j)] = 1;
							sprite_pixel_list[SL_sprites_ordered[i * 4] - (8 - j)] = s_pixel;
							sprite_attr_list[SL_sprites_ordered[i * 4] - (8 - j)] = sprite_attr;
						}
					}
				}
			}
		}

		public override void OAM_scan(int OAM_cycle)
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
						if (LCDC.Bit(2) ? LY - temp is >= -16 and < 0 : LY - temp is >= -16 and < -8)
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

		public override void Reset()
		{
			LCDC = 0;
			STAT = 0x80;
			scroll_y = 0;
			scroll_x = 0;
			LY = 0;
			LYC = 0; // NOTE: might be internal latch to 0xFF on startup, need to check ex. frame0_m2stat_count_1_dmg08_cgb04c_out91
			DMA_addr = 0xFF;
			BGP = 0xFF;
			obj_pal_0 = 0xFF;
			obj_pal_1 = 0xFF;
			window_y = 0;
			window_x = 0;
			window_y_read = 0;
			window_x_read = 0;
			window_y_latch = 0;
			window_x_latch = 0;
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
			window_is_reset = true;
			window_tile_inc = 0;
			window_y_tile = 0;
			window_x_tile = 0;
			window_y_tile_inc = 0;

			glitch_state = false;
			in_vbl = true;
		}
	}
}
