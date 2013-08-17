using System;
using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Vic
	{
		// ------------------------------------

		private int baCount;
		private bool badline;
		private bool badlineEnable;
		private int bitmapColumn;
		private int borderB;
		private bool borderCheckLEnable;
		private bool borderCheckREnable;
		private int borderL;
		private bool borderOnMain;
		private bool borderOnVertical;
		private int borderR;
		private int borderT;
		private int[] bufferC;
		private int[] bufferG;
		private int cycle;
		private int cycleIndex;
		private int dataC;
		private int dataG;
        private bool debugScreen;
        private int displayC;
		private int displayIndex;
		private bool idle;
		private int lastRasterLine;
        private int pixel;
		private int[] pixelBackgroundBuffer;
		private int pixelBackgroundBufferDelay;
		private int pixelBackgroundBufferIndex;
		private int[] pixelBuffer;
		private int pixelBufferDelay;
		private int pixelBufferIndex;
        private int pixelData;
        private int[] pixelDataBuffer;
		private int rc;
		private int refreshCounter;
        private bool renderEnabled;
		private int sr;
		private int vc;
		private int vcbase;
		private int vmli;
		private int xOffset;

		// ------------------------------------

		private int cyclesPerSec;
		private bool pinAEC = true;
        private bool pinBA = true;
        private bool pinIRQ = true;
		private int[][] pipeline;
		private int totalCycles;
		private int totalLines;

		// ------------------------------------

		public Func<int, byte> ReadColorRam;
		public Func<int, byte> ReadMemory;
	
		// ------------------------------------

		public Vic(int newCycles, int newLines, int[][] newPipeline, int newCyclesPerSec)
		{
			
			{
                debugScreen = false;

                totalCycles = newCycles;
				totalLines = newLines;
				pipeline = newPipeline;
				cyclesPerSec = newCyclesPerSec;
				pixelBufferDelay = 12;
				pixelBackgroundBufferDelay = 4;

                if (debugScreen)
                {
                    bufRect = new Rectangle(0, 0, totalCycles * 8, totalLines);
                }
                else
                {
                    bufRect = new Rectangle(136 - 24, 51 - 24, 320 + 48, 200 + 48);
                }

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

		protected const int baResetCounter = 6;

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

		// ------------------------------------

        public bool ReadAECBuffer() { return pinAEC; }
        public bool ReadBABuffer() { return pinBA; }
        public bool ReadIRQBuffer() { return pinIRQ; }

		// ------------------------------------

		public int CyclesPerFrame
		{
			get
			{
				return (totalCycles * totalLines);
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
