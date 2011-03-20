//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb

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

			void Read_bgdata(ref BGDataRecord bgdata) {
				int addr = ppur.get_ntread();
				bgdata.nt = ppubus_read(addr);
				runppu(kFetchTime);

				addr = ppur.get_atread();
				byte at = ppubus_read(addr);

				//modify at to get appropriate palette shift
				if((ppur.vt&2)!=0) at >>= 4;
				if((ppur.ht&2)!=0) at >>= 2;
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
				runppu(1);

				addr = ppur.get_ptread(bgdata.nt);
				bgdata.pt_0 = ppubus_read(addr);
				runppu(kFetchTime);
				addr |= 8;
				bgdata.pt_1 = ppubus_read(addr);
				runppu(kFetchTime);
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
				return (short)(pixel| reg_2001.intensity_lsl_8);
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

				Reg2002_vblank_active = 1;
				ppuphase = PPUPHASE.VBL;

				//Not sure if this is correct.  According to Matt Conte and my own tests, it is.
				//Timing is probably off, though.
				//NOTE:  Not having this here breaks a Super Donkey Kong game.
				reg_2003 = 0;
				const int delay = 20; //fceu used 12 here but I couldnt get it to work in marble madness and pirates.

				runppu(delay); //X6502_Run(12);
				if (reg_2000.vblank_nmi_gen) TriggerNMI();
				if (PAL)
					runppu(70 * (kLineTime) - delay);
				else
					runppu(20 * (kLineTime) - delay);

				//this seems to run just before the dummy scanline begins
				clear_2002();
				//this early out caused metroid to fail to boot. I am leaving it here as a reminder of what not to do
				//if(!PPUON) { runppu(kLineTime*242); goto finish; }

				//There are 2 conditions that update all 5 PPU scroll counters with the
				//contents of the latches adjacent to them. The first is after a write to
				//2006/2. The second, is at the beginning of scanline 20, when the PPU starts
				//rendering data for the first time in a frame (this update won't happen if
				//all rendering is disabled via 2001.3 and 2001.4).

				//if(PPUON)
				//	ppur.install_latches();

				TempOAM* oams = stackalloc TempOAM[128];
				int* oamcounts = stackalloc int[2];
				int oamslot=0;
				int oamcount=0;

				//capture the initial xscroll
				//int xscroll = ppur.fh;
				//render 241 scanlines (including 1 dummy at beginning)
				for (int sl = 0; sl < 241; sl++)
				{
					ppur.status.sl = sl;

					int yp = sl - 1;
					ppuphase = PPUPHASE.BG;

					//twiddle the oam buffers
					int scanslot = oamslot ^ 1;
					int renderslot = oamslot;
					oamslot ^= 1;

					oamcount = oamcounts[renderslot];

					//the main scanline rendering loop:
					//32 times, we will fetch a tile and then render 8 pixels.
					//two of those tiles were read in the last scanline.
					for (int xt = 0; xt < 32; xt++)
					{
						Read_bgdata(ref bgdata[xt + 2]);

						//ok, we're also going to draw here.
						//unless we're on the first dummy scanline
						if (sl != 0)
						{
							int xstart = xt << 3;
							oamcount = oamcounts[renderslot];
							int target = (yp << 8) + xstart;
							int rasterpos = xstart;

							//check all the conditions that can cause things to render in these 8px
							bool renderspritenow = reg_2001.show_obj && (xt > 0 || reg_2001.show_obj_leftmost);
							bool renderbgnow = reg_2001.show_bg && (xt > 0 || reg_2001.show_bg_leftmost);

							for (int xp = 0; xp < 8; xp++, rasterpos++)
							{

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
									pixel = ((pt_0 >> (7 - bgpx)) & 1) | (((pt_1 >> (7 - bgpx)) & 1) << 1);
									if(pixel != 0)
										 pixel |= bgdata[bgtile].at;
								}
								pixelcolor = PALRAM[pixel];

								//look for a sprite to be drawn
								bool havepixel = false;
								for (int s = 0; s < oamcount; s++)
								{
									TempOAM *oam = &oams[(renderslot<<6)+s];
									{
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

											if (!renderspritenow) continue;

											//bail out if we already have a pixel from a higher priority sprite
											if (havepixel) continue;

											//transparent pixel bailout
											if (spixel == 0) continue;

											//spritehit:
											//1. is it sprite#0?
											//2. is the bg pixel nonzero?
											//then, it is spritehit.
											if (oam->index == 0 && (pixel & 3) != 0 && rasterpos < 255)
											{
												Reg2002_objhit = true;
											}
											havepixel = true;


											//priority handling
											if ((oam->oam[2] & 0x20) != 0)
											{
												//behind background:
												if ((pixel & 3) != 0) continue;
											}

											//bring in the palette bits and palettize
											spixel |= (oam->oam[2] & 3) << 2;
											pixelcolor = PALRAM[0x10 + spixel];
										} //rasterpos in sprite range

									} //c# fixed oam ptr


								}//oamcount loop

								xbuf[target] = PaletteAdjustPixel(pixelcolor);
								target++;

							} //loop across 8 pixels
						} //scanline != 0
					} //loop across 32 tiles


					//look for sprites (was supposed to run concurrent with bg rendering)
					oamcounts[scanslot] = 0;
					oamcount = 0;
					int spriteHeight = reg_2000.obj_size_16 ? 16 : 8;

					for (int i = 0; i < 64; i++)
						oams[(scanslot<<6)+i].present = 0;

					for (int i = 0; i < 64; i++)
					{
						int spr = i * 4;
						{
							if (yp >= OAM[spr] && yp < OAM[spr] + spriteHeight)
							{
								//if we already have maxsprites, then this new one causes an overflow,
								//set the flag and bail out.
								if (oamcount >= 8 && reg_2001.PPUON)
								{
									Reg2002_objoverflow = true;
									if (SPRITELIMIT)
										break;
								}

								//just copy some bytes into the internal sprite buffer
								TempOAM* oam = &oams[(scanslot << 6) + oamcount];
								{
									for (int j = 0; j < 4; j++)
										oam->oam[j] = OAM[spr + j];
									oam->present = 1;
								}

								//note that we stuff the oam index into [6].
								//i need to turn this into a struct so we can have fewer magic numbers
								oams[(scanslot<<6)+oamcount].index = (byte)i;
								oamcount++;
							}
						}
					}
					oamcounts[scanslot] = oamcount;

					//FV is clocked by the PPU's horizontal blanking impulse, and therefore will increment every scanline.
					//well, according to (which?) tests, maybe at the end of hblank.
					//but, according to what it took to get crystalis working, it is at the beginning of hblank.

					//this is done at cycle 251
					//rendering scanline, it doesn't need to be scanline 0,
					//because on the first scanline when the increment is 0, the vs_scroll is reloaded.
					//if(PPUON && sl != 0)
					//	ppur.increment_vs();

					//todo - think about clearing oams to a predefined value to force deterministic behavior

					//so.. this is the end of hblank. latch horizontal scroll values
					//do it cycle at 251
					if (reg_2001.PPUON && sl != 0)
						ppur.install_h_latches();

					ppuphase = PPUPHASE.OBJ;

					//fetch sprite patterns
					for (int s = 0; s < MAXSPRITES; s++)
					{
						//if we have hit our eight sprite pattern and we dont have any more sprites, then bail
						if (s == oamcount && s >= 8)
							break;

						//if this is a real sprite sprite, then it is not above the 8 sprite limit.
						//this is how we support the no 8 sprite limit feature.
						//not that at some point we may need a virtual CALL_PPUREAD which just peeks and doesnt increment any counters
						//this could be handy for the debugging tools also
						bool realSprite = (s < 8);

						TempOAM* oam = &oams[(scanslot << 6) + s];
						//fixed (TempOAM* oam = &oams[scanslot, s])
						{
							int line = yp - oam->oam[0];
							if ((oam->oam[2] & 0x80) != 0) //vflip
								line = spriteHeight - line - 1;

							int patternNumber = oam->oam[1];
							int patternAddress;

							//create deterministic dummy fetch pattern
							if (oam->present==0)
							{
								patternNumber = 0;
								line = 0;
							}

							//8x16 sprite handling:
							if (reg_2000.obj_size_16)
							{
								int bank = (patternNumber & 1) << 12;
								patternNumber = patternNumber & ~1;
								patternNumber |= (line >> 3)&1;
								patternAddress = (patternNumber << 4) | bank;
							}
							else
							{
								patternAddress = (patternNumber << 4) | (reg_2000.obj_pattern_hi << 12);
							}

							//offset into the pattern for the current line.
							//tricky: tall sprites have already had lines>8 taken care of by getting a new pattern number above.
							//so we just need the line offset for the second pattern
							patternAddress += line & 7;

							//garbage nametable fetches
							//reset the scroll counter, happens at cycle 304
							//TODO - compact this logic
							if (realSprite)
							{
								if ((sl == 0) && reg_2001.PPUON)
								{
									if (ppur.status.cycle == 304)
									{
										runppu(1);
										ppur.install_latches();
										runppu(1);
									}
									else
										runppu(kFetchTime);
								}
								else
									runppu(kFetchTime);
							}

							//..etc.. hacks about dragon's lair, MMC3, crystalis and SMB3. this should be implemented through the board

							if (realSprite) runppu(kFetchTime);

							//pattern table fetches
							int addr = patternAddress;
							oam->patterns[0] = ppubus_read(addr);
							if (realSprite) runppu(kFetchTime);

							addr += 8;
							oam->patterns[1] = ppubus_read(addr);
							if (realSprite) runppu(kFetchTime);

							//hflip
							if ((oam->oam[2] & 0x40) == 0)
							{
								oam->patterns[0] = BITREV.byte_8[oam->patterns[0]];
								oam->patterns[1] = BITREV.byte_8[oam->patterns[1]];
							}
						} //c# fixed oam
					
					} //sprite pattern fetch loop

					ppuphase = PPUPHASE.BG;

					//fetch BG: two tiles for next line
					for (int xt = 0; xt < 2; xt++)
						Read_bgdata(ref bgdata[xt]);

					//I'm unclear of the reason why this particular access to memory is made.
					//The nametable address that is accessed 2 times in a row here, is also the
					//same nametable address that points to the 3rd tile to be rendered on the
					//screen (or basically, the first nametable address that will be accessed when
					//the PPU is fetching background data on the next scanline).
					//(not implemented yet)
					runppu(kFetchTime*2);
					if (sl == 0)
					{
						if (idleSynch && reg_2001.PPUON && !PAL)
							ppur.status.end_cycle = 340;
						else
							ppur.status.end_cycle = 341;
						idleSynch ^= true;
					}
					else
						ppur.status.end_cycle = 341;
					//runppu(kFetchTime);

					//After memory access 170, the PPU simply rests for 4 cycles (or the
					//equivelant of half a memory access cycle) before repeating the whole
					//pixel/scanline rendering process. If the scanline being rendered is the very
					//first one on every second frame, then this delay simply doesn't exist.
					if (ppur.status.end_cycle == 341)
						runppu(1);
				
				} //scanline loop

				//hacks...
				//if (MMC5Hack && PPUON) MMC5_hb(240);

				//idle for one line
				runppu(kLineTime);

			} //FrameAdvance


			void FrameAdvance_ppudead()
			{
				//not quite emulating all the NES power up behavior
				//since it is known that the NES ignores writes to some
				//register before around a full frame, but no games
				//should write to those regs during that time, it needs
				//to wait for vblank
				ppur.status.sl = 241;
				if (PAL)
					runppu(70 * kLineTime);
				else
					runppu(20 * kLineTime);
				ppur.status.sl = 0;
				runppu(242 * kLineTime);
				--ppudead;
			}
		}
	}
}

