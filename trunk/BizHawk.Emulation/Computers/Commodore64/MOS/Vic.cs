using System;
using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Vic
	{
		// ------------------------------------

		private class Sprite
		{
			public bool collideData;
			public bool collideSprite;
			public int color;
			public bool display;
			public bool dma;
			public bool enable;
			public int mc;
			public int mcbase;
			public bool multicolor;
			public bool multicolorCrunch;
			public int pointer;
			public bool priority;
			public bool shiftEnable;
			public int sr;
			public int x;
			public bool xCrunch;
			public bool xExpand;
			public int y;
			public bool yCrunch;
			public bool yExpand;

			public void HardReset()
			{
				collideData = false;
				collideSprite = false;
				color = 0;
				display = false;
				dma = false;
				enable = false;
				mc = 0;
				mcbase = 0;
				multicolor = false;
				multicolorCrunch = false;
				pointer = 0;
				priority = false;
				shiftEnable = false;
				sr = 0;
				x = 0;
				xCrunch = false;
				xExpand = false;
				y = 0;
				yCrunch = false;
				yExpand = false;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("collideData", ref collideData);
				ser.Sync("collideSprite", ref collideSprite);
				ser.Sync("color", ref color);
				ser.Sync("display", ref display);
				ser.Sync("dma", ref dma);
				ser.Sync("enable", ref enable);
				ser.Sync("mc", ref mc);
				ser.Sync("mcbase", ref mcbase);
				ser.Sync("multicolor", ref multicolor);
				ser.Sync("multicolorCrunch", ref multicolorCrunch);
				ser.Sync("pointer", ref pointer);
				ser.Sync("priority", ref priority);
				ser.Sync("shiftEnable", ref shiftEnable);
				ser.Sync("sr", ref sr);
				ser.Sync("x", ref x);
				ser.Sync("xCrunch", ref xCrunch);
				ser.Sync("xExpand", ref xExpand);
				ser.Sync("y", ref y);
				ser.Sync("yCrunch", ref yCrunch);
				ser.Sync("yExpand", ref yExpand);
			}
		}
		private Sprite[] sprites;

		private int backgroundColor0;
		private int backgroundColor1;
		private int backgroundColor2;
		private int backgroundColor3;
		private int baCount;
		private bool badline;
		private bool badlineEnable;
		private int bitmapColumn;
		private bool bitmapMode;
		private int borderB;
		private bool borderCheckLEnable;
		private bool borderCheckREnable;
		private int borderColor;
		private int borderL;
		private bool borderOnMain;
		private bool borderOnVertical;
		private int borderR;
		private int borderT;
		private int[] bufferC;
		private int[] bufferG;
		private bool columnSelect;
		private int cycle;
		private int cycleIndex;
		private int dataC;
		private int dataG;
		private int displayC;
		private bool displayEnable;
		private int displayIndex;
		private bool enableIntLightPen;
		private bool enableIntRaster;
		private bool enableIntSpriteCollision;
		private bool enableIntSpriteDataCollision;
		private bool extraColorMode;
		private bool idle;
		private bool intLightPen;
		private bool intRaster;
		private bool intSpriteCollision;
		private bool intSpriteDataCollision;
		private int lastRasterLine;
		private int lightPenX;
		private int lightPenY;
		private bool multicolorMode;
		private int[] pixelBackgroundBuffer;
		private int pixelBackgroundBufferDelay;
		private int pixelBackgroundBufferIndex;
		private int[] pixelBuffer;
		private int pixelBufferDelay;
		private int pixelBufferIndex;
		private int[] pixelDataBuffer;
		private int pointerCB;
		private int pointerVM;
		private int rasterInterruptLine;
		private int rasterLine;
		private int rasterX;
		private int rc;
		private int refreshCounter;
		private bool rowSelect;
		private int spriteMulticolor0;
		private int spriteMulticolor1;
		private int sr;
		private int vc;
		private int vcbase;
		private int vmli;
		private int xOffset;
		private int xScroll;
		private int yScroll;

		// ------------------------------------

		private int cyclesPerSec;
		private bool pinAEC = true;
        private bool pinBA = true;
        private bool pinIRQ = true;
		private int[][] pipeline;
		private int totalCycles;
		private int totalLines;

		// ------------------------------------

		public Func<ushort, byte> ReadColorRam;
		public Func<ushort, byte> ReadMemory;
	
		// ------------------------------------

		public Vic(int newCycles, int newLines, int[][] newPipeline, int newCyclesPerSec)
		{
			
			{
				totalCycles = newCycles;
				totalLines = newLines;
				pipeline = newPipeline;
				cyclesPerSec = newCyclesPerSec;
				pixelBufferDelay = 12;
				pixelBackgroundBufferDelay = 4;
				bufRect = new Rectangle(136 - 24, 51 - 24, 320 + 48, 200 + 48);

				buf = new int[bufRect.Width * bufRect.Height];
				bufLength = buf.Length;
				bufWidth = (totalCycles * 8);
				bufHeight = (totalLines);

				sprites = new Sprite[8];
				for (int i = 0; i < 8; i++)
					sprites[i] = new Sprite();

				bufferC = new int[40];
				bufferG = new int[40];
				pixelBuffer = new int[pixelBufferDelay];
				pixelDataBuffer = new int[pixelBufferDelay];
				pixelBackgroundBuffer = new int[pixelBackgroundBufferDelay];
			}
		}

		public void HardReset()
		{
			pinAEC = true;
			pinBA = true;
			pinIRQ = true;

			bufOffset = 0;

			backgroundColor0 = 0;
			backgroundColor1 = 0;
			backgroundColor2 = 0;
			backgroundColor3 = 0;
			baCount = baResetCounter;
			badline = false;
			badlineEnable = false;
			bitmapMode = false;
			borderCheckLEnable = false;
			borderCheckREnable = false;
			borderColor = 0;
			borderOnMain = true;
			borderOnVertical = true;
			columnSelect = false;
			displayEnable = false;
			displayIndex = 0;
			enableIntLightPen = false;
			enableIntRaster = false;
			enableIntSpriteCollision = false;
			enableIntSpriteDataCollision = false;
			extraColorMode = false;
			idle = true;
			intLightPen = false;
			intRaster = false;
			intSpriteCollision = false;
			intSpriteDataCollision = false;
			lastRasterLine = 0;
			lightPenX = 0;
			lightPenY = 0;
			multicolorMode = false;
			pixelBufferIndex = 0;
			pixelBackgroundBufferIndex = 0;
			pointerCB = 0;
			pointerVM = 0;
			rasterInterruptLine = 0;
			rasterLine = 0;
			rasterX = 0;
			rc = 7;
			refreshCounter = 0xFF;
			rowSelect = false;
			spriteMulticolor0 = 0;
			spriteMulticolor1 = 0;
			sr = 0;
			vc = 0;
			vcbase = 0;
			vmli = 0;
			xOffset = 0;
			xScroll = 0;
			yScroll = 0;

			// reset sprites
			for (int i = 0; i < 8; i++)
				sprites[i].HardReset();

			// clear C buffer
			for (int i = 0; i < 40; i++)
			{
				bufferC[i] = 0;
				bufferG[i] = 0;
			}

			// clear pixel buffer
			for (int i = 0; i < pixelBufferDelay; i++)
			{
				pixelBuffer[i] = 0;
				pixelDataBuffer[i] = 0;
			}
			for (int i = 0; i < pixelBackgroundBufferDelay; i++)
				pixelBackgroundBuffer[i] = 0;

			UpdateBorder();
		}

		private void UpdateBA()
		{
			
			{
				if (pinBA)
					baCount = baResetCounter;
				else if (baCount > 0)
					baCount--;
			}
		}

		private void UpdateBorder()
		{
			
			{
				borderL = columnSelect ? 0x018 : 0x01F;
				borderR = columnSelect ? 0x158 : 0x14F;
				borderT = rowSelect ? 0x033 : 0x037;
				borderB = rowSelect ? 0x0FB : 0x0F7;
			}
		}

		private void UpdatePins()
		{
			
			{
				pinIRQ = !(
					(enableIntRaster & intRaster) |
					(enableIntSpriteDataCollision & intSpriteDataCollision) |
					(enableIntSpriteCollision & intSpriteCollision) |
					(enableIntLightPen & intLightPen));
			}
		}

		// ------------------------------------

		protected const int pipelineUpdateVc = 1;
		protected const int pipelineChkSprChunch = 2;
		protected const int pipelineUpdateMcBase = 4;
		protected const int pipelineChkBrdL1 = 8;
		protected const int pipelineChkBrdL0 = 16;
		protected const int pipelineChkSprDma = 32;
		protected const int pipelineChkBrdR0 = 64;
		protected const int pipelineChkSprExp = 128;
		protected const int pipelineChkBrdR1 = 256;
		protected const int pipelineChkSprDisp = 512;
		protected const int pipelineUpdateRc = 1024;

		protected const int rasterIrqLine0Cycle = 1;
		protected const int rasterIrqLineXCycle = 0;

		protected const int baResetCounter = 7;

		// ------------------------------------

		public void ExecutePhase1()
		{
			
			{
				// raster IRQ compare
				if ((cycle == rasterIrqLineXCycle && rasterLine > 0) || (cycle == rasterIrqLine0Cycle && rasterLine == 0))
				{
					if (rasterLine != lastRasterLine)
						if (rasterLine == rasterInterruptLine)
							intRaster = true;
					lastRasterLine = rasterLine;
				}

				// display enable compare
				if (rasterLine == 0x030)
					badlineEnable |= displayEnable;

				// badline compare
				if (badlineEnable && rasterLine >= 0x030 && rasterLine < 0x0F7 && ((rasterLine & 0x7) == yScroll))
					badline = true;
				else
					badline = false;

				// go into display state on a badline
				if (badline)
					idle = false;

				// process some sprite crunch vars
				if (!sprites[0].yExpand) sprites[0].yCrunch = true;
				if (!sprites[1].yExpand) sprites[1].yCrunch = true;
				if (!sprites[2].yExpand) sprites[2].yCrunch = true;
				if (!sprites[3].yExpand) sprites[3].yCrunch = true;
				if (!sprites[4].yExpand) sprites[4].yCrunch = true;
				if (!sprites[5].yExpand) sprites[5].yCrunch = true;
				if (!sprites[6].yExpand) sprites[6].yCrunch = true;
				if (!sprites[7].yExpand) sprites[7].yCrunch = true;

				// set up display index for rendering
				if (cycle == 15)
					displayIndex = 0;
				else if (cycle > 15 && cycle <= 55)
					displayIndex++;

				ParseCycle();

				xOffset = 0;
				Render();

				// if the BA counter is nonzero, allow CPU bus access
				UpdateBA();
                pinAEC = false;

				// must always come last
				UpdatePins();
			}
		}

		public void ExecutePhase2()
		{
			
			{
				ParseCycle();

				// advance cycle and optionally raster line
				cycle++;
				if (cycle == totalCycles)
				{
					if (rasterLine == borderB)
						borderOnVertical = true;
					if (rasterLine == borderT && displayEnable)
						borderOnVertical = false;

					cycleIndex = 0;
					cycle = 0;
					rasterLine++;
					if (rasterLine == totalLines)
					{
						rasterLine = 0;
						vcbase = 0;
						vc = 0;
					}
				}

				Render();
                UpdateBA();
                pinAEC = (baCount > 0);

				// must always come last
				UpdatePins();
			}
		}

		private void ParseCycle()
		{
			
			{
				ushort addr = 0x3FFF;
				int cycleBAsprite0;
				int cycleBAsprite1;
				int cycleBAsprite2;
				int cycleFetchSpriteIndex;
				int fetch = pipeline[1][cycleIndex];
				int ba = pipeline[2][cycleIndex];
				int act = pipeline[3][cycleIndex];

				// apply X location
				rasterX = pipeline[0][cycleIndex];

				// perform fetch
				switch (fetch & 0xFF00)
				{
					case 0x0100:
						// fetch R
						refreshCounter = (refreshCounter - 1) & 0xFF;
						addr = (ushort)(0x3F00 | refreshCounter);
						ReadMemory(addr);
						break;
					case 0x0200:
						// fetch C
						if (!idle)
						{
							if (badline)
							{
								addr = (ushort)((pointerVM << 10) | vc);
								dataC = ReadMemory(addr);
								dataC |= ((int)ReadColorRam(addr) & 0xF) << 8;
								bufferC[vmli] = dataC;
							}
							else
							{
								dataC = bufferC[vmli];
							}
						}
						else
						{
							dataC = 0;
							bufferC[vmli] = dataC;
						}
						break;
					case 0x0300:
						// fetch G
						if (idle)
							addr = 0x3FFF;
						else
						{
							if (bitmapMode)
								addr = (ushort)(rc | (vc << 3) | ((pointerCB & 0x4) << 11));
							else
								addr = (ushort)(rc | ((dataC & 0xFF) << 3) | (pointerCB << 11));
						}
						if (extraColorMode)
							addr &= 0x39FF;
						dataG = ReadMemory(addr);
						if (!idle)
						{
							bufferG[vmli] = dataG;
							vmli = (vmli + 1) & 0x3F;
							vc = (vc + 1) & 0x3FF;
						}
						break;
					case 0x0400:
						// fetch I
						addr = (extraColorMode ? (ushort)0x39FF : (ushort)0x3FFF);
						dataG = ReadMemory(addr);
						dataC = 0;
						break;
					case 0x0500:
						// no fetch
						break;
					default:
						cycleFetchSpriteIndex = (fetch & 0x7);
						switch (fetch & 0xF0)
						{
							case 0x00:
								// fetch P
								addr = (ushort)(0x3F8 | (pointerVM << 10) | cycleFetchSpriteIndex);
								sprites[cycleFetchSpriteIndex].pointer = ReadMemory(addr);
								sprites[cycleFetchSpriteIndex].shiftEnable = false;
								break;
							case 0x10:
							case 0x20:
							case 0x30:
								// fetch S
								if (sprites[cycleFetchSpriteIndex].dma)
								{
									Sprite spr = sprites[cycleFetchSpriteIndex];
									addr = (ushort)(spr.mc | (spr.pointer << 6));
									spr.sr <<= 8;
									spr.sr |= ReadMemory(addr);
									spr.mc++;
								}
								break;
						}
						break;
				}

				// perform BA flag manipulation
				switch (ba)
				{
					case 0x0000:
						pinBA = true;
						break;
					case 0x1000:
						pinBA = !badline;
						break;
					default:
						cycleBAsprite0 = (ba & 0x000F);
						cycleBAsprite1 = (ba & 0x00F0) >> 4;
						cycleBAsprite2 = (ba & 0x0F00) >> 8;
						if ((cycleBAsprite0 < 8 && sprites[cycleBAsprite0].dma) ||
							(cycleBAsprite1 < 8 && sprites[cycleBAsprite1].dma) ||
							(cycleBAsprite2 < 8 && sprites[cycleBAsprite2].dma))
							pinBA = false;
						else
							pinBA = true;
						break;
				}

				// perform actions
				borderCheckLEnable = true;
				borderCheckREnable = true;

				if ((act & pipelineChkSprChunch) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						Sprite spr = sprites[i];
						if (spr.yCrunch)
							spr.mcbase += 2;
						spr.shiftEnable = false;
						spr.xCrunch = !spr.xExpand;
						spr.multicolorCrunch = !spr.multicolor;
					}
				}
				if ((act & pipelineChkSprDisp) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						Sprite spr = sprites[i];
						spr.mc = spr.mcbase;
						if (spr.dma && spr.y == (rasterLine & 0xFF))
						{
							spr.display = true;
						}
					}
				}
				if ((act & pipelineChkSprDma) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						Sprite spr = sprites[i];
						if (spr.enable && spr.y == (rasterLine & 0xFF) && !spr.dma)
						{
							spr.dma = true;
							spr.mcbase = 0;
							spr.yCrunch = !spr.yExpand;
						}
					}
				}
				if ((act & pipelineChkSprExp) != 0)
				{
					if (sprites[0].yExpand) sprites[0].yCrunch ^= true;
					if (sprites[1].yExpand) sprites[1].yCrunch ^= true;
					if (sprites[2].yExpand) sprites[2].yCrunch ^= true;
					if (sprites[3].yExpand) sprites[3].yCrunch ^= true;
					if (sprites[4].yExpand) sprites[4].yCrunch ^= true;
					if (sprites[5].yExpand) sprites[5].yCrunch ^= true;
					if (sprites[6].yExpand) sprites[6].yCrunch ^= true;
					if (sprites[7].yExpand) sprites[7].yCrunch ^= true;
				}
				if ((act & pipelineUpdateMcBase) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						Sprite spr = sprites[i];
						if (spr.yCrunch)
						{
							spr.mcbase++;
							if (spr.mcbase == 63)
							{
								spr.dma = false;
								spr.display = false;
							}
						}
					}
				}
				if ((act & pipelineUpdateRc) != 0)
				{
					if (rc == 7)
					{
						idle = true;
						vcbase = vc;
					}
					if (!idle)
						rc = (rc + 1) & 0x7;
				}
				if ((act & pipelineUpdateVc) != 0)
				{
					vc = vcbase;
					vmli = 0;
					if (badline)
						rc = 0;
				}

				cycleIndex++;
			}
		}

		private void Render()
		{
			
			{
				int pixel;
				int pixelData;
				bool renderEnabled = bufRect.Contains(bufPoint);

				for (int i = 0; i < 4; i++)
				{
					if (borderCheckLEnable && rasterX == borderL)
					{
						if (rasterLine == borderB)
							borderOnVertical = true;
						if (rasterLine == borderT && displayEnable)
							borderOnVertical = false;
						if (!borderOnVertical)
							borderOnMain = false;
					}
					if (borderCheckREnable && rasterX == borderR)
					{
						borderOnMain = true;
					}

					// recall pixel from buffer
					pixel = pixelBuffer[pixelBufferIndex];

					// plot pixel if within viewing area
					if (renderEnabled)
					{
						buf[bufOffset] = palette[pixel];
						bufOffset++;
						if (bufOffset == bufLength)
							bufOffset = 0;
					}
					bufPoint.X++;
					if (bufPoint.X == bufWidth)
					{
						bufPoint.X = 0;
						bufPoint.Y++;
						if (bufPoint.Y == bufHeight)
							bufPoint.Y = 0;
					}

					// put the pixel from the background buffer into the main buffer
					pixel = pixelBackgroundBuffer[pixelBackgroundBufferIndex];

					// render sprite
					int pixelOwner = 8;
					for (int j = 0; j < 8; j++)
					{
						int sprData;
						int sprPixel = pixel;

						Sprite spr = sprites[j];

						if (spr.x == rasterX)
							spr.shiftEnable = true;

						if (spr.shiftEnable)
						{
							if (spr.multicolor)
							{
								sprData = (spr.sr & 0xC00000) >> 22;
								if (spr.multicolorCrunch && spr.xCrunch)
									spr.sr <<= 2;
								spr.multicolorCrunch ^= spr.xCrunch;
							}
							else
							{
								sprData = (spr.sr & 0x800000) >> 22;
								if (spr.xCrunch)
									spr.sr <<= 1;
							}
							spr.xCrunch ^= spr.xExpand;
							switch (sprData)
							{
								case 1: sprPixel = spriteMulticolor0; break;
								case 2: sprPixel = spr.color; break;
								case 3: sprPixel = spriteMulticolor1; break;
							}
							if (sprData != 0)
							{
								// sprite-sprite collision
								if (pixelOwner >= 8)
								{
									if (!spr.priority || (pixelDataBuffer[pixelBackgroundBufferIndex] < 0x2))
										pixel = sprPixel;
									pixelOwner = j;
								}
								else
								{
									if (!borderOnVertical)
									{
										spr.collideSprite = true;
										sprites[pixelOwner].collideSprite = true;
									}
								}

								// sprite-data collision
								if (!borderOnVertical && (pixelDataBuffer[pixelBackgroundBufferIndex] >= 0x2))
								{
									spr.collideData = true;
								}
							}
							if (spr.sr == 0)
								spr.shiftEnable = false; //optimization
						}
					}

					// border doesn't work with the background buffer
					if (borderOnMain || borderOnVertical)
						pixel = borderColor;

					// store pixel in buffer
					pixelBuffer[pixelBufferIndex] = pixel;

					// fill shift register
					if (xOffset == xScroll)
					{
						if (displayIndex < 40 && !idle)
						{
							displayC = bufferC[displayIndex];
							sr |= bufferG[displayIndex];
						}
						bitmapColumn = 0;
					}

					if (!extraColorMode && !bitmapMode & !multicolorMode)
					{
						// 000
						pixelData = (sr & 0x80) >> 6;
						sr <<= 1;
						pixel = (pixelData != 0) ? displayC >> 8 : backgroundColor0;
					}
					else if (!extraColorMode && !bitmapMode & multicolorMode)
					{
						// 001
						if ((displayC & 0x800) != 0)
						{
							// multicolor 001
							pixelData = (sr & 0xC0) >> 6;
							if ((bitmapColumn & 1) != 0)
								sr <<= 2;
							switch (pixelData)
							{
								case 0x00: pixel = backgroundColor0; break;
								case 0x01: pixel = backgroundColor1; break;
								case 0x02: pixel = backgroundColor2; break;
								default: pixel = (displayC & 0x700) >> 8; break;
							}
						}
						else
						{
							// standard 001
							pixelData = (sr & 0x80) >> 6;
							sr <<= 1;
							pixel = (pixelData != 0) ? (displayC >> 8) : backgroundColor0;
						}
					}
					else if (!extraColorMode && bitmapMode & !multicolorMode)
					{
						// 010
						pixelData = (sr & 0x80) >> 6;
						sr <<= 1;
						pixel = (pixelData != 0) ? ((displayC >> 4) & 0xF) : (displayC & 0xF);
					}
					else if (!extraColorMode && bitmapMode & multicolorMode)
					{
						// 011
						pixelData = (sr & 0xC0) >> 6;
						if ((bitmapColumn & 1) != 0)
							sr <<= 2;
						switch (pixelData)
						{
							case 0x00: pixel = backgroundColor0; break;
							case 0x01: pixel = (displayC >> 4) & 0xF; break;
							case 0x02: pixel = displayC & 0xF; break;
							default: pixel = (displayC >> 8) & 0xF; break;
						}
					}
					else if (extraColorMode && !bitmapMode & !multicolorMode)
					{
						// 100
						pixelData = (sr & 0x80) >> 6;
						sr <<= 1;
						if (pixelData != 0)
						{
							pixel = displayC >> 8;
						}
						else
						{
							switch ((displayC >> 6) & 0x3)
							{
								case 0x00: pixel = backgroundColor0; break;
								case 0x01: pixel = backgroundColor1; break;
								case 0x02: pixel = backgroundColor2; break;
								default: pixel = backgroundColor3; break;
							}
						}
					}
					else if (extraColorMode && !bitmapMode & multicolorMode)
					{
						// 101
						pixelData = 0;
						pixel = 0;
					}
					else if (extraColorMode && bitmapMode & !multicolorMode)
					{
						// 110
						pixelData = 0;
						pixel = 0;
					}
					else
					{
						// 111
						pixelData = 0;
						pixel = 0;
					}

					// put the rendered pixel into the background buffer
					pixelDataBuffer[pixelBackgroundBufferIndex] = pixelData;
					pixelBackgroundBuffer[pixelBackgroundBufferIndex] = pixel;
					pixelBackgroundBufferIndex++;
					if (pixelBackgroundBufferIndex == pixelBackgroundBufferDelay)
						pixelBackgroundBufferIndex = 0;

					// advance pixel buffer
					pixelBufferIndex++;
					if (pixelBufferIndex == pixelBufferDelay)
						pixelBufferIndex = 0;

					rasterX++;
					xOffset++;
					bitmapColumn++;
				}
			}
		}

		// ------------------------------------

		public bool AEC { get { return pinAEC; } }
		public bool BA { get { return pinBA; } }
		public bool IRQ { get { return pinIRQ; } }

        public bool ReadAEC() { return pinAEC; }
        public bool ReadBA() { return pinBA; }
        public bool ReadIRQ() { return pinIRQ; }

		// ------------------------------------

		public int CyclesPerFrame
		{
			get
			{
				return (int)(totalCycles * totalLines);
			}
		}

		public int CyclesPerSecond
		{
			get
			{
				return cyclesPerSec;
			}
		}

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((ushort)(addr & 0x3F));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((ushort)(addr & 0x3F), val);
		}

		public byte Read(ushort addr)
		{
			byte result;
			addr &= 0x3F;

			switch (addr)
			{
				case 0x1E:
				case 0x1F:
					// reading clears these
					result = ReadRegister(addr);
					WriteRegister(addr, 0);
					break;
				default:
					result = ReadRegister((ushort)(addr & 0x3F));
					break;
			}
			return result;
		}

		private byte ReadRegister(ushort addr)
		{
			byte result = 0xFF; //unused bit value

			switch (addr)
			{
				case 0x00:
				case 0x02:
				case 0x04:
				case 0x06:
				case 0x08:
				case 0x0A:
				case 0x0C:
				case 0x0E:
					result = (byte)(sprites[addr >> 1].x & 0xFF);
					break;
				case 0x01:
				case 0x03:
				case 0x05:
				case 0x07:
				case 0x09:
				case 0x0B:
				case 0x0D:
				case 0x0F:
					result = (byte)(sprites[addr >> 1].y & 0xFF);
					break;
				case 0x10:
					result = (byte)(
						((sprites[0].x >> 8) & 0x01) |
						((sprites[1].x >> 7) & 0x02) |
						((sprites[2].x >> 6) & 0x04) |
						((sprites[3].x >> 5) & 0x08) |
						((sprites[4].x >> 4) & 0x10) |
						((sprites[5].x >> 3) & 0x20) |
						((sprites[6].x >> 2) & 0x40) |
						((sprites[7].x >> 1) & 0x80)
						);
					break;
				case 0x11:
					result = (byte)(
						(byte)(yScroll & 0x7) |
						(rowSelect ? (byte)0x08 : (byte)0x00) |
						(displayEnable ? (byte)0x10 : (byte)0x00) |
						(bitmapMode ? (byte)0x20 : (byte)0x00) |
						(extraColorMode ? (byte)0x40 : (byte)0x00) |
						(byte)((rasterLine & 0x100) >> 1)
						);
					break;
				case 0x12:
					result = (byte)(rasterLine & 0xFF);
					break;
				case 0x13:
					result = (byte)(lightPenX & 0xFF);
					break;
				case 0x14:
					result = (byte)(lightPenY & 0xFF);
					break;
				case 0x15:
					result = (byte)(
						(sprites[0].enable ? 0x01 : 0x00) |
						(sprites[1].enable ? 0x02 : 0x00) |
						(sprites[2].enable ? 0x04 : 0x00) |
						(sprites[3].enable ? 0x08 : 0x00) |
						(sprites[4].enable ? 0x10 : 0x00) |
						(sprites[5].enable ? 0x20 : 0x00) |
						(sprites[6].enable ? 0x40 : 0x00) |
						(sprites[7].enable ? 0x80 : 0x00)
						);
					break;
				case 0x16:
					result &= 0xC0;
					result |= (byte)(
						(byte)(xScroll & 0x7) |
						(columnSelect ? (byte)0x08 : (byte)0x00) |
						(multicolorMode ? (byte)0x10 : (byte)0x00)
						);
					break;
				case 0x17:
					result = (byte)(
						(sprites[0].yExpand ? 0x01 : 0x00) |
						(sprites[1].yExpand ? 0x02 : 0x00) |
						(sprites[2].yExpand ? 0x04 : 0x00) |
						(sprites[3].yExpand ? 0x08 : 0x00) |
						(sprites[4].yExpand ? 0x10 : 0x00) |
						(sprites[5].yExpand ? 0x20 : 0x00) |
						(sprites[6].yExpand ? 0x40 : 0x00) |
						(sprites[7].yExpand ? 0x80 : 0x00)
						);
					break;
				case 0x18:
					result &= 0x01;
					result |= (byte)(
						((pointerVM & 0xF) << 4) |
						((pointerCB & 0x7) << 1)
						);
					break;
				case 0x19:
					result &= 0x70;
					result |= (byte)(
						(intRaster ? 0x01 : 0x00) |
						(intSpriteDataCollision ? 0x02 : 0x00) |
						(intSpriteCollision ? 0x04 : 0x00) |
						(intLightPen ? 0x08 : 0x00) |
						(pinIRQ ? 0x00 : 0x80)
						);
					break;
				case 0x1A:
					result &= 0xF0;
					result |= (byte)(
						(enableIntRaster ? 0x01 : 0x00) |
						(enableIntSpriteDataCollision ? 0x02 : 0x00) |
						(enableIntSpriteCollision ? 0x04 : 0x00) |
						(enableIntLightPen ? 0x08 : 0x00)
						);
					break;
				case 0x1B:
					result = (byte)(
						(sprites[0].priority ? 0x01 : 0x00) |
						(sprites[1].priority ? 0x02 : 0x00) |
						(sprites[2].priority ? 0x04 : 0x00) |
						(sprites[3].priority ? 0x08 : 0x00) |
						(sprites[4].priority ? 0x10 : 0x00) |
						(sprites[5].priority ? 0x20 : 0x00) |
						(sprites[6].priority ? 0x40 : 0x00) |
						(sprites[7].priority ? 0x80 : 0x00)
						);
					break;
				case 0x1C:
					result = (byte)(
						(sprites[0].multicolor ? 0x01 : 0x00) |
						(sprites[1].multicolor ? 0x02 : 0x00) |
						(sprites[2].multicolor ? 0x04 : 0x00) |
						(sprites[3].multicolor ? 0x08 : 0x00) |
						(sprites[4].multicolor ? 0x10 : 0x00) |
						(sprites[5].multicolor ? 0x20 : 0x00) |
						(sprites[6].multicolor ? 0x40 : 0x00) |
						(sprites[7].multicolor ? 0x80 : 0x00)
						);
					break;
				case 0x1D:
					result = (byte)(
						(sprites[0].xExpand ? 0x01 : 0x00) |
						(sprites[1].xExpand ? 0x02 : 0x00) |
						(sprites[2].xExpand ? 0x04 : 0x00) |
						(sprites[3].xExpand ? 0x08 : 0x00) |
						(sprites[4].xExpand ? 0x10 : 0x00) |
						(sprites[5].xExpand ? 0x20 : 0x00) |
						(sprites[6].xExpand ? 0x40 : 0x00) |
						(sprites[7].xExpand ? 0x80 : 0x00)
						);
					break;
				case 0x1E:
					result = (byte)(
						(sprites[0].collideSprite ? 0x01 : 0x00) |
						(sprites[1].collideSprite ? 0x02 : 0x00) |
						(sprites[2].collideSprite ? 0x04 : 0x00) |
						(sprites[3].collideSprite ? 0x08 : 0x00) |
						(sprites[4].collideSprite ? 0x10 : 0x00) |
						(sprites[5].collideSprite ? 0x20 : 0x00) |
						(sprites[6].collideSprite ? 0x40 : 0x00) |
						(sprites[7].collideSprite ? 0x80 : 0x00)
						);
					break;
				case 0x1F:
					result = (byte)(
						(sprites[0].collideData ? 0x01 : 0x00) |
						(sprites[1].collideData ? 0x02 : 0x00) |
						(sprites[2].collideData ? 0x04 : 0x00) |
						(sprites[3].collideData ? 0x08 : 0x00) |
						(sprites[4].collideData ? 0x10 : 0x00) |
						(sprites[5].collideData ? 0x20 : 0x00) |
						(sprites[6].collideData ? 0x40 : 0x00) |
						(sprites[7].collideData ? 0x80 : 0x00)
						);
					break;
				case 0x20:
					result &= 0xF0;
					result |= (byte)(borderColor & 0x0F);
					break;
				case 0x21:
					result &= 0xF0;
					result |= (byte)(backgroundColor0 & 0x0F);
					break;
				case 0x22:
					result &= 0xF0;
					result |= (byte)(backgroundColor1 & 0x0F);
					break;
				case 0x23:
					result &= 0xF0;
					result |= (byte)(backgroundColor2 & 0x0F);
					break;
				case 0x24:
					result &= 0xF0;
					result |= (byte)(backgroundColor3 & 0x0F);
					break;
				case 0x25:
					result &= 0xF0;
					result |= (byte)(spriteMulticolor0 & 0x0F);
					break;
				case 0x26:
					result &= 0xF0;
					result |= (byte)(spriteMulticolor1 & 0x0F);
					break;
				case 0x27:
				case 0x28:
				case 0x29:
				case 0x2A:
				case 0x2B:
				case 0x2C:
				case 0x2D:
				case 0x2E:
					result &= 0xF0;
					result |= (byte)(sprites[addr - 0x27].color & 0xF);
					break;
				default:
					// not connected
					break;
			}

			return result;
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0x3F;
			switch (addr)
			{
				case 0x19:
					// interrupts are cleared by writing a 1
					if ((val & 0x01) != 0)
						intRaster = false;
					if ((val & 0x02) != 0)
						intSpriteDataCollision = false;
					if ((val & 0x04) != 0)
						intSpriteCollision = false;
					if ((val & 0x08) != 0)
						intLightPen = false;
					UpdatePins();
					break;
				case 0x1A:
					WriteRegister(addr, val);
					break;
				case 0x1E:
				case 0x1F:
					// can't write to these
					break;
				case 0x2F:
				case 0x30:
				case 0x31:
				case 0x32:
				case 0x33:
				case 0x34:
				case 0x35:
				case 0x36:
				case 0x37:
				case 0x38:
				case 0x39:
				case 0x3A:
				case 0x3B:
				case 0x3C:
				case 0x3D:
				case 0x3E:
				case 0x3F:
					// not connected
					break;
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(ushort addr, byte val)
		{
			switch (addr)
			{
				case 0x00:
				case 0x02:
				case 0x04:
				case 0x06:
				case 0x08:
				case 0x0A:
				case 0x0C:
				case 0x0E:
					sprites[addr >> 1].x &= 0x100;
					sprites[addr >> 1].x |= val;
					break;
				case 0x01:
				case 0x03:
				case 0x05:
				case 0x07:
				case 0x09:
				case 0x0B:
				case 0x0D:
				case 0x0F:
					sprites[addr >> 1].y = val;
					break;
				case 0x10:
					sprites[0].x = (sprites[0].x & 0xFF) | ((val & 0x01) << 8);
					sprites[1].x = (sprites[1].x & 0xFF) | ((val & 0x02) << 7);
					sprites[2].x = (sprites[2].x & 0xFF) | ((val & 0x04) << 6);
					sprites[3].x = (sprites[3].x & 0xFF) | ((val & 0x08) << 5);
					sprites[4].x = (sprites[4].x & 0xFF) | ((val & 0x10) << 4);
					sprites[5].x = (sprites[5].x & 0xFF) | ((val & 0x20) << 3);
					sprites[6].x = (sprites[6].x & 0xFF) | ((val & 0x40) << 2);
					sprites[7].x = (sprites[7].x & 0xFF) | ((val & 0x80) << 1);
					break;
				case 0x11:
					yScroll = (val & 0x07);
					rowSelect = ((val & 0x08) != 0);
					displayEnable = ((val & 0x10) != 0);
					bitmapMode = ((val & 0x20) != 0);
					extraColorMode = ((val & 0x40) != 0);
					rasterInterruptLine &= 0xFF;
					rasterInterruptLine |= (val & 0x80) << 1;
					UpdateBorder();
					break;
				case 0x12:
					rasterInterruptLine &= 0x100;
					rasterInterruptLine |= val;
					break;
				case 0x13:
					lightPenX = val;
					break;
				case 0x14:
					lightPenY = val;
					break;
				case 0x15:
					sprites[0].enable = ((val & 0x01) != 0);
					sprites[1].enable = ((val & 0x02) != 0);
					sprites[2].enable = ((val & 0x04) != 0);
					sprites[3].enable = ((val & 0x08) != 0);
					sprites[4].enable = ((val & 0x10) != 0);
					sprites[5].enable = ((val & 0x20) != 0);
					sprites[6].enable = ((val & 0x40) != 0);
					sprites[7].enable = ((val & 0x80) != 0);
					break;
				case 0x16:
					xScroll = (val & 0x07);
					columnSelect = ((val & 0x08) != 0);
					multicolorMode = ((val & 0x10) != 0);
					UpdateBorder();
					break;
				case 0x17:
					sprites[0].yExpand = ((val & 0x01) != 0);
					sprites[1].yExpand = ((val & 0x02) != 0);
					sprites[2].yExpand = ((val & 0x04) != 0);
					sprites[3].yExpand = ((val & 0x08) != 0);
					sprites[4].yExpand = ((val & 0x10) != 0);
					sprites[5].yExpand = ((val & 0x20) != 0);
					sprites[6].yExpand = ((val & 0x40) != 0);
					sprites[7].yExpand = ((val & 0x80) != 0);
					break;
				case 0x18:
					pointerVM = ((val >> 4) & 0xF);
					pointerCB = ((val >> 1) & 0x7);
					break;
				case 0x19:
					intRaster = ((val & 0x01) != 0);
					intSpriteDataCollision = ((val & 0x02) != 0);
					intSpriteCollision = ((val & 0x04) != 0);
					intLightPen = ((val & 0x08) != 0);
					UpdatePins();
					break;
				case 0x1A:
					enableIntRaster = ((val & 0x01) != 0);
					enableIntSpriteDataCollision = ((val & 0x02) != 0);
					enableIntSpriteCollision = ((val & 0x04) != 0);
					enableIntLightPen = ((val & 0x08) != 0);
					UpdatePins();
					break;
				case 0x1B:
					sprites[0].priority = ((val & 0x01) != 0);
					sprites[1].priority = ((val & 0x02) != 0);
					sprites[2].priority = ((val & 0x04) != 0);
					sprites[3].priority = ((val & 0x08) != 0);
					sprites[4].priority = ((val & 0x10) != 0);
					sprites[5].priority = ((val & 0x20) != 0);
					sprites[6].priority = ((val & 0x40) != 0);
					sprites[7].priority = ((val & 0x80) != 0);
					break;
				case 0x1C:
					sprites[0].multicolor = ((val & 0x01) != 0);
					sprites[1].multicolor = ((val & 0x02) != 0);
					sprites[2].multicolor = ((val & 0x04) != 0);
					sprites[3].multicolor = ((val & 0x08) != 0);
					sprites[4].multicolor = ((val & 0x10) != 0);
					sprites[5].multicolor = ((val & 0x20) != 0);
					sprites[6].multicolor = ((val & 0x40) != 0);
					sprites[7].multicolor = ((val & 0x80) != 0);
					break;
				case 0x1D:
					sprites[0].xExpand = ((val & 0x01) != 0);
					sprites[1].xExpand = ((val & 0x02) != 0);
					sprites[2].xExpand = ((val & 0x04) != 0);
					sprites[3].xExpand = ((val & 0x08) != 0);
					sprites[4].xExpand = ((val & 0x10) != 0);
					sprites[5].xExpand = ((val & 0x20) != 0);
					sprites[6].xExpand = ((val & 0x40) != 0);
					sprites[7].xExpand = ((val & 0x80) != 0);
					break;
				case 0x1E:
					sprites[0].collideSprite = ((val & 0x01) != 0);
					sprites[1].collideSprite = ((val & 0x02) != 0);
					sprites[2].collideSprite = ((val & 0x04) != 0);
					sprites[3].collideSprite = ((val & 0x08) != 0);
					sprites[4].collideSprite = ((val & 0x10) != 0);
					sprites[5].collideSprite = ((val & 0x20) != 0);
					sprites[6].collideSprite = ((val & 0x40) != 0);
					sprites[7].collideSprite = ((val & 0x80) != 0);
					break;
				case 0x1F:
					sprites[0].collideData = ((val & 0x01) != 0);
					sprites[1].collideData = ((val & 0x02) != 0);
					sprites[2].collideData = ((val & 0x04) != 0);
					sprites[3].collideData = ((val & 0x08) != 0);
					sprites[4].collideData = ((val & 0x10) != 0);
					sprites[5].collideData = ((val & 0x20) != 0);
					sprites[6].collideData = ((val & 0x40) != 0);
					sprites[7].collideData = ((val & 0x80) != 0);
					break;
				case 0x20:
					borderColor = (val & 0xF);
					break;
				case 0x21:
					backgroundColor0 = (val & 0xF);
					break;
				case 0x22:
					backgroundColor1 = (val & 0xF);
					break;
				case 0x23:
					backgroundColor2 = (val & 0xF);
					break;
				case 0x24:
					backgroundColor3 = (val & 0xF);
					break;
				case 0x25:
					spriteMulticolor0 = (val & 0xF);
					break;
				case 0x26:
					spriteMulticolor1 = (val & 0xF);
					break;
				case 0x27:
				case 0x28:
				case 0x29:
				case 0x2A:
				case 0x2B:
				case 0x2C:
				case 0x2D:
				case 0x2E:
					sprites[addr - 0x27].color = (val & 0xF);
					break;
				default:
					break;
			}
		}

		// --------------------------

		public void SyncState(Serializer ser)
		{
			for (int i = 0; i < 8; i++)
			{
				ser.BeginSection("sprite" + i.ToString());
				sprites[i].SyncState(ser);
				ser.EndSection();
			}
			ser.Sync("backgroundColor0", ref backgroundColor0);
			ser.Sync("backgroundColor1", ref backgroundColor1);
			ser.Sync("backgroundColor2", ref backgroundColor2);
			ser.Sync("backgroundColor3", ref backgroundColor3);
			ser.Sync("baCount", ref baCount);
			ser.Sync("badline", ref badline);
			ser.Sync("badlineEnable", ref badlineEnable);
			ser.Sync("bitmapColumn", ref bitmapColumn);
			ser.Sync("bitmapMode", ref bitmapMode);
			ser.Sync("borderB", ref borderB);
			ser.Sync("borderCheckLEnable", ref borderCheckLEnable);
			ser.Sync("borderCheckREnable", ref borderCheckREnable);
			ser.Sync("borderColor", ref borderColor);
			ser.Sync("borderL", ref borderL);
			ser.Sync("borderOnMain", ref borderOnMain);
			ser.Sync("borderOnVertical", ref borderOnVertical);
			ser.Sync("borderR", ref borderR);
			ser.Sync("borderT", ref borderT);
			ser.Sync("bufferC", ref bufferC, false);
			ser.Sync("bufferG", ref bufferG, false);
			ser.Sync("columnSelect", ref columnSelect);
			ser.Sync("cycle", ref cycle);
			ser.Sync("cycleIndex", ref cycleIndex);
			ser.Sync("dataC", ref dataC);
			ser.Sync("dataG", ref dataG);
			ser.Sync("displayC", ref displayC);
			ser.Sync("displayEnable", ref displayEnable);
			ser.Sync("displayIndex", ref displayIndex);
			ser.Sync("enableIntLightPen", ref enableIntLightPen);
			ser.Sync("enableIntRaster", ref enableIntRaster);
			ser.Sync("enableIntSpriteCollision", ref enableIntSpriteCollision);
			ser.Sync("enableIntSpriteDataCollision", ref enableIntSpriteDataCollision);
			ser.Sync("extraColorMode", ref extraColorMode);
			ser.Sync("idle", ref idle);
			ser.Sync("intLightPen", ref intLightPen);
			ser.Sync("intRaster", ref intRaster);
			ser.Sync("intSpriteCollision", ref intSpriteCollision);
			ser.Sync("intSpriteDataCollision", ref intSpriteDataCollision);
			ser.Sync("lastRasterLine", ref lastRasterLine);
			ser.Sync("lightPenX", ref lightPenX);
			ser.Sync("lightPenY", ref lightPenY);
			ser.Sync("multicolorMode", ref multicolorMode);
			ser.Sync("pixelBuffer", ref pixelBuffer, false);
			ser.Sync("pixelBufferDelay", ref pixelBufferDelay);
			ser.Sync("pixelBufferIndex", ref pixelBufferIndex);
			ser.Sync("pixelBackgroundBuffer", ref pixelBackgroundBuffer, false);
			ser.Sync("pixelBackgroundBufferDelay", ref pixelBackgroundBufferDelay);
			ser.Sync("pixelBackgroundBufferIndex", ref pixelBackgroundBufferIndex);
			ser.Sync("pixelDataBuffer", ref pixelDataBuffer, false);
			ser.Sync("pointerCB", ref pointerCB);
			ser.Sync("pointerVM", ref pointerVM);
			ser.Sync("rasterInterruptLine", ref rasterInterruptLine);
			ser.Sync("rasterLine", ref rasterLine);
			ser.Sync("rasterX", ref rasterX);
			ser.Sync("rc", ref rc);
			ser.Sync("refreshCounter", ref refreshCounter);
			ser.Sync("rowSelect", ref rowSelect);
			ser.Sync("spriteMulticolor0", ref spriteMulticolor0);
			ser.Sync("spriteMulticolor1", ref spriteMulticolor1);
			ser.Sync("sr", ref sr);
			ser.Sync("vc", ref vc);
			ser.Sync("vcbase", ref vcbase);
			ser.Sync("vmli", ref vmli);
			ser.Sync("xOffset", ref xOffset);
			ser.Sync("xScroll", ref xScroll);
			ser.Sync("yScroll", ref yScroll);

			ser.Sync("cyclesPerSec", ref cyclesPerSec);
			ser.Sync("pinAEC", ref pinAEC);
			ser.Sync("pinBA", ref pinBA);
			ser.Sync("pinIRQ", ref pinIRQ);
			ser.Sync("totalCycles", ref totalCycles);
			ser.Sync("totalLines", ref totalLines);
		}
	}
}
