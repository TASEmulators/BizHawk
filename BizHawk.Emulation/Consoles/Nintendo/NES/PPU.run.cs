//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb

//todo - read http://wiki.nesdev.com/w/index.php/PPU_sprite_priority

//TODO - correctly emulate PPU OFF state

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;


namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		partial class PPU
		{
			const int kFetchTime = 2;

			struct BGDataRecord {
				public byte nt, at;
				public byte pt_0, pt_1;
			};

			public short[] xbuf = new short[256*240];

			int ppu_addr_temp;
			void Read_bgdata(ref BGDataRecord bgdata)
			{
				for (int i = 0; i < 8; i++)
					Read_bgdata(i,ref bgdata);
			}
			void Read_bgdata(int cycle, ref BGDataRecord bgdata)
			{
				switch (cycle)
				{
					case 0:
						ppu_addr_temp = ppur.get_ntread();
						bgdata.nt = ppubus_read(ppu_addr_temp, true);
						runppu(1);
						break;
					case 1:
						runppu(1);
						break;
					case 2:
						{
							ppu_addr_temp = ppur.get_atread();
							byte at = ppubus_read(ppu_addr_temp, true);

							//modify at to get appropriate palette shift
							if ((ppur.vt & 2) != 0) at >>= 4;
							if ((ppur.ht & 2) != 0) at >>= 2;
							at &= 0x03;
							at <<= 2;
							bgdata.at = at;

							//horizontal scroll clocked at cycle 3 and then
							//vertical scroll at 251
							runppu(1);
							if (reg_2001.PPUON)
							{
								ppur.increment_hsc();
								if (ppur.status.cycle == 251)
									ppur.increment_vs();
							}
							break;
						}
					case 3:
						runppu(1);
						break;
					case 4:
						ppu_addr_temp = ppur.get_ptread(bgdata.nt);
						bgdata.pt_0 = ppubus_read(ppu_addr_temp, true);
						runppu(1);
						break;
					case 5:
						runppu(1);
						break;
					case 6:
						ppu_addr_temp |= 8;
						bgdata.pt_1 = ppubus_read(ppu_addr_temp, true);
						runppu(1);
						break;
					case 7:
						runppu(1);
						break;
				} //switch(cycle)
			}

			unsafe struct TempOAM
			{
				public fixed byte oam[4];
				public fixed byte patterns[2];
				public byte index;
				public byte present;
			}

			//TODO - check flashing sirens in werewolf
			short PaletteAdjustPixel(int pixel)
			{
				//tack on the deemph bits. THESE MAY BE ORDERED WRONG. PLEASE CHECK IN THE PALETTE CODE
				return (short)(pixel | reg_2001.intensity_lsl_6);
			}

			const int kLineTime = 341;
			public unsafe void FrameAdvance()
			{
				BGDataRecord *bgdata = stackalloc BGDataRecord[34]; //one at the end is junk, it can never be rendered

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
				reg_2003 = 0;

				//this was repeatedly finetuned from the fceux days thrugh the old cpu core and into the new one to pass 05-nmi_timing.nes
				//note that there is still some leniency. for instance, 4,2 will pass in addition to 3,3
				const int delay = 6;
				runppu(3);
				bool nmi_destiny = reg_2000.vblank_nmi_gen && Reg2002_vblank_active;
				runppu(3);
				if (nmi_destiny) TriggerNMI();
				runppu(postNMIlines * kLineTime - delay);

				//this seems to run just before the dummy scanline begins
				clear_2002();

				TempOAM* oams = stackalloc TempOAM[128];
				int* oamcounts = stackalloc int[2];
				int oamslot=0;
				int oamcount=0;

				idleSynch ^= true;

				//render 241 scanlines (including 1 dummy at beginning)
				for (int sl = 0; sl < 241; sl++)
				{
					ppur.status.cycle = 0;

					ppur.status.sl = sl;

					int yp = sl - 1;
					ppuphase = PPUPHASE.BG;

					if (NTViewCallback != null && yp == NTViewCallback.Scanline) NTViewCallback.Callback();
					if (PPUViewCallback != null && yp == PPUViewCallback.Scanline) PPUViewCallback.Callback();

					//twiddle the oam buffers
					int scanslot = oamslot ^ 1;
					int renderslot = oamslot;
					oamslot ^= 1;

					oamcount = oamcounts[renderslot];

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
							oamcount = oamcounts[renderslot];
							int target = yp_shift + xstart;
							int rasterpos = xstart;

							//check all the conditions that can cause things to render in these 8px
							bool renderspritenow = reg_2001.show_obj && (xt > 0 || reg_2001.show_obj_leftmost);
							bool renderbgnow = reg_2001.show_bg && (xt > 0 || reg_2001.show_bg_leftmost);

							for (int xp = 0; xp < 8; xp++, rasterpos++)
							{
								//process the current clock's worth of bg data fetching
								//this needs to be split into 8 pieces or else exact sprite 0 hitting wont work due to the cpu not running while the sprite renders below
								Read_bgdata(xp, ref bgdata[xt + 2]);

								//bg pos is different from raster pos due to its offsetability.
								//so adjust for that here
								int bgpos = rasterpos + ppur.fh;
								int bgpx = bgpos & 7;
								int bgtile = bgpos >> 3;

								int pixel = 0, pixelcolor;

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
								else
								{
									if (!renderspritenow)
									{
										//according to qeed's doc, use palette 0 or $2006's value if it is & 0x3Fxx
										int addr = ppur.get_2007access();
										if ((addr & 0x3F00) == 0x3F00)
										{
											pixel = addr & 0x1F;
										}
									}
									pixelcolor = PALRAM[pixel];
									pixelcolor |= 0x8000; //whats this? i think its a flag to indicate a hidden background to be used by the canvas filling logic later
								}

								if (!nes.CoreInputComm.NES_ShowBG)
									pixelcolor = 0x8000; //whats this? i think its a flag to indicate a hidden background to be used by the canvas filling logic later

								//look for a sprite to be drawn
								bool havepixel = false;
								int renderslot_shift = renderslot << 6;
								for (int s = 0; s < oamcount; s++)
								{
									TempOAM* oam = &oams[renderslot_shift + s];
									int x = oam->oam[3];
									if (rasterpos >= x && rasterpos < x + 8)
									{
										//build the pixel.
										//fetch the LSB of the patterns
										int spixel = oam->patterns[0] & 1;
										spixel |= (oam->patterns[1] & 1) << 1;

										//shift down the patterns so the next pixel is in the LSB
										oam->patterns[0] >>= 1;
										oam->patterns[1] >>= 1;

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
										Reg2002_objhit |= (oam->index == 0 && pixel != 0 && rasterpos < 255);
										//priority handling, if in front of BG:
										bool drawsprite = !(((oam->oam[2] & 0x20) != 0) && ((pixel & 3) != 0));
										if (drawsprite && nes.CoreInputComm.NES_ShowOBJ)
										{
											//bring in the palette bits and palettize
											spixel |= (oam->oam[2] & 3) << 2;
											//save it for use in the framebuffer
											pixelcolor = PALRAM[0x10 + spixel];
										}
									} //rasterpos in sprite range
								} //oamcount loop
								if (reg_2001.color_disable)
									pixelcolor &= 0x30;

								xbuf[target] = PaletteAdjustPixel(pixelcolor);

								target++;
							} //loop across 8 pixels
						} //loop across 32 tiles
					}
					else
						for (int xt = 0; xt < 32; xt++)
							Read_bgdata(ref bgdata[xt + 2]);

					//look for sprites (was supposed to run concurrent with bg rendering)
					oamcounts[scanslot] = 0;
					oamcount = 0;
					int spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

					int scanslot_lshift = scanslot << 6;

					for (int i = 0; i < 64; i++)
					{
						oams[scanslot_lshift + i].present = 0;
						int spr = i * 4;
						if (yp >= OAM[spr] && yp < OAM[spr] + spriteHeight)
						{
							//if we already have maxsprites, then this new one causes an overflow,
							//set the flag and bail out.
							//should we set this flag anyway??
							if (oamcount >= 8 && reg_2001.PPUON)
							{
								Reg2002_objoverflow = true;
								if(!nes.CoreInputComm.NES_UnlimitedSprites)
									break;
							}
							//just copy some bytes into the internal sprite buffer
							TempOAM* oam = &oams[scanslot_lshift + oamcount];
							for (int j = 0; j < 4; j++)
								oam->oam[j] = OAM[spr + j];
							oam->present = 1;
							//note that we stuff the oam index into [6].
							//i need to turn this into a struct so we can have fewer magic numbers
							oams[scanslot_lshift + oamcount].index = (byte)i;
							oamcount++;
						}
					}
					oamcounts[scanslot] = oamcount;

					ppuphase = PPUPHASE.OBJ;

					//fetch sprite patterns
					int oam_todo = oamcount;
					if (oam_todo < 8)
						oam_todo = 8;
					for (int s = 0; s < oam_todo; s++)
					{
						//if this is a real sprite sprite, then it is not above the 8 sprite limit.
						//this is how we support the no 8 sprite limit feature.
						//not that at some point we may need a virtual CALL_PPUREAD which just peeks and doesnt increment any counters
						//this could be handy for the debugging tools also
						bool realSprite = (s < 8);
						bool junksprite = (s >= oamcount || !reg_2001.PPUON);

						TempOAM* oam = &oams[scanslot_lshift + s];
						int line = yp - oam->oam[0];
						if ((oam->oam[2] & 0x80) != 0) //vflip
							line = spriteHeight - line - 1;

						int patternNumber = oam->oam[1];
						int patternAddress;

						//create deterministic dummy fetch pattern.
						if (oam->present == 0)
						{
							//according to nintendulator:
							//* On the first empty sprite slot, read the Y-coordinate of sprite #63 followed by $FF for the remaining 7 cycles
							//* On all subsequent empty sprite slots, read $FF for all 8 reads
							//well, we shall just read $FF and that is good enough for now to make mmc3 work
							patternNumber = 0xFF;
							line = 0;
						}

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
						ppubus_read(ppur.get_ntread(), true);
						if (reg_2001.PPUON)
						{
							if (sl == 0 && ppur.status.cycle == 304)
							{
								runppu(1);
								if (reg_2001.PPUON) ppur.install_latches();
								runppu(1);
								garbage_todo = 0;
							}
							if ((sl != 0) && ppur.status.cycle == 256)
							{
								runppu(1);
								//at 257: 3d world runner is ugly if we do this at 256
								if (reg_2001.PPUON) ppur.install_h_latches();
								runppu(1);
								garbage_todo = 0;
							}
						}
						if (realSprite) runppu(garbage_todo);

						ppubus_read(ppur.get_atread(), true); //at or nt?
						if (realSprite) runppu(kFetchTime);

						//TODO - fake sprites should not come through ppubus_read but rather peek it
						//(at least, they should not probe it with AddressPPU. maybe the difference between peek and read is not necessary)

						if (junksprite)
						{
							if (realSprite)
							{
								ppubus_read(patternAddress, true);
								ppubus_read(patternAddress, true);
								runppu(kFetchTime * 2);
							}
						}
						else
						{
							int addr = patternAddress;
							oam->patterns[0] = ppubus_read(addr, true);
							if (realSprite) runppu(kFetchTime);

							addr += 8;
							oam->patterns[1] = ppubus_read(addr, true);
							if (realSprite) runppu(kFetchTime);

							//hflip
							if ((oam->oam[2] & 0x40) == 0)
							{
								oam->patterns[0] = BITREV.byte_8[oam->patterns[0]];
								oam->patterns[1] = BITREV.byte_8[oam->patterns[1]];
							}
						}
					} //sprite pattern fetch loop

					ppuphase = PPUPHASE.BG;

					//fetch BG: two tiles for next line
					for (int xt = 0; xt < 2; xt++)
						Read_bgdata(ref bgdata[xt]);

					//this sequence is tuned to pass 10-even_odd_timing.nes
					runppu(kFetchTime);
					bool evenOddDestiny = reg_2001.show_bg;
					runppu(kFetchTime);

					//After memory access 170, the PPU simply rests for 4 cycles (or the
					//equivelant of half a memory access cycle) before repeating the whole
					//pixel/scanline rendering process. If the scanline being rendered is the very
					//first one on every second frame, then this delay simply doesn't exist.
					if (sl == 0 && idleSynch && evenOddDestiny && chopdot)
					{ }
					else
						runppu(1);
				} //scanline loop

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
				ppur.status.sl = 241;
				runppu(postNMIlines * kLineTime);
				ppur.status.sl = 0;
				runppu(241 * kLineTime);
				runppu(preNMIlines * kLineTime);
				--ppudead;
			}
		}
	}
}

