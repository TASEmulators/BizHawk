using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew : IVideoProvider
	{
		private int cycle;
		private Action[][] pipeline;
		private bool pipelineGAccess;
		private bool pipelineMemoryBusy;
		private int pipelineLength;

		private void ExecutePipeline()
		{
			pipelineGAccess = false;
			pipelineMemoryBusy = false;

			foreach (Action a in pipeline[cycle])
				a();

			cycle++;
			if (cycle >= pipelineLength)
			{
				cycle = 0;
				PipelineRasterAdvance();
			}

			signal.VicAEC = !pipelineMemoryBusy;
		}

		private void InitPipeline(Region region)
		{
			switch (region)
			{
				case Region.NTSC:
					plotterDelay = 12;
					rasterLines = 263;
					rasterLeft = 0x19C;
					pipeline = new Action[][]
					{
						new Action[]
						{	// 0
							PipelineCycle,
							PipelineIRQ0,
							PipelineFetchSprite3P,
							PipelineRender
						},
						new Action[]
						{	// 1
							PipelineCycle,
							PipelineIRQ1,
							PipelineFetchSprite3S,
							PipelineRender
						},
						new Action[]
						{	// 2
							PipelineCycle,
							PipelineFetchSprite4P,
							PipelineRender
						},
						new Action[]
						{	// 3
							PipelineCycle,
							PipelineFetchSprite4S,
							PipelineRender
						},
						new Action[]
						{	// 4
							PipelineCycle,
							PipelineFetchSprite5P,
							PipelineRender
						},
						new Action[]
						{	// 5
							PipelineCycle,
							PipelineFetchSprite5S,
							PipelineRender
						},
						new Action[]
						{	// 6
							PipelineCycle,
							PipelineFetchSprite6P,
							PipelineRender
						},
						new Action[]
						{	// 7
							PipelineCycle,
							PipelineFetchSprite6S,
							PipelineRender
						},
						new Action[]
						{	// 8
							PipelineCycle,
							PipelineFetchSprite7P,
							PipelineRender
						},
						new Action[]
						{	// 9
							PipelineCycle,
							PipelineFetchSprite7S,
							PipelineRender
						},
						new Action[]
						{	// 10
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 11
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 12
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 13
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 14
							PipelineCycle,
							PipelineVCReset,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 15
							PipelineCycle,
							PipelineSpriteMCBASEAdvance,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 16
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 17
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 18
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 19
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 20
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 21
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 22
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 23
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 24
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 25
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 26
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 27
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 28
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 29
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 30
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 31
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 32
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 33
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 34
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 35
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 36
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 37
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 38
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 39
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 40
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 41
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 42
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 43
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 44
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 45
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 46
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 47
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 48
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 49
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 50
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 51
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 52
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 53
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 54
							PipelineCycle,
							PipelineFetchC,
							PipelineSpriteMYEFlip,
							PipelineSpriteEnable0,
							PipelineRender
						},
						new Action[]
						{	// 55
							PipelineCycle,
							PipelineSpriteEnable1,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 56
							PipelineCycle,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 57
							PipelineCycle,
							PipelineSpriteDMA,
							PipelineRCReset,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 58
							PipelineCycle,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 59
							PipelineCycle,
							PipelineFetchSprite0P,
							PipelineRender
						},
						new Action[]
						{	// 60
							PipelineCycle,
							PipelineFetchSprite0S,
							PipelineRender
						},
						new Action[]
						{	// 61
							PipelineCycle,
							PipelineFetchSprite1P,
							PipelineRender
						},
						new Action[]
						{	// 62
							PipelineCycle,
							PipelineFetchSprite1S,
							PipelineBorderCheck,
							PipelineRender
						},
						new Action[]
						{	// 63
							PipelineCycle,
							PipelineFetchSprite2P,
							PipelineRender
						},
						new Action[]
						{	// 64
							PipelineCycle,
							PipelineFetchSprite2S,
							PipelineRender
						},
					};
					break;
				case Region.PAL:
					plotterDelay = 4;
					rasterLines = 312;
					rasterLeft = 0x194;
					pipeline = new Action[][]
					{
						new Action[]
						{	// 0
							PipelineCycle,
							PipelineIRQ0,
							PipelineFetchSprite3P,
							PipelineRender
						},
						new Action[]
						{	// 1
							PipelineCycle,
							PipelineIRQ1,
							PipelineFetchSprite3S,
							PipelineRender
						},
						new Action[]
						{	// 2
							PipelineCycle,
							PipelineFetchSprite4P,
							PipelineRender
						},
						new Action[]
						{	// 3
							PipelineCycle,
							PipelineFetchSprite4S,
							PipelineRender
						},
						new Action[]
						{	// 4
							PipelineCycle,
							PipelineFetchSprite5P,
							PipelineRender
						},
						new Action[]
						{	// 5
							PipelineCycle,
							PipelineFetchSprite5S,
							PipelineRender
						},
						new Action[]
						{	// 6
							PipelineCycle,
							PipelineFetchSprite6P,
							PipelineRender
						},
						new Action[]
						{	// 7
							PipelineCycle,
							PipelineFetchSprite6S,
							PipelineRender
						},
						new Action[]
						{	// 8
							PipelineCycle,
							PipelineFetchSprite7P,
							PipelineRender
						},
						new Action[]
						{	// 9
							PipelineCycle,
							PipelineFetchSprite7S,
							PipelineRender
						},
						new Action[]
						{	// 10
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 11
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 12
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 13
							PipelineCycle,
							PipelineVCReset,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 14
							PipelineCycle,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 15
							PipelineCycle,
							PipelineSpriteMCBASEAdvance,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 16
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 17
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 18
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 19
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 20
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 21
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 22
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 23
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 24
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 25
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 26
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 27
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 28
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 29
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 30
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 31
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 32
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 33
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 34
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 35
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 36
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 37
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 38
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 39
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 40
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 41
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 42
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 43
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 44
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 45
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 46
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 47
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 48
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 49
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 50
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 51
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 52
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 53
							PipelineCycle,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 54
							PipelineCycle,
							PipelineFetchC,
							PipelineSpriteMYEFlip,
							PipelineSpriteEnable0,
							PipelineRender
						},
						new Action[]
						{	// 55
							PipelineCycle,
							PipelineSpriteEnable1,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 56
							PipelineCycle,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 57
							PipelineCycle,
							PipelineSpriteDMA,
							PipelineRCReset,
							PipelineFetchSprite0P,
							PipelineRender
						},
						new Action[]
						{	// 58
							PipelineCycle,
							PipelineFetchSprite0S,
							PipelineRender
						},
						new Action[]
						{	// 59
							PipelineCycle,
							PipelineFetchSprite1P,
							PipelineRender
						},
						new Action[]
						{	// 60
							PipelineCycle,
							PipelineFetchSprite1S,
							PipelineRender
						},
						new Action[]
						{	// 61
							PipelineCycle,
							PipelineFetchSprite2P,
							PipelineRender
						},
						new Action[]
						{	// 62
							PipelineCycle,
							PipelineFetchSprite2S,
							PipelineBorderCheck,
							PipelineRender
						}
					};
					break;
			}

			pipelineLength = pipeline.Length;
		}

		private void PipelineBadlineDelay()
		{
			if (badline)
				pipelineMemoryBusy = true;
		}

		private void PipelineBorderCheck()
		{
			if ((RASTER == borderTop) && (DEN))
				borderOnVertical = false;
			if (RASTER == borderBottom)
				borderOnVertical = true;
		}

		private void PipelineCycle()
		{
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

		private void PipelineDramRefresh()
		{
			mem.VicRead((ushort)refreshAddress);
			refreshAddress = (refreshAddress - 1) & 0xFF;
			refreshAddress |= 0x3F00;
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
					pipelineMemoryBusy = true;
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

		private void PipelineFetchG()
		{
			int gAddress;

			if (idle || VMLI >= 40 || !displayEnabled)
			{
				PipelineIdle();
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

		private void PipelineFetchSprite0P()
		{
			PipelineFetchSpriteP(0);
		}

		private void PipelineFetchSprite0S()
		{
			PipelineFetchSpriteS(0);
		}

		private void PipelineFetchSprite1P()
		{
			PipelineFetchSpriteP(1);
		}

		private void PipelineFetchSprite1S()
		{
			PipelineFetchSpriteS(1);
		}

		private void PipelineFetchSprite2P()
		{
			PipelineFetchSpriteP(2);
		}

		private void PipelineFetchSprite2S()
		{
			PipelineFetchSpriteS(2);
		}

		private void PipelineFetchSprite3P()
		{
			PipelineFetchSpriteP(3);
		}

		private void PipelineFetchSprite3S()
		{
			PipelineFetchSpriteS(3);
		}

		private void PipelineFetchSprite4P()
		{
			PipelineFetchSpriteP(4);
		}

		private void PipelineFetchSprite4S()
		{
			PipelineFetchSpriteS(4);
		}

		private void PipelineFetchSprite5P()
		{
			PipelineFetchSpriteP(5);
		}

		private void PipelineFetchSprite5S()
		{
			PipelineFetchSpriteS(5);
		}

		private void PipelineFetchSprite6P()
		{
			PipelineFetchSpriteP(6);
		}

		private void PipelineFetchSprite6S()
		{
			PipelineFetchSpriteS(6);
		}

		private void PipelineFetchSprite7P()
		{
			PipelineFetchSpriteP(7);
		}

		private void PipelineFetchSprite7S()
		{
			PipelineFetchSpriteS(7);
		}

		private void PipelineFetchSpriteP(int index)
		{
			ushort pointerOffset = (ushort)((VM << 10) | 0x3F8 | index);
			sprites[index].MPTR = mem.VicRead(pointerOffset);

			if (sprites[index].MDMA)
			{
				sprites[index].MSR = mem.VicRead((ushort)((sprites[index].MPTR << 6) | (sprites[index].MC)));
				sprites[index].MC++;
				pipelineMemoryBusy = true;
			}
		}

		private void PipelineFetchSpriteS(int index)
		{
			if (sprites[index].MDMA)
			{
				for (int i = 0; i < 2; i++)
				{
					sprites[index].MSR <<= 8;
					sprites[index].MSR |= mem.VicRead((ushort)((sprites[index].MPTR << 6) | (sprites[index].MC)));
					sprites[index].MC++;
				}
				pipelineMemoryBusy = true;
			}
		}

		private void PipelineIdle()
		{
			mem.VicRead(ECM ? (ushort)0x39FF : (ushort)0x3FFF);
		}

		private void PipelineIRQ0()
		{
			if (RASTER == rasterInterruptLine && RASTER > 0)
				IRST = true;
		}

		private void PipelineIRQ1()
		{
			if (RASTER == 0 && rasterInterruptLine == 0)
				IRST = true;
		}

		private void PipelinePlot()
		{
			switch (graphicsMode)
			{
				case 0x00:
					PipelinePlot000();
					break;
				case 0x01:
					PipelinePlot001();
					break;
				case 0x02:
					PipelinePlot010();
					break;
				case 0x03:
					PipelinePlot011();
					break;
				case 0x04:
					PipelinePlot100();
					break;
				case 0x05:
					PipelinePlot101();
					break;
				case 0x06:
					PipelinePlot110();
					break;
				case 0x07:
					PipelinePlot111();
					break;
			}
		}

		private void PipelinePlot000()
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

		private void PipelinePlot001()
		{
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
				PipelinePlot000();
			}
		}

		private void PipelinePlot010()
		{
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
		}

		private void PipelinePlot011()
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
					plotterPixel = characterData >> 4;
					break;
				case 0x02:
					plotterPixel = characterData & 0xF;
					break;
				case 0x03:
					plotterPixel = colorData & 0xF;
					break;
			}
		}

		private void PipelinePlot100()
		{
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
		}

		private void PipelinePlot101()
		{
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
		}

		private void PipelinePlot110()
		{
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
		}

		private void PipelinePlot111()
		{
			if ((bitmapColumn & 0x01) == 0x00)
			{
				plotterData = bitmapData >> 6;
				bitmapData <<= 2;
			}
			plotterPixel = 0;
		}

		private void PipelineRasterAdvance()
		{
			RASTER++;
			if (RASTER == rasterLines)
			{
				RASTER = 0;
				VCBASE = 0;
				displayEnabled = false;
				rasterX = rasterLeft;
			}
		}

		private void PipelineRCReset()
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

		private void PipelineRender()
		{
			for (int i = 0; i < 8; i++)
			{
				int pixel;

				if ((pipelineGAccess) && XSCROLL == i)
				{
					bitmapColumn = 0;
					PipelineFetchG();
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

				PipelinePlot();
				PipelineSprites();

				if (borderOnMain || borderOnVertical)
					pixel = EC;
				else
					pixel = (
						(spriteData == 0) ||
						((spriteData != 0) && (spritePriority = true) && (plotterDataBuffer[plotterBufferIndex] >= 0x2)))
						? plotterPixelBuffer[plotterBufferIndex] : spritePixel;

				PipelineWritePixel(pixel);

				plotterPixelBuffer[plotterBufferIndex] = plotterPixel;
				plotterDataBuffer[plotterBufferIndex] = plotterData;
				plotterBufferIndex++;
				if (plotterBufferIndex == plotterDelay)
					plotterBufferIndex = 0;

				bitmapColumn++;
				rasterX++;
				if (rasterX >= rasterWidth)
					rasterX -= rasterWidth;
			}
		}

		private void PipelineSprites()
		{
			int pixelOwner = -1;
			int data = 0;
			int pixel = 0;

			spriteData = 0;
			spritePixel = 0;
			spritePriority = false;

			for (int i = 0; i < 8; i++)
			{
				VicIINewSprite sprite = sprites[i];

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
						data = ((sprite.MSR >> 22) & 0x3);
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
						data = ((sprite.MSR >> 22) & 0x2);
						if (!sprite.MxXE || sprite.MxXEToggle)
						{
							sprite.MSR <<= 1;
						}
						sprite.MxXEToggle = !sprite.MxXEToggle;
					}

					switch (data)
					{
						case 1:
							pixel = MMx[0];
							break;
						case 2:
							pixel = sprite.MxC;
							break;
						case 3:
							pixel = MMx[1];
							break;
					}

					if (!borderOnVertical)
					{
						if (data != 0)
						{
							if (pixelOwner >= 0)
							{
								sprite.MxM = true;
								sprites[pixelOwner].MxM = true;
							}
							else
							{
								spritePriority = sprite.MxDP;
								spritePixel = pixel;
								spriteData = data;
								pixelOwner = i;
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

		private void PipelineSpriteDMA()
		{
			for (int i = 0; i < 8; i++)
			{
				VicIINewSprite sprite = sprites[i];
				sprite.MC = sprite.MCBASE;
				if (sprite.MDMA && sprite.MxY == (RASTER & 0xFF))
				{
					sprite.MxXEToggle = false;
				}
			}
		}

		private void PipelineSpriteEnable(int index)
		{
			int endIndex = index + 4;
			for (int i = index; i < endIndex; i++)
			{
				VicIINewSprite sprite = sprites[i];
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

		private void PipelineSpriteEnable0()
		{
			PipelineSpriteEnable(0);
		}

		private void PipelineSpriteEnable1()
		{
			PipelineSpriteEnable(4);
		}

		private void PipelineSpriteMCBASEAdvance()
		{
			for (int i = 0; i < 8; i++)
			{
				VicIINewSprite sprite = sprites[i];
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

		private void PipelineSpriteMYEFlip()
		{
			for (int i = 0; i < 8; i++)
				if (sprites[i].MxYE)
					sprites[i].MxYEToggle = !sprites[i].MxYEToggle;
		}

		private void PipelineVCReset()
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

		private void PipelineWritePixel(int pixelValue)
		{
			videoBuffer[videoBufferIndex++] = palette[pixelValue & 0xF];
			if (videoBufferIndex == videoBufferSize)
				videoBufferIndex = 0;
		}
	}
}
