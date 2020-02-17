//TODO - correctly emulate PPU OFF state

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	sealed partial class PPU
	{
		struct BGDataRecord
		{
			public byte nt, at;
			public byte pt_0, pt_1;
		}

		BGDataRecord[] bgdata = new BGDataRecord[34]; 

		public short[] xbuf = new short[256 * 240];

		// values here are used in sprite evaluation
		public int spr_true_count;
		public bool sprite_eval_write;
		public byte read_value;
		public int soam_index;
		public int soam_index_prev;
		public int soam_m_index;
		public int oam_index;
		public int oam_index_aux;
		public int soam_index_aux;
		public bool is_even_cycle;
		public bool sprite_zero_in_range = false;
		public bool sprite_zero_go = false;
		public int yp;
		public int target;
		public int spriteHeight;
		public byte[] soam = new byte[256]; // in a real nes, this would only be 32, but we wish to allow more then 8 sprites per scanline
		public bool ppu_was_on;
		public byte[] sl_sprites = new byte[3 * 256];

		// installing vram address is delayed after second write to 2006, set this up here
		public int install_2006;
		public bool race_2006, race_2006_2;
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

		// attempt to emulate graphics pipeline behaviour
		// experimental
		int pixelcolor_latch_1;

		void pipeline(int pixelcolor, int row_check)
		{
			if (row_check > 0)
			{
				if (reg_2001.color_disable)
					pixelcolor_latch_1 &= 0x30;

				//TODO - check flashing sirens in werewolf
				//tack on the deemph bits. THESE MAY BE ORDERED WRONG. PLEASE CHECK IN THE PALETTE CODE
				xbuf[(target - 1)] = (short)(pixelcolor_latch_1 | reg_2001.intensity_lsl_6);
			}
		
			pixelcolor_latch_1 = pixelcolor;
		}

		void Read_bgdata(int cycle, int i)
		{
			switch (cycle)
			{
				case 0:
					ppu_addr_temp = ppur.get_ntread();
					bgdata[i].nt = ppubus_read(ppu_addr_temp, true, true);
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
						bgdata[i].at = at;
						break;
					}
				case 3:
					break;
				case 4:
					ppu_addr_temp = ppur.get_ptread(bgdata[i].nt);
					bgdata[i].pt_0 = ppubus_read(ppu_addr_temp, true, true);
					break;
				case 5:
					break;
				case 6:
					ppu_addr_temp |= 8;
					bgdata[i].pt_1 = ppubus_read(ppu_addr_temp, true, true);
					break;
				case 7:
					break;
			} //switch(cycle)
		}

		// these are states for the ppu incrementer
		public bool do_vbl;
		public bool do_active_sl;
		public bool do_pre_vbl;

		bool nmi_destiny;
		bool evenOddDestiny;
		static int start_up_offset = 2;
		int NMI_offset;
		int yp_shift;
		int sprite_eval_cycle;
		int xt;
		int xp;
		int xstart;
		int rasterpos;
		bool renderspritenow;
		int s;
		int ppu_aux_index;
		bool junksprite;
		int line;
		int patternNumber;
		int patternAddress;
		int temp_addr;

		public void ppu_init_frame()
		{
			ppur.status.sl = 241 + preNMIlines;
			ppur.status.cycle = 0;

			// These things happen at the start of every frame
			ppuphase = PPU_PHASE_VBL;
			bgdata = new BGDataRecord[34];
		}

		public void TickPPU_VBL()
		{
			if (ppur.status.cycle == 0 && ppur.status.sl == 241 + preNMIlines)
			{
				nmi_destiny = reg_2000.vblank_nmi_gen && Reg2002_vblank_active;
				if (cpu_stepcounter == 2) { NMI_offset = 1; }
				else if (cpu_stepcounter == 1) { NMI_offset = 2; }
				else { NMI_offset = 0; }
			}
			else if (ppur.status.cycle <= 2 && nmi_destiny)
			{
				nmi_destiny &= reg_2000.vblank_nmi_gen && Reg2002_vblank_active;
			}
			else if (ppur.status.cycle == (3 + NMI_offset) && ppur.status.sl == 241 + preNMIlines)
			{
				if (nmi_destiny) { nes.cpu.NMI = true; }
				nes.Board.AtVsyncNMI();
			}

			if (ppur.status.cycle == 340)
			{
				if (ppur.status.sl == 241 + preNMIlines + postNMIlines - 1)
				{
					Reg2002_vblank_clear_pending = true;
					idleSynch ^= true;
					Reg2002_objhit = Reg2002_objoverflow = 0;
				}
			}

			runppu(); // note cycle ticks inside runppu

			if (ppur.status.cycle == 341)
			{
				if (Reg2002_vblank_clear_pending)
				{
					Reg2002_vblank_active = 0;
					Reg2002_vblank_clear_pending = false;
				}

				ppur.status.cycle = 0;
				ppur.status.sl++;
				if (ppur.status.sl == 241 + preNMIlines + postNMIlines)
				{
					do_vbl = false;
					ppur.status.sl = 0;
					do_active_sl = true;
				}
			}
		}

		public void TickPPU_active()
		{
			if (ppur.status.cycle < 256)
			{
				if (ppur.status.cycle == 0)
				{
					ppur.status.cycle = 0;

					spr_true_count = 0;
					soam_index = 0;
					soam_m_index = 0;
					oam_index_aux = 0;
					oam_index = 0;
					is_even_cycle = true;
					sprite_eval_write = true;
					sprite_zero_go = sprite_zero_in_range;

					sprite_zero_in_range = false;

					yp = ppur.status.sl - 1;
					ppuphase = PPU_PHASE_BG;

					// "If PPUADDR is not less then 8 when rendering starts, the first 8 bytes in OAM are written to from 
					// the current location of PPUADDR"			
					if (ppur.status.sl == 0 && PPUON && reg_2003 >= 8 && region == Region.NTSC)
					{
						for (int i = 0; i < 8; i++)
						{
							OAM[i] = OAM[(reg_2003 & 0xF8) + i];
						}
					}

					if (NTViewCallback != null && yp == NTViewCallback.Scanline) NTViewCallback.Callback();
					if (PPUViewCallback != null && yp == PPUViewCallback.Scanline) PPUViewCallback.Callback();

					// set up intial values to use later
					yp_shift = yp << 8;
					xt = 0;
					xp = 0;

					sprite_eval_cycle = 0;

					xstart = xt << 3;
					target = yp_shift + xstart;
					rasterpos = xstart;

					spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

					//check all the conditions that can cause things to render in these 8px
					renderspritenow = show_obj_new && (xt > 0 || reg_2001.show_obj_leftmost);
				}

				if (ppur.status.sl != 0)
				{
					/////////////////////////////////////////////
					// Sprite Evaluation Start
					/////////////////////////////////////////////
					
					if (sprite_eval_cycle < 64)
					{
						// the first 64 cycles of each scanline are used to initialize sceondary OAM 
						// the actual effect setting a flag that always returns 0xFF from a OAM read
						// this is a bit of a shortcut to save some instructions
						// data is read from OAM as normal but never used
						if (!is_even_cycle)
						{
							soam[soam_index] = 0xFF;
							soam_index++;
						}
					}
					// otherwise, scan through OAM and test if sprites are in range
					// if they are, they get copied to the secondary OAM 
					else
					{
						if (sprite_eval_cycle == 64)
						{
							soam_index = 0;
							oam_index = reg_2003;
						}

						if (oam_index >= 256)
						{
							oam_index = 0;
							sprite_eval_write = false;
						}

						if (is_even_cycle)
						{
							if ((oam_index + soam_m_index) < 256)
								read_value = OAM[oam_index + soam_m_index];
							else
								read_value = OAM[oam_index + soam_m_index - 256];
						}
						else if (sprite_eval_write)
						{
							//look for sprites 
							if (spr_true_count == 0 && soam_index < 8)
							{
								soam[soam_index * 4] = read_value;
							}

							if (soam_index < 8)
							{
								if (yp >= read_value && yp < read_value + spriteHeight && spr_true_count == 0)
								{
									//a flag gets set if sprite zero is in range
									if (oam_index == reg_2003) { sprite_zero_in_range = true; }
										
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
										oam_index += 4;
										soam_index++;
										if (soam_index == 8)
										{
											// oam_index could be pathologically misaligned at this point, so we have to find the next 
											// nearest actual sprite to work on >8 sprites per scanline option
											oam_index_aux = (oam_index % 4) * 4;
										}

										soam_m_index = 0;
										spr_true_count = 0;
									}
								}
								else
								{
									oam_index += 4;
								}
							}
							else
							{
								if (yp >= read_value && yp < read_value + spriteHeight && PPUON)
								{
									Reg2002_objoverflow = true;
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
										oam_index += 4;
										soam_index++;
										soam_m_index = 0;
										spr_true_count = 0;
									}
								}
								else
								{
									oam_index += 4;
									if (soam_index == 8)
									{
										soam_m_index++; // glitchy increment
										soam_m_index &= 3;
									}

								}

								read_value = soam[0]; //writes change to reads 
							}
						}
						else
						{
							// if we don't write sprites anymore, just scan through the oam
							read_value = soam[0];
							oam_index += 4;
						}
					}
					
					/////////////////////////////////////////////
					// Sprite Evaluation End
					/////////////////////////////////////////////

					int pixel = 0, pixelcolor = PALRAM[pixel];

					//process the current clock's worth of bg data fetching
					//this needs to be split into 8 pieces or else exact sprite 0 hitting wont work 
					// due to the cpu not running while the sprite renders below
					if (PPUON) { Read_bgdata(xp, xt + 2); }
					//according to qeed's doc, use palette 0 or $2006's value if it is & 0x3Fxx
					//at one point I commented this out to fix bottom-left garbage in DW4. but it's needed for full_nes_palette. 
					//solution is to only run when PPU is actually OFF (left-suppression doesnt count)
					else
					{
						// if there's anything wrong with how we're doing this, someone please chime in
						int addr = ppur.get_2007access();
						if ((addr & 0x3F00) == 0x3F00)
						{
							pixel = addr & 0x1F;
						}
						pixelcolor = PALRAM[pixel];
						pixelcolor |= 0x8000;
					}

					//generate the BG data
					if (show_bg_new && (xt > 0 || reg_2001.show_bg_leftmost))
					{
						int bgtile = (rasterpos + ppur.fh) >> 3;
						byte pt_0 = bgdata[bgtile].pt_0;
						byte pt_1 = bgdata[bgtile].pt_1;
						int sel = 7 - (rasterpos + ppur.fh) & 7;
						pixel = ((pt_0 >> sel) & 1) | (((pt_1 >> sel) & 1) << 1);
						if (pixel != 0)
							pixel |= bgdata[bgtile].at;
						pixelcolor = PALRAM[pixel];
					}

					if (!nes.Settings.DispBackground)
						pixelcolor = 0x8000;

					//check if the pixel has a sprite in it
					if (sl_sprites[256 + xt * 8 + xp] != 0 && renderspritenow)
					{
						int s = sl_sprites[xt * 8 + xp];
						int spixel = sl_sprites[256 + xt * 8 + xp];
						int temp_attr = sl_sprites[512 + xt * 8 + xp];
						
						//TODO - make sure we dont trigger spritehit if the edges are masked for either BG or OBJ
						//spritehit:
						//1. is it sprite#0?
						//2. is the bg pixel nonzero?
						//then, it is spritehit.
						Reg2002_objhit |= (sprite_zero_go && s == 0 && pixel != 0 && rasterpos < 255 && show_bg_new && show_obj_new);
						//priority handling, if in front of BG:
						bool drawsprite = !(((temp_attr & 0x20) != 0) && ((pixel & 3) != 0));
						if (drawsprite && nes.Settings.DispSprites)
						{
							//bring in the palette bits and palettize
							spixel |= (temp_attr & 3) << 2;
							//save it for use in the framebuffer
							pixelcolor = PALRAM[0x10 + spixel];
						}
					} //oamcount loop

					runppu();

					if (xp == 6 && PPUON)
					{
						ppu_was_on = true;
						if (ppur.status.cycle == 255) { race_2006 = true; }
					}

					if (xp == 7 && PPUON)
					{
						ppur.increment_hsc();

						if ((ppur.status.cycle == 256) && ppu_was_on)
						{
							ppur.increment_vs();
						}

						if (race_2006_2)
						{
							if (ppur.status.cycle == 256)
							{
								ppur.fv &= ppur._fv;
								ppur.v &= ppur._v;
								ppur.h &= ppur._h;
								ppur.vt &= ppur._vt;
								ppur.ht &= ppur._ht;
							}
							else
							{
								ppur.fv = ppur._fv;
								ppur.v = ppur._v;
								ppur.h &= ppur._h;
								ppur.vt = ppur._vt;
								ppur.ht &= ppur._ht;
							}
						}

						ppu_was_on = false;
					}

					race_2006_2 = false;

					pipeline(pixelcolor, xt * 8 + xp);
					target++;

					// clear out previous sprites from scanline buffer
					sl_sprites[256 + xt * 8 + xp] = 0;

					// end of visible part of the scanline
					sprite_eval_cycle++;
					xp++;
					rasterpos++;

					if (xp == 8)
					{
						xp = 0;
						xt++;

						xstart = xt << 3;
						target = yp_shift + xstart;
						rasterpos = xstart;

						spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

						//check all the conditions that can cause things to render in these 8px
						renderspritenow = show_obj_new && (xt > 0 || reg_2001.show_obj_leftmost);
					}				
				}
				else
				{
					// if scanline is the pre-render line, we just read BG data
					Read_bgdata(xp, xt + 2);

					runppu();

					if (xp == 6 && PPUON)
					{
						ppu_was_on = true;
						if (ppur.status.cycle == 255) { race_2006 = true; }
						
					}

					if (xp == 7 && PPUON)
					{
						ppur.increment_hsc();

						if ((ppur.status.cycle == 256) && ppu_was_on)
						{
							ppur.increment_vs();
						}

						if(race_2006_2)
						{
							if (ppur.status.cycle == 256)
							{
								ppur.fv &= ppur._fv;
								ppur.v &= ppur._v;
								ppur.h &= ppur._h;
								ppur.vt &= ppur._vt;
								ppur.ht &= ppur._ht;
							}
							else
							{
								ppur.fv = ppur._fv;
								ppur.v = ppur._v;
								ppur.h &= ppur._h;
								ppur.vt = ppur._vt;
								ppur.ht &= ppur._ht;
							}
						}

						ppu_was_on = false;
					}

					race_2006_2 = false;

					xp++;

					if (xp == 8)
					{
						xp = 0;
						xt++;
					}
				}
			}
			else if (ppur.status.cycle < 320)
			{
				// after we are done with the visible part of the frame, we reach sprite transfer to temp OAM tables and such
				if (ppur.status.cycle == 256)
				{
					// do the more then 8 sprites stuff here where it is convenient
					// normally only 8 sprites are allowed, but with a particular setting we can have more then that
					// this extra bit takes care of it quickly
					soam_index_aux = 8;

					if (nes.Settings.AllowMoreThanEightSprites)
					{
						while (oam_index_aux < 64 && soam_index_aux < 64)
						{
							//look for sprites 
							soam[soam_index_aux * 4] = OAM[oam_index_aux * 4];
							if (yp >= OAM[oam_index_aux * 4] && yp < OAM[oam_index_aux * 4] + spriteHeight)
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

					ppuphase = PPU_PHASE_OBJ;

					spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

					s = 0;
					ppu_aux_index = 0;

					junksprite = !PPUON;

					t_oam[s].oam_y = soam[s * 4];
					t_oam[s].oam_ind = soam[s * 4 + 1];
					t_oam[s].oam_attr = soam[s * 4 + 2];
					t_oam[s].oam_x = soam[s * 4 + 3];

					line = yp - t_oam[s].oam_y;
					if ((t_oam[s].oam_attr & 0x80) != 0) //vflip
						line = spriteHeight - line - 1;

					patternNumber = t_oam[s].oam_ind;
				}

				switch (ppu_aux_index)
				{
					case 0:
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

						ppubus_read(ppur.get_ntread(), true, true);

						read_value = t_oam[s].oam_y;
						runppu();
						break;
					case 1:
						if (ppur.status.sl == 0 && ppur.status.cycle == 305 && PPUON)
						{
							ppur.install_latches();

							read_value = t_oam[s].oam_ind;
							runppu();

						}
						else if ((ppur.status.sl != 0) && ppur.status.cycle == 257 && PPUON)
						{

							if (target <= 61441 && target > 0 && s == 0)
							{
								pipeline(0, 256);   //  last pipeline call option 1 of 2
								target++;
							}

							//at 257: 3d world runner is ugly if we do this at 256
							if (PPUON/* && !race_2006_2*/) { ppur.install_h_latches(); }
							race_2006_2 = false;
							read_value = t_oam[s].oam_ind;
							runppu();

							/*
							if (target <= 61441 && target > 0 && s == 0)
							{
								//pipeline(0, 257);
							}
							*/
						}
						else
						{
							if (target <= 61441 && target > 0 && s == 0)
							{
								pipeline(0, 256);  //  last pipeline call option 2 of 2
								target++;
							}

							read_value = t_oam[s].oam_ind;
							runppu();

							/*
							if (target <= 61441 && target > 0 && s == 0)
							{
								//pipeline(0, 257);
							}
							*/
						}
						break;

					case 2:
						ppubus_read(ppur.get_atread(), true, true); //at or nt?
						read_value = t_oam[s].oam_attr;
						runppu();
						break;

					case 3:
						read_value = t_oam[s].oam_x;
						runppu();
						break;

					case 4:
						// if the PPU is off, we don't put anything on the bus
						if (junksprite)
						{
							ppubus_read(patternAddress, true, false);
							runppu();
						}
						else
						{
							temp_addr = patternAddress;
							t_oam[s].patterns_0 = ppubus_read(temp_addr, true, true);
							read_value = t_oam[s].oam_x;
							runppu();
						}
						break;
					case 5:
						runppu();
						break;
					case 6:
						// if the PPU is off, we don't put anything on the bus
						if (junksprite)
						{
							ppubus_read(patternAddress, true, false);
							runppu();
						}
						else
						{
							temp_addr += 8;
							t_oam[s].patterns_1 = ppubus_read(temp_addr, true, true);
							read_value = t_oam[s].oam_x;
							runppu();
						}
						break;
					case 7:
						// if the PPU is off, we don't put anything on the bus
						if (junksprite)
						{
							runppu();
						}
						else
						{
							runppu();

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

						}
						break;
				}

				ppu_aux_index++;
				if (ppu_aux_index == 8)
				{
					// now that we have a sprite, we can fill in the next scnaline's sprite pixels with it
					// this saves quite a bit of processing compared to checking each pixel

					if (s < soam_index_prev && (ppur.status.sl != 0) && (ppur.status.sl != 240))
					{
						int temp_x = t_oam[s].oam_x;
						for (int i = 0; (temp_x + i) < 256 && i < 8; i++)
						{
							if (sl_sprites[256 + temp_x + i] == 0)
							{
								if (t_oam[s].patterns_0.Bit(i) || t_oam[s].patterns_1.Bit(i))
								{
									int spixel = t_oam[s].patterns_0.Bit(i) ? 1 : 0;
									spixel |= (t_oam[s].patterns_1.Bit(i) ? 2 : 0);

									sl_sprites[temp_x + i] = (byte)s;
									sl_sprites[256 + temp_x + i] = (byte)spixel;
									sl_sprites[512 + temp_x + i] = t_oam[s].oam_attr;
								}
							}
						}
					}

					ppu_aux_index = 0;
					s++;

					if (s < 8)
					{
						junksprite = !PPUON;

						t_oam[s].oam_y = soam[s * 4];
						t_oam[s].oam_ind = soam[s * 4 + 1];
						t_oam[s].oam_attr = soam[s * 4 + 2];
						t_oam[s].oam_x = soam[s * 4 + 3];

						line = yp - t_oam[s].oam_y;
						if ((t_oam[s].oam_attr & 0x80) != 0) //vflip
							line = spriteHeight - line - 1;

						patternNumber = t_oam[s].oam_ind;
					}
					else
					{
						// repeat all the above steps for more then 8 sprites but don't run any cycles
						if (soam_index_aux > 8)
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

								int temp_x = t_oam[s].oam_x;
								if ((ppur.status.sl != 0) && (ppur.status.sl != 240))
								{
									for (int i = 0; (temp_x + i) < 256 && i < 8; i++)
									{
										if (sl_sprites[256 + temp_x + i] == 0)
										{
											if (t_oam[s].patterns_0.Bit(i) || t_oam[s].patterns_1.Bit(i))
											{
												int spixel = t_oam[s].patterns_0.Bit(i) ? 1 : 0;
												spixel |= (t_oam[s].patterns_1.Bit(i) ? 2 : 0);

												sl_sprites[temp_x + i] = (byte)s;
												sl_sprites[256 + temp_x + i] = (byte)spixel;
												sl_sprites[512 + temp_x + i] = t_oam[s].oam_attr;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (ppur.status.cycle < 336)
				{
					if (ppur.status.cycle == 320)
					{
						ppuphase = PPU_PHASE_BG;
						xt = 0;
						xp = 0;
					}

					// if scanline is the pre-render line, we just read BG data
					Read_bgdata(xp, xt);

					runppu();

					if (xp == 6 && PPUON)
					{
						ppu_was_on = true;
					}

					if (xp == 7 && PPUON)
					{
						if (!race_2006)
							ppur.increment_hsc();

						if (ppur.status.cycle == 256 && !race_2006)
							ppur.increment_vs();

						ppu_was_on = false;
					}

					xp++;

					if (xp == 8)
					{
						xp = 0;
						xt++;
					}
				}
				else if (ppur.status.cycle < 340)
				{
					if (ppur.status.cycle == 339)
					{
						evenOddDestiny = PPUON;
					}

					runppu();
				}
				else
				{
					// After memory access 170, the PPU simply rests for 4 cycles (or the
					// equivelant of half a memory access cycle) before repeating the whole
					// pixel/scanline rendering process. If the scanline being rendered is the very
					// first one on every second frame, then this delay simply doesn't exist.
					if (ppur.status.sl == 0 && idleSynch && evenOddDestiny && chopdot)
					{ ppur.status.cycle++; } // increment cycle without running ppu
					else
					{ runppu(); }
				}
			}

			if (ppur.status.cycle == 341)
			{
				ppur.status.cycle = 0;
				ppur.status.sl++;

				if (ppur.status.sl == 241)
				{
					do_active_sl = false;
					do_pre_vbl = true;
				}
			}
		}

		public void TickPPU_preVBL()
		{
			if ((ppur.status.cycle == 340) && (ppur.status.sl == 241 + preNMIlines - 1))
			{
				Reg2002_vblank_active_pending = true;
			}

			runppu();

			if (ppur.status.cycle == 341)
			{
				ppur.status.cycle = 0;
				ppur.status.sl++;

				if (ppur.status.sl == 241 + preNMIlines)
				{
					if (Reg2002_vblank_active_pending)
					{
						Reg2002_vblank_active = 1;
						Reg2002_vblank_active_pending = false;
					}

					do_pre_vbl = false;
					do_vbl = true;

					ppu_init_frame();
					nes.frame_is_done = true;
				}				
			}
		}

		//not quite emulating all the NES power up behavior
		//since it is known that the NES ignores writes to some
		//register before around a full frame, but no games
		//should write to those regs during that time, it needs
		//to wait for vblank
		public void NewDeadPPU()
		{
			if (ppur.status.cycle == 241 * 341 - start_up_offset - 1)
			{
				Reg2002_vblank_active_pending = true;
			}

			runppu();

			if (ppur.status.cycle == 241 * 341 - start_up_offset)
			{
				if (Reg2002_vblank_active_pending)
				{
					Reg2002_vblank_active = 1;
					Reg2002_vblank_active_pending = false;
				}

				ppudead--;

				ppu_init_frame();

				do_vbl = true;

				nes.frame_is_done = true;
			}
		}
	}
}
