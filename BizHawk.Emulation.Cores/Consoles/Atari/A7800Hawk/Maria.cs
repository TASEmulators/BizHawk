using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the Atari 7800 Maria graphics chip
	public class Maria : IVideoProvider
	{
		public A7800Hawk Core { get; set; }

		public int _frameHz = 60;
		public int _screen_width = 320;
		public int _screen_height = 263;

		public int[] _vidbuffer;
		public int[] _palette;

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 320;
		public int VirtualHeight => _screen_height;
		public int BufferWidth => 320;
		public int BufferHeight => _screen_height;
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		// the Maria chip can directly access memory
		public Func<ushort, byte> ReadMemory;

		public int cycle;
		public int scanline;
		public bool sl_DMA_complete;

		public int DMA_phase = 0;
		public int DMA_phase_counter;

		public static int DMA_START_UP = 0;
		public static int DMA_HEADER = 1;
		public static int DMA_GRAPHICS = 2;
		public static int DMA_CHAR_MAP = 3;
		public static int DMA_SHUTDOWN_OTHER = 4;
		public static int DMA_SHUTDOWN_LAST = 5;

		public byte list_low_byte;
		public byte list_high_byte;



		// each frame contains 263 scanlines
		// each scanline consists of 113.5 CPU cycles (fast access) which equates to 454 Maria cycles
		// In total there are 29850.5 CPU cycles (fast access) in a frame
		public void RunFrame()
		{
			scanline = 0;

			Core.Maria_regs[8] = 0x80; // indicates VBlank state

			// we start off in VBlank for 20 scanlines
			// at the end of vblank is a DMA to set up the display for the start of drawing
			// this is free time for the CPU to set up display lists
			while (scanline < 19)
			{
				Core.RunCPUCycle();
				cycle++;

				if (cycle == 454)
				{
					scanline++;
					cycle = 0;
					Core.tia._hsyncCnt = 0;
					Core.cpu.RDY = true;
				}

			}

			// "The end of vblank is made up of a DMA startup plus a long shut down"
			sl_DMA_complete = false;
			DMA_phase = DMA_START_UP;
			DMA_phase_counter = 0;

			for (int i=0; i<454;i++)
			{
				if (i<28)
				{
					// DMA doesn't start until 7 CPU cycles into a scanline
				}
				else if (i==28)
				{
					Core.cpu_halt_pending = true;
					DMA_phase_counter = 0;
				}
				else if (!sl_DMA_complete)
				{
					RunDMA(i - 28, true);
				}

				Core.RunCPUCycle();
			}

			scanline++;
			cycle = 0;

			Core.Maria_regs[8] = 0; // we have now left VBLank

			// Now proceed with the remaining scanlines
			// the first one is a pre-render line, since we didn't actually put any data into the buffer yet
			while (scanline < 263)
			{


				Core.RunCPUCycle();

				cycle++;

				if (cycle == 454)
				{
					scanline++;
					cycle = 0;
					Core.tia._hsyncCnt = 0;
					Core.cpu.RDY = true;
				}
			}
		}

		public void RunDMA(int c, bool short_dma)
		{
			// During DMA the CPU is HALTED, This appears to happen on the falling edge of Phi2
			// Current implementation is that a HALT request must be acknowledged in phi1
			// if the CPU is now in halted state, start DMA
			if (Core.cpu_is_halted)
			{
				DMA_phase_counter++;

				if (DMA_phase_counter==2 && DMA_phase==DMA_START_UP)
				{
					DMA_phase_counter = 0;
					if (short_dma)
						DMA_phase = DMA_SHUTDOWN_LAST;
					else
					{
						DMA_phase = DMA_HEADER;
					}

					return;
				}

				if (DMA_phase == DMA_HEADER)
				{
					// get all the data from the display list header
					if (DMA_phase_counter==1)
					{

					}
				}

				if (DMA_phase == DMA_SHUTDOWN_LAST)
				{
					if (DMA_phase_counter==4)
					{
						Core.cpu_resume_pending = true;
						sl_DMA_complete = true;
					}
					return;
				}


			}


		}

		public void Reset()
		{
			_vidbuffer = new int[VirtualWidth * VirtualHeight];
		}

	}
}
