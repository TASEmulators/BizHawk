using System;
using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	sealed public partial class Vic
	{
        public Func<int, byte> ReadColorRam;
        public Func<int, byte> ReadMemory;

        public bool ReadAECBuffer() { return pinAEC; }
        public bool ReadBABuffer() { return pinBA; }
        public bool ReadIRQBuffer() { return pinIRQ; }

        int cyclesPerSec;
        int irqShift;
		int[][] pipeline;
		int totalCycles;
		int totalLines;

		public Vic(int newCycles, int newLines, int[][] newPipeline, int newCyclesPerSec, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd)
		{
            {
                debugScreen = false;

                this.hblankStart = hblankStart;
                this.hblankEnd = hblankEnd;
                this.vblankStart = vblankStart;
                this.vblankEnd = vblankEnd;

                totalCycles = newCycles;
				totalLines = newLines;
				pipeline = newPipeline;
				cyclesPerSec = newCyclesPerSec;
				//pixelBufferDelay = 4;
				//pixelBackgroundBufferDelay = 12;

                bufWidth = TimingBuilder_ScreenWidth(pipeline[0], hblankStart, hblankEnd);
                bufHeight = TimingBuilder_ScreenHeight(vblankStart, vblankEnd, newLines);

                buf = new int[bufWidth * bufHeight];
				bufLength = buf.Length;

				sprites = new Sprite[8];
				for (int i = 0; i < 8; i++)
					sprites[i] = new Sprite();

				bufferC = new int[40];
				bufferG = new int[40];
				//pixelBuffer = new int[pixelBufferDelay];
				//pixelDataBuffer = new int[pixelBufferDelay];
				//pixelBackgroundBuffer = new int[pixelBackgroundBufferDelay];
			}
		}

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

        public void ExecutePhase1()
        {
            //xScroll = 1;
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
                {
                    badline = true;
                }
                else
                {
                    badline = false;
                }

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

                ParseCycle();

                //xOffset = 0;
                Render();

                // if the BA counter is nonzero, allow CPU bus access
                UpdateBA();
                pinAEC = false;

                // must always come last
                //UpdatePins();
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

                    if (rasterLine == vblankStart)
                        vblank = true;
                    if (rasterLine == vblankEnd)
                        vblank = false;

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

        private void UpdateBA()
		{
			if (pinBA)
				baCount = baResetCounter;
			else if (baCount > 0)
				baCount--;
		}

		private void UpdateBorder()
		{
			borderL = columnSelect ? 0x018 : 0x01F;
			borderR = columnSelect ? 0x158 : 0x14F;
            //borderL = columnSelect ? 28 : 35;
            //borderR = columnSelect ? 348 : 339;
			borderT = rowSelect ? 0x033 : 0x037;
			borderB = rowSelect ? 0x0FB : 0x0F7;
		}

		private void UpdatePins()
		{
			bool irqTemp = !(
				(enableIntRaster & intRaster) |
				(enableIntSpriteDataCollision & intSpriteDataCollision) |
				(enableIntSpriteCollision & intSpriteCollision) |
				(enableIntLightPen & intLightPen));

            irqShift <<= 1;
            irqShift |= (irqTemp ? 0x1 : 0x0);
            pinIRQ = (irqShift & 0x1) != 0;
		}

        private void UpdateVideoMode()
        {
            if (!extraColorMode && !bitmapMode && !multicolorMode)
            {
                videoMode = VicVideoMode.Mode000;
                return;
            }
            else if (!extraColorMode && !bitmapMode && multicolorMode)
            {
                videoMode = VicVideoMode.Mode001;
                return;
            }
            else if (!extraColorMode && bitmapMode && !multicolorMode)
            {
                videoMode = VicVideoMode.Mode010;
                return;
            }
            else if (!extraColorMode && bitmapMode && multicolorMode)
            {
                videoMode = VicVideoMode.Mode011;
                return;
            }
            else if (extraColorMode && !bitmapMode && !multicolorMode)
            {
                videoMode = VicVideoMode.Mode100;
                return;
            }
            videoMode = VicVideoMode.ModeBad;
        }
	}
}
