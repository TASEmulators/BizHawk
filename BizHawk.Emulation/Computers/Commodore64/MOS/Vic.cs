using System;
using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Vic
	{
        public Func<int, byte> ReadColorRam;
        public Func<int, byte> ReadMemory;

        public bool ReadAECBuffer() { return pinAEC; }
        public bool ReadBABuffer() { return pinBA; }
        public bool ReadIRQBuffer() { return pinIRQ; }

        protected int cyclesPerSec;
		protected int[][] pipeline;
		protected int totalCycles;
		protected int totalLines;

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
	}
}
