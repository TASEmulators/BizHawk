//TODO - correctly emulate PPU OFF state

using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	sealed partial class PPU
	{
		const int kFetchTime = 2;
		const int kLineTime = 341;

		struct BGDataRecord
		{
			public byte nt, at;
			public byte pt_0, pt_1;
		};

		public short[] xbuf = new short[256 * 240];

		// values here are used in sprite evaluation
		public int spr_true_count;
		public bool sprite_eval_write;
		public byte read_value;
		public int soam_index;
		public int soam_index_prev;
		public int soam_m_index;
		public int oam_index;
		public byte read_value_aux;
		public int soam_m_index_aux;
		public int oam_index_aux;
		public int soam_index_aux;
		public bool is_even_cycle;
		public bool sprite_zero_in_range = false;
		public bool sprite_zero_go = false;
		public int yp;
		public int target;
		public int spriteHeight;
		public byte[] soam = new byte[512]; // in a real nes, this would only be 32, but we wish to allow more then 8 sprites per scanline
		public bool reg_2001_color_disable_latch; // the value used here is taken 
		public bool ppu_was_on;

		// installing vram address is delayed after second write to 2006, set this up here
		public int install_2006;
		public bool race_2006;
		public int install_2001;
		public bool show_bg_new; //Show background
		public bool show_obj_new; //Show sprites

		struct TempOAM
		{
			public byte oam_y;
			public byte oam_ind;
			public byte oam_attr;
			public byte oam_x;
			public byte patterns_0;
			public byte patterns_1;
		}

		TempOAM[] t_oam = new TempOAM[64];

		int ppu_addr_temp;
		void Read_bgdata(ref BGDataRecord bgdata)
		{
			for (int i = 0; i < 8; i++)
			{
				Read_bgdata(i, ref bgdata);
				runppu(1);

				if (PPUON && i==6)
				{
					ppu_was_on = true;
				}

				if (PPUON && i==7)
				{
					if (!race_2006)
						ppur.increment_hsc();

					if (ppur.status.cycle == 256 && !race_2006)
						ppur.increment_vs();

					ppu_was_on = false;
				}
			}
		}

		// attempt to emulate graphics pipeline behaviour
		// experimental
		int pixelcolor_latch_1;
		int pixelcolor_latch_2;
		void pipeline(int pixelcolor, int target, int row_check)
		{
			if (row_check > 1)
			{
				if (reg_2001.color_disable)
					pixelcolor_latch_2 &= 0x30;

				//TODO - check flashing sirens in werewolf
				//tack on the deemph bits. THESE MAY BE ORDERED WRONG. PLEASE CHECK IN THE PALETTE CODE
				xbuf[(target - 2)] = (short)(pixelcolor_latch_2 | reg_2001.intensity_lsl_6);
			}

			if (row_check >= 1)
			{
				pixelcolor_latch_2 = pixelcolor_latch_1;
			}

			pixelcolor_latch_1 = pixelcolor;
		}

		void Read_bgdata(int cycle, ref BGDataRecord bgdata)
		{
			switch (cycle)
			{
				case 0:
					ppu_addr_temp = ppur.get_ntread();
					bgdata.nt = ppubus_read(ppu_addr_temp, true, true);
					break;
				case 1:
					break;
				case 2:
					{
						ppu_addr_temp = ppur.get_atread();
						byte at = ppubus_read(ppu_addr_temp, true, true);

						//modify at to get appropriate palette shift
						if ((ppur.vt & 2) != 0) at >>= 4;
						if ((ppur.ht & 2) != 0) at >>= 2;
						at &= 0x03;
						at <<= 2;
						bgdata.at = at;						
						break;
					}
				case 3:
					break;
				case 4:
					ppu_addr_temp = ppur.get_ptread(bgdata.nt);
					bgdata.pt_0 = ppubus_read(ppu_addr_temp, true, true);
					break;
				case 5:
					break;
				case 6:
					ppu_addr_temp |= 8;
					bgdata.pt_1 = ppubus_read(ppu_addr_temp, true, true);
					break;
				case 7:
					break;
			} //switch(cycle)
		}

		public unsafe void FrameAdvance()
		{
			BGDataRecord* bgdata = stackalloc BGDataRecord[34]; //one at the end is junk, it can never be rendered

			//262 scanlines
			if (ppudead != 0)
			{
				FrameAdvance_ppudead();
				return;
			}

			Reg2002_vblank_active_pending = true;
			ppuphase = PPUPHASE.VBL;
			ppur.status.sl = 241;

			//Not sure if this is correct.  According to Matt Conte and my own tests, it is. Timing is probably off, though.
			//NOTE:  Not having this here breaks a Super Donkey Kong game.
			if (PPUON) reg_2003 = 0;

			//this was repeatedly finetuned from the fceux days thrugh the old cpu core and into the new one to pass 05-nmi_timing.nes
			//note that there is still some leniency. for instance, 4,2 will pass in addition to 3,3
			const int delay = 6;
			runppu(3);
			bool nmi_destiny = reg_2000.vblank_nmi_gen && Reg2002_vblank_active;
			runppu(3);
			if (nmi_destiny) nes.cpu.NMI = true;

			nes.Board.AtVsyncNMI();
			runppu(postNMIlines * kLineTime - delay);

			//this seems to happen just before the dummy scanline begins
			Reg2002_objhit = Reg2002_objoverflow = 0;
			Reg2002_vblank_clear_pending = true;

			idleSynch ^= true;

			//render 241 scanlines (including 1 dummy at beginning)
			for (int sl = 0; sl < 241; sl++)
			{
				ppur.status.cycle = 0;

				ppur.status.sl = sl;

				spr_true_count = 0;
				soam_index = 0;
				soam_m_index = 0;
				soam_m_index_aux = 0;
				oam_index_aux = 0;
				oam_index = 0;
				is_even_cycle = true;
				sprite_eval_write = true;
				sprite_zero_go = false;
				if (sprite_zero_in_range)
					sprite_zero_go = true;

				sprite_zero_in_range = false;

				yp = sl - 1;
				ppuphase = PPUPHASE.BG;

				// "If PPUADDR is not less then 8 when rendering starts, the first 8 bytes in OAM and written to from 
				// the current location off PPUADDR"			
				if (sl == 0 && PPUON && reg_2003 >= 8 && region==Region.NTSC)
				{
					for (int i = 0; i < 8; i++)
					{
						OAM[i] = OAM[reg_2003 & 0xF8 + i];
					}
				}
				
				if (NTViewCallback != null && yp == NTViewCallback.Scanline) NTViewCallback.Callback();
				if (PPUViewCallback != null && yp == PPUViewCallback.Scanline) PPUViewCallback.Callback();

				//ok, we're also going to draw here.
				//unless we're on the first dummy scanline
				if (sl != 0)
				{
					//the main scanline rendering loop:
					//32 times, we will fetch a tile and then render 8 pixels.
					//two of those tiles were read in the last scanline.
					int yp_shift = yp << 8;
					for (int xt = 0; xt < 32; xt++)
					{
						int xstart = xt << 3;
						target = yp_shift + xstart;
						int rasterpos = xstart;

						spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

						//check all the conditions that can cause things to render in these 8px
						bool renderspritenow = show_obj_new && (xt > 0 || reg_2001.show_obj_leftmost);
						bool renderbgnow;
						bool hit_pending = false;

						for (int xp = 0; xp < 8; xp++, rasterpos++)
						{
							//////////////////////////////////////////////////
							//Sprite Evaluation Start
							//////////////////////////////////////////////////
							if (ppur.status.cycle <= 63 && !is_even_cycle)
							{
								// the first 64 cycles of each scanline are used to initialize sceondary OAM 
								// the actual effect setting a flag that always returns 0xFF from a OAM read
								// this is a bit of a shortcut to save some instructions
								// data is read from OAM as normal but never used
								soam[soam_index] = 0xFF;
								soam_index++;
							}
							if (ppur.status.cycle == 64)
							{
								soam_index = 0;
								oam_index = 0;// reg_2003;
							}

							// otherwise, scan through OAM and test if sprites are in range
							// if they are, they get copied to the secondary OAM 
							if (ppur.status.cycle >= 64)
							{
								if (oam_index >= 256)
								{
									oam_index = 0;
									sprite_eval_write = false;
								}

								if (is_even_cycle && oam_index<256)
								{
									if ((oam_index + soam_m_index) < 256)
										read_value = OAM[oam_index + soam_m_index];
									else
										read_value = OAM[oam_index + soam_m_index - 256];
								}
								else if (!sprite_eval_write)
								{
									// if we don't write sprites anymore, just scan through the oam
									read_value = soam[0];
									oam_index+=4;
								}
								else if (sprite_eval_write)
								{
									//look for sprites 
									if (spr_true_count==0 && soam_index<8)
									{
										soam[soam_index*4] = read_value;
									}

									if (soam_index < 8)
									{
										if (yp >= read_value && yp < read_value + spriteHeight && spr_true_count == 0)
										{
											//a flag gets set if sprite zero is in range
											if (oam_index == 0)//reg_2003)
												sprite_zero_in_range = true;

											spr_true_count++;
											soam_m_index++;
										}
										else if (spr_true_count > 0 && spr_true_count < 4)
										{
											soam[soam_index * 4 + soam_m_index] = read_value;

											soam_m_index++;

											spr_true_count++;
											if (spr_true_count == 4)
											{
												oam_index+=4;
												soam_index++;
												if (soam_index == 8)
												{
													// oam_index could be pathologically misaligned at this point, so we have to find the next 
													// nearest actual sprite to work on >8 sprites per scanline option
													oam_index_aux = (oam_index%4)*4;
												}

												soam_m_index = 0;
												spr_true_count = 0;
											}
										}
										else
										{
											oam_index+=4;
										}
									}
									else if (soam_index>=8)
									{
										if (yp >= read_value && yp < read_value + spriteHeight && PPUON)
										{
											hit_pending = true;
											//Reg2002_objoverflow = true;
										}

										if (yp >= read_value && yp < read_value + spriteHeight && spr_true_count == 0)
										{
											spr_true_count++;
											soam_m_index++;
										}
										else if (spr_true_count > 0 && spr_true_count < 4)
										{
											soam_m_index++;

											spr_true_count++;
											if (spr_true_count == 4)
											{
												oam_index+=4;
												soam_index++;
												soam_m_index = 0;
												spr_true_count = 0;
											}
										}
										else
										{
											oam_index+=4;
											if (soam_index==8)
											{
												soam_m_index++; // glitchy increment
												soam_m_index &= 3;
											}
												
										}

										read_value = soam[0]; //writes change to reads 
									}

								}

							}

							//////////////////////////////////////////////////
							//Sprite Evaluation End
							//////////////////////////////////////////////////

							//process the current clock's worth of bg data fetching
							//this needs to be split into 8 pieces or else exact sprite 0 hitting wont work due to the cpu not running while the sprite renders below


							if (PPUON)
								Read_bgdata(xp, ref bgdata[xt + 2]);

							runppu(1);

							if (PPUON && xp == 6)
							{
								ppu_was_on = true;
							}

							if (PPUON && xp == 7)
							{
								if (!race_2006)
									ppur.increment_hsc();

								if (ppur.status.cycle == 256 && !race_2006)
									ppur.increment_vs();

								ppu_was_on = false;
							}

							if (hit_pending)
							{
								hit_pending = false;
								Reg2002_objoverflow = true;
							}

							renderbgnow =  show_bg_new && (xt > 0 || reg_2001.show_bg_leftmost);
							//bg pos is different from raster pos due to its offsetability.
							//so adjust for that here
							int bgpos = rasterpos + ppur.fh;
							int bgpx = bgpos & 7;
							int bgtile = bgpos >> 3;

							int pixel = 0, pixelcolor = PALRAM[pixel];

							//according to qeed's doc, use palette 0 or $2006's value if it is & 0x3Fxx
							//at one point I commented this out to fix bottom-left garbage in DW4. but it's needed for full_nes_palette. 
							//solution is to only run when PPU is actually OFF (left-suppression doesnt count)
							if (!PPUON)
							{
								// if there's anything wrong with how we're doing this, someone please chime in
								int addr = ppur.get_2007access();
								if ((addr & 0x3F00) == 0x3F00)
								{
									pixel = addr & 0x1F;
								}
								pixelcolor = PALRAM[pixel];
								pixelcolor |= 0x8000; //whats this? i think its a flag to indicate a hidden background to be used by the canvas filling logic later
							}

							//generate the BG data
							if (renderbgnow)
							{
								byte pt_0 = bgdata[bgtile].pt_0;
								byte pt_1 = bgdata[bgtile].pt_1;
								int sel = 7 - bgpx;
								pixel = ((pt_0 >> sel) & 1) | (((pt_1 >> sel) & 1) << 1);
								if (pixel != 0)
									pixel |= bgdata[bgtile].at;
								pixelcolor = PALRAM[pixel];
							}

							if (!nes.Settings.DispBackground)
								pixelcolor = 0x8000; //whats this? i think its a flag to indicate a hidden background to be used by the canvas filling logic later

							//look for a sprite to be drawn
							bool havepixel = false;
							for (int s = 0; s < soam_index_prev; s++)
							{
								int x = t_oam[s].oam_x;
								if (rasterpos >= x && rasterpos < x + 8)
								{
									//build the pixel.
									//fetch the LSB of the patterns
									int spixel = t_oam[s].patterns_0 & 1;
									spixel |= (t_oam[s].patterns_1 & 1) << 1;

									//shift down the patterns so the next pixel is in the LSB
									t_oam[s].patterns_0 >>= 1;
									t_oam[s].patterns_1 >>= 1;

									//bail out if we already have a pixel from a higher priority sprite.
									//notice that we continue looping anyway, so that we can shift down the patterns
									//transparent pixel bailout
									if (!renderspritenow || havepixel || spixel == 0) continue;

									havepixel = true;

									//TODO - make sure we dont trigger spritehit if the edges are masked for either BG or OBJ
									//spritehit:
									//1. is it sprite#0?
									//2. is the bg pixel nonzero?
									//then, it is spritehit.
									Reg2002_objhit |= (sprite_zero_go && s == 0 && pixel != 0 && rasterpos < 255 && show_bg_new && show_obj_new);

									//priority handling, if in front of BG:
									bool drawsprite = !(((t_oam[s].oam_attr & 0x20) != 0) && ((pixel & 3) != 0));
									if (drawsprite && nes.Settings.DispSprites)
									{
										//bring in the palette bits and palettize
										spixel |= (t_oam[s].oam_attr & 3) << 2;
										//save it for use in the framebuffer
										pixelcolor = PALRAM[0x10 + spixel];
									}
								} //rasterpos in sprite range
							} //oamcount loop
							 
							pipeline(pixelcolor, target, xt*32+xp);
							target++;							
						} //loop across 8 pixels
					} //loop across 32 tiles
				}
				else
					for (int xt = 0; xt < 32; xt++)
						Read_bgdata(ref bgdata[xt + 2]);

				// normally only 8 sprites are allowed, but with a particular setting we can have more then that
				// this extra bit takes care of it quickly
				soam_index_aux = 8;

				if (nes.Settings.AllowMoreThanEightSprites)
				{
					while (oam_index_aux < 64 && soam_index_aux<64)
					{
						//look for sprites 
						soam[soam_index_aux * 4] = OAM[oam_index_aux * 4];
						read_value_aux = OAM[oam_index_aux * 4];
						if (yp >= read_value_aux && yp < read_value_aux + spriteHeight)
						{
							soam[soam_index_aux * 4 + 1] = OAM[oam_index_aux * 4 + 1];
							soam[soam_index_aux * 4 + 2] = OAM[oam_index_aux * 4 + 2];
							soam[soam_index_aux * 4 + 3] = OAM[oam_index_aux * 4 + 3];
							soam_index_aux++;
							oam_index_aux++;
						}
						else
						{
							oam_index_aux++;
						}
					}
				}

				soam_index_prev = soam_index_aux;

				if (soam_index_prev > 8 && !nes.Settings.AllowMoreThanEightSprites)
					soam_index_prev = 8;

				ppuphase = PPUPHASE.OBJ;

				spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

				for (int s = 0; s < 8; s++)
				{
					bool junksprite = (!PPUON);

					t_oam[s].oam_y = soam[s * 4];
					t_oam[s].oam_ind = soam[s * 4 + 1];
					t_oam[s].oam_attr = soam[s * 4 + 2];
					t_oam[s].oam_x = soam[s * 4 + 3];

					int line = yp - t_oam[s].oam_y;
					if ((t_oam[s].oam_attr & 0x80) != 0) //vflip
						line = spriteHeight - line - 1;

					int patternNumber = t_oam[s].oam_ind;
					int patternAddress;

					//8x16 sprite handling:
					if (reg_2000.obj_size_16)
					{
						int bank = (patternNumber & 1) << 12;
						patternNumber = patternNumber & ~1;
						patternNumber |= (line >> 3) & 1;
						patternAddress = (patternNumber << 4) | bank;
					}
					else
						patternAddress = (patternNumber << 4) | (reg_2000.obj_pattern_hi << 12);

					//offset into the pattern for the current line.
					//tricky: tall sprites have already had lines>8 taken care of by getting a new pattern number above.
					//so we just need the line offset for the second pattern
					patternAddress += line & 7;

					//garbage nametable fetches + scroll resets
					int garbage_todo = 2;

					ppubus_read(ppur.get_ntread(), true, true);

					if (PPUON)
					{
						if (sl == 0 && ppur.status.cycle == 304)
						{

							read_value = t_oam[s].oam_y;
							runppu(1);
							
							if (PPUON) ppur.install_latches();

							read_value = t_oam[s].oam_ind;
							runppu(1);
							

							garbage_todo = 0;
						}
						if ((sl != 0) && ppur.status.cycle == 256)
						{

							read_value = t_oam[s].oam_y;

							runppu(1);
							
							if (target<=61441 && target > 0 && s==0)
							{
								pipeline(0, target,256);
								target++;
							}

							//at 257: 3d world runner is ugly if we do this at 256
							if (PPUON) ppur.install_h_latches();
							read_value = t_oam[s].oam_ind;
							runppu(1);
							
							if (target <= 61441 && target > 0 && s==0)
							{
								pipeline(0, target, 257);  //  last pipeline call option 1 of 2
							}
							garbage_todo = 0;
						}
					}

					for (int i = 0; i < garbage_todo; i++)
					{
						if (i==0)
							read_value = t_oam[s].oam_y;
						else
							read_value = t_oam[s].oam_ind;
								
						runppu(1);
							
						if (i == 0)
						{
							if (target <= 61441 && target > 0 && s==0)
							{
								pipeline(0, target,256);
								target++;
							}
						}
						else
						{
							if (target <= 61441 && target > 0 && s==0)
							{
								pipeline(0, target, 257);  //  last pipeline call option 2 of 2
							}
						}
					}

					ppubus_read(ppur.get_atread(), true, true); //at or nt?

					read_value = t_oam[s].oam_attr;
					runppu(1);

					read_value = t_oam[s].oam_x;
					runppu(1);

					// if the PPU is off, we don't put anything on the bus
					if (junksprite)
					{
						ppubus_read(patternAddress, true, false);
						ppubus_read(patternAddress, true, false);
						runppu(kFetchTime * 2);
					}
					else
					{
						int addr = patternAddress;
						t_oam[s].patterns_0 = ppubus_read(addr, true, true);
						read_value = t_oam[s].oam_x;
						runppu(kFetchTime);

						addr += 8;
						t_oam[s].patterns_1 = ppubus_read(addr, true, true);
						read_value = t_oam[s].oam_x;
						runppu(kFetchTime);

						// hflip
						if ((t_oam[s].oam_attr & 0x40) == 0)
						{
							t_oam[s].patterns_0 = BitReverse.Byte8[t_oam[s].patterns_0];
							t_oam[s].patterns_1 = BitReverse.Byte8[t_oam[s].patterns_1];
						}

						// if the sprites attribute is 0xFF, then this indicates a non-existent sprite
						// I think the logic here is that bits 2-4 in OAM are disabled, but soam is initialized with 0xFF
						// so the only way a sprite could have an 0xFF attribute is if it is not in the scope of the scanline
						if (t_oam[s].oam_attr==0xFF)
						{
							t_oam[s].patterns_0 = 0;
							t_oam[s].patterns_1 = 0;
						}

					}

				} // sprite pattern fetch loop
				
				//now do the same for extra sprites, but without any cycles run
				if (soam_index_aux>8)
				{
					for (int s = 8; s < soam_index_aux; s++)
					{
						t_oam[s].oam_y = soam[s * 4];
						t_oam[s].oam_ind = soam[s * 4 + 1];
						t_oam[s].oam_attr = soam[s * 4 + 2];
						t_oam[s].oam_x = soam[s * 4 + 3];

						int line = yp - t_oam[s].oam_y;
						if ((t_oam[s].oam_attr & 0x80) != 0) //vflip
							line = spriteHeight - line - 1;

						int patternNumber = t_oam[s].oam_ind;
						int patternAddress;

						//8x16 sprite handling:
						if (reg_2000.obj_size_16)
						{
							int bank = (patternNumber & 1) << 12;
							patternNumber = patternNumber & ~1;
							patternNumber |= (line >> 3) & 1;
							patternAddress = (patternNumber << 4) | bank;
						}
						else
							patternAddress = (patternNumber << 4) | (reg_2000.obj_pattern_hi << 12);

						//offset into the pattern for the current line.
						//tricky: tall sprites have already had lines>8 taken care of by getting a new pattern number above.
						//so we just need the line offset for the second pattern
						patternAddress += line & 7;

						ppubus_read(ppur.get_ntread(), true, false);

						ppubus_read(ppur.get_atread(), true, false); //at or nt?

						int addr = patternAddress;
						t_oam[s].patterns_0 = ppubus_read(addr, true, false);

						addr += 8;
						t_oam[s].patterns_1 = ppubus_read(addr, true, false);

						// hflip
						if ((t_oam[s].oam_attr & 0x40) == 0)
						{
							t_oam[s].patterns_0 = BitReverse.Byte8[t_oam[s].patterns_0];
							t_oam[s].patterns_1 = BitReverse.Byte8[t_oam[s].patterns_1];
						}

						// if the sprites attribute is 0xFF, then this indicates a non-existent sprite
						// I think the logic here is that bits 2-4 in OAM are disabled, but soam is initialized with 0xFF
						// so the only way a sprite could have an 0xFF attribute is if it is not in the scope of the scanline
						if (t_oam[s].oam_attr == 0xFF)
						{
							t_oam[s].patterns_0 = 0;
							t_oam[s].patterns_1 = 0;
						}

					} // sprite pattern fetch loop

				}

				ppuphase = PPUPHASE.BG;

				// fetch BG: two tiles for next line
				for (int xt = 0; xt < 2; xt++)
				{
					Read_bgdata(ref bgdata[xt]);
				}

				// this sequence is tuned to pass 10-even_odd_timing.nes				
				runppu(1);
				
				runppu(1);
				
				runppu(1);
					
				runppu(1);
				bool evenOddDestiny = PPUON;

				// After memory access 170, the PPU simply rests for 4 cycles (or the
				// equivelant of half a memory access cycle) before repeating the whole
				// pixel/scanline rendering process. If the scanline being rendered is the very
				// first one on every second frame, then this delay simply doesn't exist.
				if (sl == 0 && idleSynch && evenOddDestiny && chopdot)
				{ }
				else
					runppu(1);

			} // scanline loop

			ppur.status.sl = 241;

			//idle for pre NMI lines
			runppu(preNMIlines * kLineTime);
		} //FrameAdvance

		void FrameAdvance_ppudead()
		{
			//not quite emulating all the NES power up behavior
			//since it is known that the NES ignores writes to some
			//register before around a full frame, but no games
			//should write to those regs during that time, it needs
			//to wait for vblank

			runppu(241 * kLineTime-3);// -8*3);
			ppudead--;
		}
	}
}
