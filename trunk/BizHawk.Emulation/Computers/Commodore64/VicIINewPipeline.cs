using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew
	{
		private int cycle;
		private Action[][] pipeline;
		private int pipelineLength;

		private void ExecutePipeline()
		{
			foreach (Action a in pipeline[cycle++])
				a();

			if (cycle >= pipelineLength)
				cycle = 0;
		}

		private void InitPipeline(Region region)
		{
			switch (region)
			{
				case Region.NTSC:
					rasterLines = 263;
					rasterLeft = 0x19C;
					pipeline = new Action[][]
					{
						new Action[]
						{	// 0
							PipelineScan,
							PipelineIRQ0,
							PipelineFetchSprite3P,
							PipelineRender
						},
						new Action[]
						{	// 1
							PipelineScan,
							PipelineIRQ1,
							PipelineFetchSprite3S,
							PipelineRender
						},
						new Action[]
						{	// 2
							PipelineScan,
							PipelineFetchSprite4P,
							PipelineRender
						},
						new Action[]
						{	// 3
							PipelineScan,
							PipelineFetchSprite4S,
							PipelineRender
						},
						new Action[]
						{	// 4
							PipelineScan,
							PipelineFetchSprite5P,
							PipelineRender
						},
						new Action[]
						{	// 5
							PipelineScan,
							PipelineFetchSprite5S,
							PipelineRender
						},
						new Action[]
						{	// 6
							PipelineScan,
							PipelineFetchSprite6P,
							PipelineRender
						},
						new Action[]
						{	// 7
							PipelineScan,
							PipelineFetchSprite6S,
							PipelineRender
						},
						new Action[]
						{	// 8
							PipelineScan,
							PipelineFetchSprite7P,
							PipelineRender
						},
						new Action[]
						{	// 9
							PipelineScan,
							PipelineFetchSprite7S,
							PipelineRender
						},
						new Action[]
						{	// 10
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 11
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 12
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 13
							PipelineScan,
							PipelineVCReset,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 14
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 15
							PipelineScan,
							PipelineSpriteMCBASEAdvance,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 16
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 17
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 18
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 19
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 20
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 21
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 22
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 23
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 24
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 25
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 26
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 27
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 28
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 29
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 30
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 31
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 32
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 33
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 34
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 35
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 36
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 37
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 38
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 39
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 40
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 41
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 42
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 43
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 44
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 45
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 46
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 47
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 48
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 49
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 50
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 51
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 52
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 53
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 54
							PipelineScan,
							PipelineFetchC,
							PipelineSpriteMYEFlip,
							PipelineSpriteEnable0,
							PipelineRender
						},
						new Action[]
						{	// 55
							PipelineScan,
							PipelineSpriteEnable1,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 56
							PipelineScan,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 57
							PipelineScan,
							PipelineSpriteDMA,
							PipelineRCReset,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 58
							PipelineScan,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 59
							PipelineScan,
							PipelineFetchSprite0P,
							PipelineRender
						},
						new Action[]
						{	// 60
							PipelineScan,
							PipelineFetchSprite0S,
							PipelineRender
						},
						new Action[]
						{	// 61
							PipelineScan,
							PipelineFetchSprite1P,
							PipelineRender
						},
						new Action[]
						{	// 62
							PipelineScan,
							PipelineFetchSprite1S,
							PipelineBorderCheck,
							PipelineRender
						},
						new Action[]
						{	// 63
							PipelineScan,
							PipelineFetchSprite2P,
							PipelineRender
						},
						new Action[]
						{	// 64
							PipelineScan,
							PipelineFetchSprite2S,
							PipelineRender
						},
					};
					break;
				case Region.PAL:
					rasterLines = 312;
					rasterLeft = 0x194;
					pipeline = new Action[][]
					{
						new Action[]
						{	// 0
							PipelineScan,
							PipelineIRQ0,
							PipelineFetchSprite3P,
							PipelineRender
						},
						new Action[]
						{	// 1
							PipelineScan,
							PipelineIRQ1,
							PipelineFetchSprite3S,
							PipelineRender
						},
						new Action[]
						{	// 2
							PipelineScan,
							PipelineFetchSprite4P,
							PipelineRender
						},
						new Action[]
						{	// 3
							PipelineScan,
							PipelineFetchSprite4S,
							PipelineRender
						},
						new Action[]
						{	// 4
							PipelineScan,
							PipelineFetchSprite5P,
							PipelineRender
						},
						new Action[]
						{	// 5
							PipelineScan,
							PipelineFetchSprite5S,
							PipelineRender
						},
						new Action[]
						{	// 6
							PipelineScan,
							PipelineFetchSprite6P,
							PipelineRender
						},
						new Action[]
						{	// 7
							PipelineScan,
							PipelineFetchSprite6S,
							PipelineRender
						},
						new Action[]
						{	// 8
							PipelineScan,
							PipelineFetchSprite7P,
							PipelineRender
						},
						new Action[]
						{	// 9
							PipelineScan,
							PipelineFetchSprite7S,
							PipelineRender
						},
						new Action[]
						{	// 10
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 11
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 12
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 13
							PipelineScan,
							PipelineVCReset,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 14
							PipelineScan,
							PipelineDramRefresh,
							PipelineRender
						},
						new Action[]
						{	// 15
							PipelineScan,
							PipelineSpriteMCBASEAdvance,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 16
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 17
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 18
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 19
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 20
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 21
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 22
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 23
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 24
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 25
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 26
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 27
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 28
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 29
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 30
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 31
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 32
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 33
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 34
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 35
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 36
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 37
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 38
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 39
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 40
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 41
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 42
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 43
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 44
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 45
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 46
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 47
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 48
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 49
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 50
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 51
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 52
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 53
							PipelineScan,
							PipelineFetchC,
							PipelineRender
						},
						new Action[]
						{	// 54
							PipelineScan,
							PipelineFetchC,
							PipelineSpriteMYEFlip,
							PipelineSpriteEnable0,
							PipelineRender
						},
						new Action[]
						{	// 55
							PipelineScan,
							PipelineSpriteEnable1,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 56
							PipelineScan,
							PipelineIdle,
							PipelineRender
						},
						new Action[]
						{	// 57
							PipelineScan,
							PipelineSpriteDMA,
							PipelineRCReset,
							PipelineFetchSprite0P,
							PipelineRender
						},
						new Action[]
						{	// 58
							PipelineScan,
							PipelineFetchSprite0S,
							PipelineRender
						},
						new Action[]
						{	// 59
							PipelineScan,
							PipelineFetchSprite1P,
							PipelineRender
						},
						new Action[]
						{	// 60
							PipelineScan,
							PipelineFetchSprite1S,
							PipelineRender
						},
						new Action[]
						{	// 61
							PipelineScan,
							PipelineFetchSprite2P,
							PipelineRender
						},
						new Action[]
						{	// 62
							PipelineScan,
							PipelineFetchSprite2S,
							PipelineBorderCheck,
							PipelineRender
						}
					};
					break;
			}

			pipelineLength = pipeline.Length;
		}

		private void PipelineBorderCheck()
		{
			if ((RASTER == borderTop) && (DEN))
				borderOnVertical = false;
			if (RASTER == borderBottom)
				borderOnVertical = true;
		}

		private void PipelineDramRefresh()
		{
			mem.VicRead((ushort)refreshAddress);
			refreshAddress = (refreshAddress - 1) & 0xFF;
			refreshAddress |= 0x3F00;
		}

		private void PipelineFetchC()
		{
			int cAddress = (VM << 10) | VC;
			characterDataBus = mem.VicRead((ushort)cAddress);
			colorDataBus = mem.colorRam[VC];
		}

		private void PipelineFetchCIdle()
		{
			characterDataBus = 0;
			colorDataBus = 0;
		}

		private void PipelineFetchG()
		{
			switch (graphicsMode)
			{
				case 0: // 000
				case 1: // 001
					PipelineFetchG000();
					break;
				case 2: // 010
				case 3: // 011
					PipelineFetchG010();
					break;
				case 4: // 100
				case 5: // 101
					PipelineFetchG100();
					break;
				case 6: // 110
				case 7: // 111
					PipelineFetchG110();
					break;
			}
		}

		private void PipelineFetchG000()
		{
			int gAddress = (CB << 11) | (characterData << 3) | RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		private void PipelineFetchG010()
		{
			int gAddress = ((CB & 0x4) << 11) | (VC << 3) | RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		private void PipelineFetchG100()
		{
			int gAddress = (CB << 11) | ((characterData & 0x3F) << 3) | RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		private void PipelineFetchG110()
		{
			int gAddress = ((CB & 0x4) << 11) | ((VC & 0x33F) << 3) | RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		private void PipelineFetchGIdle()
		{
			mem.VicRead(ECM ? (ushort)0x39FF : (ushort)0x3FFF); 
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
				sprites[index].MSRC = 24;
				sprites[index].MSR = mem.VicRead((ushort)((sprites[index].MPTR << 6) | (sprites[index].MC)));
				sprites[index].MC++;
			}

			signal.VicAEC = !sprites[index].MDMA;
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
			}

			signal.VicAEC = !sprites[index].MDMA;
		}

		private void PipelineIdle()
		{
		}

		private void PipelineIRQ0()
		{
		}

		private void PipelineIRQ1()
		{
		}

		private void PipelinePlot000()
		{
		}

		private void PipelinePlot001()
		{
		}

		private void PipelinePlot010()
		{
		}

		private void PipelinePlot011()
		{
		}

		private void PipelinePlot100()
		{
		}

		private void PipelinePlot101()
		{
		}

		private void PipelinePlot110()
		{
		}

		private void PipelinePlot111()
		{
		}
		private void PipelineRCReset()
		{
		}

		private void PipelineRender()
		{
		}

		private void PipelineScan()
		{
		}

		private void PipelineSpriteDMA()
		{
		}

		private void PipelineSpriteEnable0()
		{
		}

		private void PipelineSpriteEnable1()
		{
		}

		private void PipelineSpriteMCBASEAdvance()
		{
		}

		private void PipelineSpriteMYEFlip()
		{
		}

		private void PipelineVCReset()
		{
		}
	}
}
