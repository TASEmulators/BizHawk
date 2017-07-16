using System;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

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
			public byte[] obj; // up to 32 bytes can compose one object
		}

		// technically there is no limit on he number of graphics objects, but since dma is automatically killed
		// at the end of a scanline, we have an effective limit
		GFX_Object[] GFX_Objects = new GFX_Object[128];

		public int _frameHz = 60;
		public int _screen_width = 320;
		public int _screen_height = 263;
		public int _vblanklines = 20;

		public int[] _vidbuffer;
		public int[] _palette;
		public int[] scanline_buffer = new int[320];

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 320;
		public int VirtualHeight => _screen_height - _vblanklines;
		public int BufferWidth => 320;
		public int BufferHeight => _screen_height - _vblanklines;
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		// the Maria chip can directly access memory
		public Func<ushort, byte> ReadMemory;

		public int cycle;
		public int scanline;
		public int DLI_countdown;
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
		public int graphics_read_time = 3; // depends on content of graphics header
		public int DMA_phase_next;

		public ushort display_zone_pointer;
		public int display_zone_counter;

		public byte current_DLL_offset;
		public ushort current_DLL_addr;
		public bool current_DLL_DLI;
		public bool current_DLL_H16;
		public bool current_DLL_H8;

		public int header_counter;
		public int header_counter_max;
		public int header_pointer; // since headers could be 4 or 5 bytes, we need a seperate pointer

		// write mode is actually persistent but exists outside of the regs
		public bool global_write_mode; 

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
			// Since long shut down loads up the next zone, this basically loads up the DLL for the first zone
			sl_DMA_complete = false;
			do_dma = false;
			
			for (int i=0; i<454;i++)
			{
				if(i==0 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(5))
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
				else if (sl_DMA_complete && current_DLL_DLI && !Core.cpu_is_halted)
				{
					// schedule an NMI for one maria tick into the future
					// (but set to 2 since it decrements immediately)
					DLI_countdown = 2;
					current_DLL_DLI = false;
				}

				if (DLI_countdown > 0)
				{
					DLI_countdown--;
					if (DLI_countdown == 0)
					{
						Core.cpu.NMI = true;
					}
				}

				Core.RunCPUCycle();
			}

			scanline++;
			cycle = 0;
			do_dma = false;
			sl_DMA_complete = false;
			Core.cpu.RDY = true;
			Core.Maria_regs[8] = 0; // we have now left VBLank


			// Now proceed with the remaining scanlines
			// the first one is a pre-render line, since we didn't actually put any data into the buffer yet
			while (scanline < 263)
			{
				
				if (cycle == 28 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(5))
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
				else if (sl_DMA_complete && current_DLL_DLI && !Core.cpu_is_halted)
				{
					// schedule an NMI for one maria tick into the future
					// (but set to 2 since it decrements immediately)
					DLI_countdown = 2;
					current_DLL_DLI = false;
				}

				if (DLI_countdown > 0)
				{
					DLI_countdown--;
					if (DLI_countdown == 0)
					{
						Core.cpu.NMI = true;
					}
				}

				Core.RunCPUCycle();

				cycle++;

				if (cycle == 454)
				{
					if (scanline > 20)
					{
						// add the current graphics to the buffer
						draw_scanline(scanline - 21);
					}
					//Console.Write("Scanline");
					//Console.WriteLine(scanline - 21);
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
							if (!temp.Bit(6))
							{
								// at the end of the list, time to end the DMA
								// check if we are at the end of the zone
								if (current_DLL_offset == 0)
								{
									DMA_phase_next = DMA_SHUTDOWN_LAST;
								}
								else
								{
									DMA_phase_next = DMA_SHUTDOWN_OTHER;
								}
								header_read_time = 8;
								header_pointer++;
							}
							else
							{
								// we are in 5 Byte header mode (i.e. using the character map)
								GFX_Objects[header_counter].write_mode = temp.Bit(7);
								global_write_mode = temp.Bit(7);
								GFX_Objects[header_counter].ind_mode = temp.Bit(5);
								header_pointer++;
								temp = (byte)(ReadMemory((ushort)(current_DLL_addr + header_pointer)));
								GFX_Objects[header_counter].addr |= (ushort)(temp << 8);
								header_pointer++;
								temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								int temp_w = (temp & 0x1F); // this is the 2's complement of width (for reasons that escape me)

								if (temp_w == 0)
								{
									// important note here. In 5 byte mode, width 0 actually counts as 32
									GFX_Objects[header_counter].width = 32;
								}
								else
								{
									temp_w = (temp_w - 1);
									temp_w = (0x1F - temp_w);
									GFX_Objects[header_counter].width = (byte)(temp_w & 0x1F);
								}

								GFX_Objects[header_counter].palette = (byte)((temp & 0xE0) >> 5);
								header_pointer++;
								GFX_Objects[header_counter].h_pos = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								header_pointer++;

								DMA_phase_next = DMA_GRAPHICS;

								header_read_time = 10;
							}
						}
						else
						{
							int temp_w = (temp & 0x1F); // this is the 2's complement of width (for reasons that escape me)
							temp_w = (temp_w - 1);
							temp_w = (0x1F - temp_w);
							GFX_Objects[header_counter].width = (byte)(temp_w & 0x1F);

							GFX_Objects[header_counter].palette = (byte)((temp & 0xE0) >> 5);
							header_pointer++;
							temp = (byte)(ReadMemory((ushort)(current_DLL_addr + header_pointer)));
							GFX_Objects[header_counter].addr |= (ushort)(temp << 8);
							header_pointer++;
							GFX_Objects[header_counter].h_pos = ReadMemory((ushort)(current_DLL_addr + header_pointer));
							header_pointer++;

							DMA_phase_next = DMA_GRAPHICS;

							GFX_Objects[header_counter].write_mode = global_write_mode;

							GFX_Objects[header_counter].ind_mode = false;

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
						ushort addr_t = 0;

						// in 5 byte mode, we first have to check if we are in direct or indirect mode
						if (GFX_Objects[header_counter].ind_mode)
						{
							int ch_size = 0;

							if (Core.Maria_regs[0x1C].Bit(4))
							{
								graphics_read_time = 9 * GFX_Objects[header_counter].width + 3;
								ch_size = 2;
							}
							else
							{
								graphics_read_time = 6 * GFX_Objects[header_counter].width + 3;
								ch_size = 1;
							}

							// the address here is specified by CHAR_BASE maria registers
							// ushort addr = (ushort)(GFX_Objects[header_counter].addr & 0xFF);
							for (int i = 0; i < GFX_Objects[header_counter].width; i++)
							{
								addr_t = ReadMemory((ushort)(GFX_Objects[header_counter].addr + i));
								addr_t |= (ushort)((Core.Maria_regs[0x14] + current_DLL_offset) << 8);

								if ((current_DLL_H16 && addr_t.Bit(12)) || (current_DLL_H8 && addr_t.Bit(11)))
								{
									if (i * ch_size < 32)
									{
										GFX_Objects[header_counter].obj[i * ch_size] = 0;
									}
									if ((i * ch_size + 1 < 32) && (ch_size == 2))
									{
										GFX_Objects[header_counter].obj[i * ch_size + 1] = 0;
									}
									if (ch_size == 1)
									{
										graphics_read_time -= 6;
									}
									else
									{
										graphics_read_time -= 9;
									}

								}
								else
								{
									if (i * ch_size < 32)
									{
										GFX_Objects[header_counter].obj[i * ch_size] = ReadMemory(addr_t);
									}
									if ((i * ch_size + 1 < 32) && (ch_size == 2))
									{
										GFX_Objects[header_counter].obj[i * ch_size + 1] = ReadMemory(addr_t);
									}
								}
							}
						}
						else
						{
							graphics_read_time = 3 * GFX_Objects[header_counter].width;

							for (int i = 0; i < GFX_Objects[header_counter].width; i++)
							{
								addr_t = (ushort)(GFX_Objects[header_counter].addr + (current_DLL_offset << 8) + i);
								if ((current_DLL_H16 && addr_t.Bit(12)) || (current_DLL_H8 && addr_t.Bit(11)))
								{
									GFX_Objects[header_counter].obj[i] = 0;
									graphics_read_time -= 3;
								}
								else
								{
									GFX_Objects[header_counter].obj[i] = ReadMemory(addr_t);
								}
							}
						}
					}

					if (DMA_phase_counter == graphics_read_time || graphics_read_time == 0)
					{
						// We have read the graphics data, for this header, now return to the header list 
						// This loop will continue until a header indicates its time to stop
						DMA_phase = DMA_HEADER;
						DMA_phase_counter = 0;
					}
					return;
				}

				if (DMA_phase == DMA_SHUTDOWN_OTHER)
				{
					Core.cpu_resume_pending = true;
					sl_DMA_complete = true;
					current_DLL_offset -= 1; // this is reduced by one for each scanline, which changes where graphics are fetched
					header_counter_max = header_counter;
					header_counter = -1;
					header_pointer = 0;
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

						current_DLL_offset = (byte)(temp & 0xF);
						current_DLL_DLI = temp.Bit(7);
						current_DLL_H16 = temp.Bit(6);
						current_DLL_H8 = temp.Bit(5);

						header_counter_max = header_counter;
						header_counter = -1;
						header_pointer = 0;
					}
					return;
				}
			}
		}

		public void draw_scanline(int scanline)
		{
			int local_start;
			int local_width;
			int local_palette;
			int index;
			int color;

			int disp_mode = Core.Maria_regs[0x1C] & 0x3;

			int temp_bg = Core.Maria_regs[0];


			for (int i = 0; i < 320; i++)
			{
				scanline_buffer[i] = _palette[temp_bg];
			}

			for (int i = 0; i < header_counter_max; i++)
			{
				local_start = GFX_Objects[i].h_pos;
				local_palette = GFX_Objects[i].palette;

				// the two different rendering paths are basically controlled by write mode
				if (GFX_Objects[i].write_mode)
				{

				}
				else
				{
					if (disp_mode == 0)
					{
						local_width = GFX_Objects[i].width;

						for (int j = 0; j < local_width; j++)
						{
							for (int k = 7; k >= 0; k--)
							{
								index = local_start * 2 + j * 8 + (7 - k);

								if (index > 511) index -= 512;
								if (index < 320)
								{
									color = GFX_Objects[i].obj[j];

									// this is now the color index (0-3) we choose from the palette
									if (k>=6)
									{
										color = (color >> 6) & 0x3; 

									}
									else if (k>=4)
									{
										color = (color >> 4) & 0x3; 

									}
									else if (k>=2)
									{
										color = (color >> 2) & 0x3;
									}
									else
									{
										color = color & 0x3;
									}
									
									if (color != 0) // transparent
									{
										color = Core.Maria_regs[local_palette * 4 + color];

										// the top 4 bits from this are the color, the bottom 4 are the luminosity
										// this is already conveniently arranged in the palette
										scanline_buffer[index] = _palette[color];
									}
								}
							}
						}
					}
					else if (disp_mode == 2) // note: 1 is not used
					{
						local_width = GFX_Objects[i].width;
						// here the palette is determined by palette bit 2 only
						// hence only palette 0 or 4 is available
						local_palette = GFX_Objects[i].palette & 0x4;

						int temp_c0 = GFX_Objects[i].palette & 0x1;
						int temp_c1 = GFX_Objects[i].palette & 0x2;

						for (int j = 0; j < local_width; j++)
						{
							for (int k = 7; k >= 0; k--)
							{
								color = (GFX_Objects[i].obj[j] >> k) & 1;
								color = (color << 1) | ((k % 2 == 0) ? temp_c0 : temp_c1);
								index = local_start * 2 + j * 8 + (7 - k);
								if (index > 511) index -= 512;

								if (index < 320)
								{

									color = Core.Maria_regs[local_palette + color];

									// the top 4 bits from this are the color, the bottom 4 are the luminosity
									// this is already conveniently arranged in the palette
									scanline_buffer[index] = _palette[color];
								}
							}
						}

					}
					else
					{
						local_width = GFX_Objects[i].width;

						for (int j = 0; j < local_width; j++)
						{
							for (int k = 7; k >= 0; k--)
							{
								color = (GFX_Objects[i].obj[j] >> k) & 1;
								index = local_start * 2 + j * 8 + (7 - k);
								if (index > 511) index -= 512;
								if (index < 320 && color == 1)
								{
									color = Core.Maria_regs[local_palette * 4 + 2]; // automatically use index 2 here

									// the top 4 bits from this are the color, the bottom 4 are the luminosity
									// this is already conveniently arranged in the palette
									scanline_buffer[index] = _palette[color];
								}
							}
						}
					}
				}
			}
	
			// send buffer to the video buffer
			for (int i = 0; i < 320; i ++)
			{
				_vidbuffer[scanline * 320 + i] = scanline_buffer[i];
			}
		}

		public void Reset()
		{
			_vidbuffer = new int[VirtualWidth * VirtualHeight];

			for (int i = 0; i < 128; i++)
			{
				GFX_Objects[i].obj = new byte[32];
			}
		}

		// Most of the Maria state is captured in Maria Regs in the core
		// Only write Mode is persistent and outside of the regs
		// also since DMA is always killed at scanline boundaries, most related check variables are also not needed
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Maria");

			ser.Sync("global write mode", ref global_write_mode);

			ser.EndSection();
		}
	}
}
