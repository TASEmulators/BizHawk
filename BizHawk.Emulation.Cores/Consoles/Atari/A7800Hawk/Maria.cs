using System;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the Atari 7800 Maria graphics chip
	public class Maria : IVideoProvider
	{
		public A7800Hawk Core { get; set; }

		struct GFX_Object
		{
			public byte palette;
			public byte width;
			public ushort addr;
			public byte h_pos;

			// additional entries used only in 5-byte header mode
			public bool write_mode;
			public bool ind_mode;
			public bool exp_mode;
		}

		// technically there is no limit on he number of graphics objects, but since dma is automatically killed
		// at the end of a scanline, we have an effective limit
		GFX_Object[] GFX_Objects = new GFX_Object[64];

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
		public bool do_dma;

		public int DMA_phase = 0;
		public int DMA_phase_counter;

		public static int DMA_START_UP = 0;
		public static int DMA_HEADER = 1;
		public static int DMA_GRAPHICS = 2;
		public static int DMA_CHAR_MAP = 3;
		public static int DMA_SHUTDOWN_OTHER = 4;
		public static int DMA_SHUTDOWN_LAST = 5;

		public int header_read_time = 8; // default for 4 byte headers (10 for 5 bytes ones)
		public int DMA_phase_next;
		public int base_scanline;

		public ushort display_zone_pointer;
		public int display_zone_counter;

		public byte current_DLL_offset;
		public ushort current_DLL_addr;
		public bool current_DLL_DLI;
		public bool current_DLL_H16;
		public bool current_DLL_H8;

		public int header_counter;
		public int header_pointer; // since headers could be 4 or 5 bytes, we need a seperate pointer

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
			do_dma = false;

			for (int i=0; i<454;i++)
			{
				if (i<28)
				{
					// DMA doesn't start until 7 CPU cycles into a scanline
				}
				else if (i==28 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(6))
				{
					Core.cpu_halt_pending = true;
					DMA_phase = DMA_START_UP;
					DMA_phase_counter = 0;
					do_dma = true;
				}
				else if (!sl_DMA_complete && do_dma)
				{
					RunDMA(true);
				}

				Core.RunCPUCycle();
			}

			scanline++;
			cycle = 0;
			do_dma = false;
			Core.Maria_regs[8] = 0; // we have now left VBLank'
			base_scanline = 0;
			sl_DMA_complete = false;

			// Now proceed with the remaining scanlines
			// the first one is a pre-render line, since we didn't actually put any data into the buffer yet
			while (scanline < 263)
			{
				
				if (cycle < 28)
				{
					// DMA doesn't start until 7 CPU cycles into a scanline
				}
				else if (cycle == 28 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(6))
				{
					Core.cpu_halt_pending = true;
					DMA_phase = DMA_START_UP;
					DMA_phase_counter = 0;
					do_dma = true;
				}
				else if (!sl_DMA_complete && do_dma)
				{
					RunDMA(false);
				}

				Core.RunCPUCycle();

				cycle++;

				if (cycle == 454)
				{
					scanline++;
					cycle = 0;
					Core.tia._hsyncCnt = 0;
					Core.cpu.RDY = true;
					do_dma = false;
					sl_DMA_complete = false;
				}
			}
		}

		public void RunDMA(bool short_dma)
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
					{
						DMA_phase = DMA_SHUTDOWN_LAST;

						// also here we load up the display list list
						// is the timing correct?
						display_zone_pointer = (ushort)((Core.Maria_regs[0xC] << 8) | Core.Maria_regs[0x10]);
						display_zone_counter = -1;
					}
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
						header_counter++;
						GFX_Objects[header_counter].addr = ReadMemory((ushort)(current_DLL_addr + header_pointer));
						header_pointer++;
						byte temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
						// if there is no width, then we must have an extended header
						// or at the end of this list
						if ((temp & 0x1F) == 0)
						{
							if ((temp & 0xE0) == 0)
							{
								// at the end of the list, time to end the DMA
								// check if we are at the end of the zone
								if (scanline == base_scanline + current_DLL_offset)
								{
									DMA_phase_next = DMA_SHUTDOWN_LAST;
								}
								else
								{
									DMA_phase_next = DMA_SHUTDOWN_OTHER;
								}
								header_read_time = 8;
							}
							else
							{
								// we are in 5 Byte header mode
								GFX_Objects[header_counter].write_mode = temp.Bit(7);
								GFX_Objects[header_counter].ind_mode = temp.Bit(5);
								header_pointer++;
								temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								GFX_Objects[header_counter].addr |= (ushort)(temp << 8);
								header_pointer++;
								temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								GFX_Objects[header_counter].width = (byte)(temp & 0x1F);
								GFX_Objects[header_counter].palette = (byte)((temp & 0xE0) >> 5);
								header_pointer++;
								GFX_Objects[header_pointer].h_pos = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								header_pointer++;

								GFX_Objects[header_pointer].exp_mode = true;
								DMA_phase_next = DMA_GRAPHICS;

								header_read_time = 10;
							}
						}
						else
						{
							GFX_Objects[header_counter].width = (byte)(temp & 0x1F);
							GFX_Objects[header_counter].palette = (byte)((temp & 0xE0) >> 5);
							header_pointer++;
							temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
							GFX_Objects[header_counter].addr |= (ushort)(temp << 8);
							header_pointer++;
							GFX_Objects[header_pointer].h_pos = ReadMemory((ushort)(current_DLL_addr + header_pointer));
							header_pointer++;

							GFX_Objects[header_pointer].exp_mode = false;
							DMA_phase_next = DMA_GRAPHICS;

							header_read_time = 8;
						}

					}
					else if (DMA_phase_counter == header_read_time)
					{
						DMA_phase_counter = 0;
						DMA_phase = DMA_phase_next;
					}
					return;
				}

				if (DMA_phase == DMA_GRAPHICS)
				{
					if (DMA_phase_counter == 1)
					{
						// get all the graphics data
					}

					if (DMA_phase_counter == 3)
					{
						DMA_phase = DMA_SHUTDOWN_OTHER;
						DMA_phase_counter = 0;
					}
					return;
				}

				if (DMA_phase == DMA_SHUTDOWN_OTHER)
				{
					Core.cpu_resume_pending = true;
					sl_DMA_complete = true;
					return;
				}

				if (DMA_phase == DMA_SHUTDOWN_LAST)
				{
					if (DMA_phase_counter==6)
					{
						Core.cpu_resume_pending = true;
						sl_DMA_complete = true;

						// on the last line of a zone, we load up the disply list list for the next zone.
						display_zone_counter++;
						ushort temp_addr = (ushort)(display_zone_pointer + 3 * display_zone_counter);
						byte temp = ReadMemory(temp_addr);
						current_DLL_addr = (ushort)(ReadMemory((ushort)(temp_addr + 1)) << 8);
						current_DLL_addr |= ReadMemory((ushort)(temp_addr + 2));

						current_DLL_offset = (byte)(temp & 0xF + 1);
						current_DLL_DLI = temp.Bit(7);
						current_DLL_H16 = temp.Bit(6);
						current_DLL_H8 = temp.Bit(5);

						header_counter = -1;
						header_pointer = 0;
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
