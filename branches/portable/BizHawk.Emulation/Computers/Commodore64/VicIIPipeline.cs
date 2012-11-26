using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII : IVideoProvider
	{
		private int baCount;
		private int cycle;
		private uint[][] pipeline;
		private bool pipelineGAccess;
		private int pipelineLength;

		private void ExecutePipeline()
		{
			pipelineGAccess = false;
			advanceX = true;
			baCount = 0;

			uint tableX = pipeline[0][cycle];
			uint tableFetch = pipeline[1][cycle];
			uint tableBA = pipeline[2][cycle];
			uint tableOps = pipeline[3][cycle];
			
			#region Pipeline Cycle Init
			{

				//rasterX = (int)tableX;

				if (cycle == 0)
				{
					if (!rasterInterruptTriggered && RASTER == rasterInterruptLine && RASTER > 0)
					{
						IRST = true;
						rasterInterruptTriggered = true;
					}
				}
				else if (cycle == 1)
				{
					if (!rasterInterruptTriggered && RASTER == 0 && rasterInterruptLine == 0)
					{
						IRST = true;
						rasterInterruptTriggered = true;
					}
				}

				if (RASTER == 0x030)
					displayEnabled = (displayEnabled | DEN);

				if (RASTER >= 0x030 && RASTER < 0x0F8)
					badline = ((YSCROLL == (RASTER & 0x07)) && displayEnabled);
				else
					badline = false;

				if (badline)
					idle = false;

				for (int i = 0; i < 8; i++)
					if (!sprites[i].MxYE)
						sprites[i].MxYEToggle = true;
			}
			#endregion
			#region Pipeline Fetch
			{
				switch (tableFetch)
				{
					case 0x00: PipelineFetchSpriteP(0); break;
					case 0x01: PipelineFetchSpriteP(1); break;
					case 0x02: PipelineFetchSpriteP(2); break;
					case 0x03: PipelineFetchSpriteP(3); break;
					case 0x04: PipelineFetchSpriteP(4); break;
					case 0x05: PipelineFetchSpriteP(5); break;
					case 0x06: PipelineFetchSpriteP(6); break;
					case 0x07: PipelineFetchSpriteP(7); break;
					case 0x08: PipelineFetchSpriteS(0); break;
					case 0x09: PipelineFetchSpriteS(1); break;
					case 0x0A: PipelineFetchSpriteS(2); break;
					case 0x0B: PipelineFetchSpriteS(3); break;
					case 0x0C: PipelineFetchSpriteS(4); break;
					case 0x0D: PipelineFetchSpriteS(5); break;
					case 0x0E: PipelineFetchSpriteS(6); break;
					case 0x0F: PipelineFetchSpriteS(7); break;
					case 0x10:
						mem.VicRead(ECM ? (ushort)0x39FF : (ushort)0x3FFF);
						break;
					case 0x11:
						mem.VicRead((ushort)refreshAddress);
						refreshAddress = (refreshAddress - 1) & 0xFF;
						refreshAddress |= 0x3F00;
						break;
					case 0x12:
						PipelineFetchC();
						break;
				}
			}
			#endregion
			#region BA
			{
				uint baSprite0 = tableBA & 0xF;
				uint baSprite1 = (tableBA >> 4) & 0xF;
				uint baSprite2 = (tableBA >> 8) & 0xF;
				bool baFetch = ((tableBA >> 12) & 0x1) != 0;

				if ((baSprite0 < 8 && sprites[baSprite0].MDMA) || (baSprite1 < 8 && sprites[baSprite1].MDMA) || (baSprite2 < 8 && sprites[baSprite2].MDMA) || (baFetch && badline))
					baCount++;
			}
			#endregion
			#region Operations
			{
				//if ((tableOps & OpChkBrdL0) != 0)
				//{
				//}
				//if ((tableOps & OpChkBrdL1) != 0)
				//{
				//}
				//if ((tableOps & OpChkBrdR0) != 0)
				//{
				//}
				//if ((tableOps & OpChkBrdR1) != 0)
				//{
				//}
				//if ((tableOps & OpChkSprCrunch) != 0)
				//{
				//}
				if ((tableOps & OpChkSprDisp) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						VicIISprite sprite = sprites[i];
						sprite.MC = sprite.MCBASE;
						if (sprite.MDMA && sprite.MxY == (RASTER & 0xFF))
						{
							sprite.MxXEToggle = false;
						}
					}
				}
				if ((tableOps & OpChkSprDma) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						VicIISprite sprite = sprites[i];
						sprite.MD = false;
						if (sprite.MxE == true && sprite.MxY == (RASTER & 0xFF) && sprite.MDMA == false)
						{
							sprite.MDMA = true;
							sprite.MCBASE = 0;
							if (sprite.MxYE)
								sprite.MxYEToggle = false;
						}
						sprite.MxXEToggle = false;
					}
				}
				if ((tableOps & OpChkSprExp) != 0)
				{
					for (int i = 0; i < 8; i++)
						if (sprites[i].MxYE)
							sprites[i].MxYEToggle = !sprites[i].MxYEToggle;
				}
				if ((tableOps & OpUpdateMcBase) != 0)
				{
					for (int i = 0; i < 8; i++)
					{
						VicIISprite sprite = sprites[i];
						if (sprite.MxYEToggle)
						{
							sprite.MCBASE += 3;
							if (sprite.MxYEToggle && sprite.MCBASE == 63)
							{
								sprite.MDMA = false;
							}
						}
					}
				}
				if ((tableOps & OpUpdateRc) != 0)
				{
					if (RC == 7)
					{
						idle = true;
						VCBASE = VC;
					}
					if (!idle)
					{
						RC = (RC + 1) & 0x7;
					}
				}
				if ((tableOps & OpUpdateVc) != 0)
				{
					VC = VCBASE;
					VMLI = 0;
					bitmapColumn = 0;
					if (badline)
					{
						RC = 0;
					}
					bitmapData = 0;
					colorData = 0;
					characterData = 0;
				}
			}
			#endregion
			#region Render
			{

				for (int i = 0; i < 8; i++)
				{
					int pixel;

					if ((pipelineGAccess) && XSCROLL == i)
					{
						#region Fetch G
						{
							int gAddress;
							bitmapColumn = 0;

							if (idle || VMLI >= 40 || !displayEnabled)
							{
								mem.VicRead(ECM ? (ushort)0x39FF : (ushort)0x3FFF);
								characterData = 0;
								colorData = 0;
							}
							else
							{
								characterData = characterDataBus;
								colorData = colorDataBus;
							}
							switch (graphicsMode)
							{
								case 0: // 000
								case 1: // 001
									gAddress = (CB << 11) | (characterData << 3) | RC;
									bitmapData = mem.VicRead((ushort)gAddress);
									break;
								case 2: // 010
								case 3: // 011
									gAddress = ((CB & 0x4) << 11) | (VC << 3) | RC;
									bitmapData = mem.VicRead((ushort)gAddress);
									break;
								case 4: // 100
								case 5: // 101
									gAddress = (CB << 11) | ((characterData & 0x3F) << 3) | RC;
									bitmapData = mem.VicRead((ushort)gAddress);
									break;
								case 6: // 110
								case 7: // 111
									gAddress = ((CB & 0x4) << 11) | ((VC & 0x33F) << 3) | RC;
									bitmapData = mem.VicRead((ushort)gAddress);
									break;
							}
							if (!idle)
							{
								VC++;
								VMLI++;
							}
						}
						#endregion
					}

					if (rasterX == borderRight)
						borderOnMain = true;
					if (rasterX == borderLeft)
					{
						if (RASTER == borderBottom)
							borderOnVertical = true;
						if ((RASTER == borderTop) && DEN)
							borderOnVertical = false;
						if (!borderOnVertical)
							borderOnMain = false;
					}

					#region Plotter
					switch (graphicsMode)
					{
						case 0x00:
							if ((bitmapData & 0x80) != 0x00)
							{
								plotterData = 0x03;
								plotterPixel = colorData;
							}
							else
							{
								plotterData = 0x00;
								plotterPixel = BxC[0];
							}
							bitmapData <<= 1;
							break;
						case 0x01:
							if ((colorData & 0x08) != 0x00)
							{
								if ((bitmapColumn & 0x01) == 0x00)
								{
									plotterData = (bitmapData >> 6) & 0x03;
									bitmapData <<= 2;
								}
								switch (plotterData)
								{
									case 0x00:
										plotterPixel = BxC[0];
										break;
									case 0x01:
										plotterPixel = BxC[1];
										break;
									case 0x02:
										plotterPixel = BxC[2];
										break;
									case 0x03:
										plotterPixel = colorData & 0x7;
										break;
								}
							}
							else
							{
								if ((bitmapData & 0x80) != 0x00)
								{
									plotterData = 0x03;
									plotterPixel = colorData;
								}
								else
								{
									plotterData = 0x00;
									plotterPixel = BxC[0];
								}
								bitmapData <<= 1;
							}
							break;
						case 0x02:
							if ((bitmapData & 0x80) != 0x00)
							{
								plotterData = 0x03;
								plotterPixel = characterData >> 4;
							}
							else
							{
								plotterData = 0x00;
								plotterPixel = characterData & 0xF;
							}
							bitmapData <<= 1;
							break;
						case 0x03:
							if ((bitmapColumn & 0x01) == 0x00)
							{
								plotterData = (bitmapData >> 6) & 0x03;
								bitmapData <<= 2;
							}
							switch (plotterData)
							{
								case 0x00:
									plotterPixel = BxC[0];
									break;
								case 0x01:
									plotterPixel = characterData >> 4;
									break;
								case 0x02:
									plotterPixel = characterData & 0xF;
									break;
								case 0x03:
									plotterPixel = colorData & 0xF;
									break;
							}
							break;
						case 0x04:
							if ((bitmapData & 0x80) != 0x00)
							{
								plotterData = 0x03;
								plotterPixel = colorData;
							}
							else
							{
								plotterData = 0x00;
								plotterPixel = BxC[characterData >> 6];
							}
							bitmapData <<= 1;
							break;
						case 0x05:
							if ((colorData & 0x08) != 0x00)
							{
								if ((bitmapColumn & 0x01) == 0x00)
								{
									plotterData = bitmapData >> 6;
									plotterPixel = 0;
									bitmapData <<= 2;
								}
							}
							else
							{
								plotterData = bitmapData >> 7;
								plotterPixel = 0;
								bitmapData <<= 1;
							}
							break;
						case 0x06:
							if ((bitmapData & 0x80) != 0x00)
							{
								plotterData = 0x03;
								plotterPixel = 0;
							}
							else
							{
								plotterData = 0x00;
								plotterPixel = 0;
							}
							bitmapData <<= 1;
							break;
						case 0x07:
							if ((bitmapColumn & 0x01) == 0x00)
							{
								plotterData = bitmapData >> 6;
								bitmapData <<= 2;
							}
							plotterPixel = 0;
							break;
					}
					#endregion
					#region Sprites
					{
						int pixelOwner = -1;
						int sData = 0;
						int sPixel = 0;

						spriteData = 0;
						spritePixel = 0;
						spritePriority = false;

						for (int j = 0; j < 8; j++)
						{
							VicIISprite sprite = sprites[j];

							if (sprite.MSR == 0)
							{
								sprite.MD = false;
							}
							else if ((!sprite.MD) && (sprite.MxX == rasterX))
							{
								sprite.MD = true;
							}

							if (sprite.MD)
							{
								if (sprite.MxMC)
								{
									sData = ((sprite.MSR >> 22) & 0x3);
									if ((rasterX & 0x1) != (sprite.MxX & 0x1))
									{
										if (!sprite.MxXE || sprite.MxXEToggle)
										{
											sprite.MSR <<= 2;
										}
										sprite.MxXEToggle = !sprite.MxXEToggle;
									}
								}
								else
								{
									sData = ((sprite.MSR >> 22) & 0x2);
									if (!sprite.MxXE || sprite.MxXEToggle)
									{
										sprite.MSR <<= 1;
									}
									sprite.MxXEToggle = !sprite.MxXEToggle;
								}

								if (!borderOnVertical)
								{
									if (sData != 0)
									{
										if (pixelOwner >= 0)
										{
											sprite.MxM = true;
											sprites[pixelOwner].MxM = true;
										}
										else
										{
											switch (sData)
											{
												case 1:
													sPixel = MMx[0];
													break;
												case 2:
													sPixel = sprite.MxC;
													break;
												case 3:
													sPixel = MMx[1];
													break;
											}

											spritePriority = sprite.MxDP;
											spritePixel = sPixel;
											spriteData = sData;
											pixelOwner = j;
										}
										if (plotterDataBuffer[plotterBufferIndex] >= 0x2)
										{
											sprite.MxD = true;
											IMBC = true;
										}
									}
								}
							}
						}
					}
					#endregion
					#region Pixelbuffer Write
					{
						if (borderOnMain || borderOnVertical)
							pixel = EC;
						else
						{
							if ((spriteData == 0) || (spritePriority == true && plotterDataBuffer[plotterBufferIndex] >= 0x2))
							{
								pixel = plotterPixelBuffer[plotterBufferIndex];
							}
							else
							{
								pixel = spritePixel;
							}
						}

						// write pixel to buffer
						videoBuffer[videoBufferIndex++] = palette[pixel & 0xF];
						if (videoBufferIndex == videoBufferSize)
							videoBufferIndex = 0;

						plotterPixelBuffer[plotterBufferIndex] = plotterPixel;
						plotterDataBuffer[plotterBufferIndex] = plotterData;
						plotterBufferIndex++;
						if (plotterBufferIndex == plotterDelay)
							plotterBufferIndex = 0;

						bitmapColumn++;
						if (advanceX)
						{
							rasterX++;
							if (rasterX >= rasterWidth)
								rasterX -= rasterWidth;
						}
					}
					#endregion
				}
			}
			#endregion

			cycle++;
			if (cycle >= pipelineLength)
			{
				cycle = 0;
				rasterInterruptTriggered = false;
				RASTER++;
				if (RASTER == rasterLines)
				{
					RASTER = 0;
					VCBASE = 0;
					displayEnabled = false;
					rasterX = rasterLeft;
				}
				badline = false;
			}

			signal.VicBA = (baCount > 0);
			PipelineBA(signal.VicBA);
			if (baCount > 0)
			{
				if (fetchCounter > 0)
					fetchCounter--;
				signal.VicAEC = (fetchCounter != 0);
			}
			else
			{
				fetchCounter = 0;
				signal.VicAEC = true;
			}
		}

		private void InitPipeline(Region region)
		{
			switch (region)
			{
				case Region.NTSC:
					plotterDelay = 12;
					rasterLines = 263;
					rasterLeft = 0x19C;
					pipeline = cycleTabNTSC;
					break;
				case Region.PAL:
					plotterDelay = 4;
					rasterLines = 312;
					rasterLeft = 0x194;
					pipeline = cycleTabPAL;
					break;
			}

			pipelineLength = pipeline[0].Length;
		}

		private void PipelineBA(bool val)
		{
			if (val)
			{
				if (signal.VicAEC == true && fetchCounter == 0)
					fetchCounter = 4;
			}
			else
			{
				fetchCounter = 0;
			}
		}

		private void PipelineBorderCheck()
		{
			if ((RASTER == borderTop) && (DEN))
				borderOnVertical = false;
			if (RASTER == borderBottom)
				borderOnVertical = true;
		}

		private void PipelineFetchC()
		{
			pipelineGAccess = true;
			if (idle || VMLI >= 40)
			{
				characterDataBus = 0;
				colorDataBus = 0;
			}
			else
			{
				if (badline)
				{
					int cAddress = (VM << 10) | VC;
					characterDataBus = mem.VicRead((ushort)cAddress);
					colorDataBus = mem.colorRam[VC];
				}
				else
				{
					characterDataBus = characterMemory[VMLI];
					colorDataBus = colorMemory[VMLI];
					return;
				}
				colorMemory[VMLI] = colorDataBus;
				characterMemory[VMLI] = characterDataBus;
			}
		}

		private void PipelineFetchSpriteP(int index)
		{
			VicIISprite spr = sprites[index];
			ushort pointerOffset = (ushort)((VM << 10) | 0x3F8 | index);

			spr.MPTR = mem.VicRead(pointerOffset);

			if (spr.MDMA)
			{
				spr.MSR = mem.VicRead((ushort)((spr.MPTR << 6) | (spr.MC)));
				spr.MC++;
			}
		}

		private void PipelineFetchSpriteS(int index)
		{
			VicIISprite spr = sprites[index];
			if (spr.MDMA)
			{
				for (int i = 0; i < 2; i++)
				{
					spr.MSR <<= 8;
					spr.MSR |= mem.VicRead((ushort)((spr.MPTR << 6) | (spr.MC)));
					spr.MC++;
				}
			}
		}
	}
}
