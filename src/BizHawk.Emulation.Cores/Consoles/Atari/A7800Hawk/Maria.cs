using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the Atari 7800 Maria graphics chip
	public sealed class Maria
	{
		public A7800Hawk Core { get; set; }

		private struct GFX_Object
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

		// technically there is no limit on the number of graphics objects, but since dma is automatically killed
		// at the end of a scanline, we have an effective limit
		private readonly GFX_Object[] GFX_Objects = new GFX_Object[128];

		public byte[,] line_ram = new byte[2, 320];
		private byte temp_check = 0;

		private int GFX_index = 0;

		public int[] _palette;
		public int[] scanline_buffer = new int[320];

		// the Maria chip can directly access memory
		public Func<ushort, byte> ReadMemory;

		public int cycle;
		public int scanline;
		public int DLI_countdown;
		public bool sl_DMA_complete;
		public bool do_dma;

		public int DMA_phase = 0;
		public int DMA_phase_counter;

		public const int DMA_START_UP = 0;
		public const int DMA_HEADER = 1;
		public const int DMA_GRAPHICS = 2;
		public const int DMA_CHAR_MAP = 3;
		public const int DMA_SHUTDOWN_OTHER = 4;
		public const int DMA_SHUTDOWN_LAST = 5;

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

		public bool global_write_mode;

		public int header_counter;
		public int header_pointer; // since headers could be 4 or 5 bytes, we need a seperate pointer
		public ushort addr_t;
		public int ch_size;
		public int scan_index;
		public int read_time;
		public bool zero_hole; // the first DMA hole does not save cycles

		// variables for drawing a pixel
		private int color;
		private int local_GFX_index;
		private int temp_palette;
		private int temp_bit_0;
		private int temp_bit_1;
		private int disp_mode;
		private byte BG_latch_1;
		private int pixel;

		// each frame contains 263 scanlines
		// each scanline consists of 113.5 CPU cycles (fast access) which equates to 454 Maria cycles
		// In total there are 29850.5 CPU cycles (fast access) in a frame
		public void RunFrame()
		{
			scanline = 0;
			global_write_mode = false;
			Core.Maria_regs[8] = 0x80; // indicates VBlank state

			// we start off in VBlank for 20 scanlines
			// at the end of vblank is a DMA to set up the display for the start of drawing
			// this is free time for the CPU to set up display lists
			while (scanline < 20)
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
			Core.Maria_regs[8] = 0; // we have now left VBLank

			for (int i=0; i<454;i++)
			{
				if(i==28 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(5))
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

			// Now proceed with the remaining scanlines
			// the first one is a pre-render line, since we didn't actually put any data into the buffer yet
			while (scanline < Core._screen_height)
			{				
				if (cycle == 28 && Core.Maria_regs[0x1C].Bit(6) && !Core.Maria_regs[0x1C].Bit(5))
				{
					Core.cpu_halt_pending = true;
					DMA_phase = DMA_START_UP;
					DMA_phase_counter = 0;
					do_dma = true;
					sl_DMA_complete = false;
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

				if (cycle == 453 && !sl_DMA_complete && do_dma && (DMA_phase == DMA_GRAPHICS || DMA_phase == DMA_HEADER))
				{
					DMA_phase = current_DLL_offset == 0
						? DMA_SHUTDOWN_LAST
						: DMA_SHUTDOWN_OTHER;

					DMA_phase_counter = 0;
				}
				
				Core.RunCPUCycle();

				//////////////////////////////////////////////
				// Drawing Start
				//////////////////////////////////////////////

				if (cycle >=133 && cycle < 453  && scanline > 20)
				{
					pixel = cycle - 133;
					local_GFX_index = (GFX_index == 1) ? 0 : 1; // whatever the current index is, we use the opposite

					if (cycle == 133)
					{
						disp_mode = Core.Maria_regs[0x1C] & 0x3;
					}
					BG_latch_1 = Core.Maria_regs[0];

					color = line_ram[local_GFX_index, pixel];

					if (disp_mode == 0)
					{
						// direct read, nothing to do
					}
					else if (disp_mode == 2) // note: 1 is not used
					{
						// there is a trick here to be aware of.
						// the renderer has no concept of objects, as it only has information on each pixel
						// but objects are specified in groups of 8 pixels. 
						// however, since objects can only be placed in 160 resolution
						// we can pick bits based on whether the current pixel is even or odd
						temp_palette = color & 0x10;
						temp_bit_0 = 0;
						temp_bit_1 = 0;

						if (pixel % 2 == 0)
						{
							temp_bit_1 = color & 2;
							temp_bit_0 = (color & 8) >> 3;
						}
						else
						{
							temp_bit_1 = (color & 1) << 1;
							temp_bit_0 = (color & 4) >> 2;
						}

						color = temp_palette + temp_bit_1 + temp_bit_0;
					}
					else
					{
						// same as above, we can use the pixel index to pick the bits out
						if (pixel % 2 == 0)
						{
							color &= 0x1E;
						}
						else
						{
							color = (color & 0x1C) + ((color & 1) << 1);
						}
					}

					if ((color & 0x3) != 0)
					{
						scanline_buffer[pixel] = _palette[Core.Maria_regs[color]];
					}
					else
					{
						scanline_buffer[pixel] = _palette[BG_latch_1];
					}
					
					// send buffer to the video buffer
					Core._vidbuffer[(scanline - 21) * 320 + pixel] = scanline_buffer[pixel];

					// clear the line ram
					line_ram[local_GFX_index, pixel] = 0;
				}

				//////////////////////////////////////////////
				// Drawing End
				//////////////////////////////////////////////

				cycle++;

				if (cycle == 454)
				{
					scanline++;

					cycle = 0;
					Core.tia._hsyncCnt = 0;
					Core.cpu.RDY = true;

					// swap scanline buffers
					GFX_index = GFX_index == 1 ? 0 : 1;
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
								temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								GFX_Objects[header_counter].addr |= (ushort)(temp << 8);
								header_pointer++;
								temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
								int temp_w = temp & 0x1F; // this is the 2's complement of width (for reasons that escape me)

								if (temp_w == 0)
								{
									// important note here. In 5 byte mode, width 0 actually counts as 32
									GFX_Objects[header_counter].width = 32;
								}
								else
								{
									temp_w--;
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
							temp_w--;
							temp_w = (0x1F - temp_w);
							GFX_Objects[header_counter].width = (byte)(temp_w & 0x1F);

							GFX_Objects[header_counter].palette = (byte)((temp & 0xE0) >> 5);
							header_pointer++;
							temp = ReadMemory((ushort)(current_DLL_addr + header_pointer));
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
						addr_t = 0;
						scan_index = 0;
						read_time = 0;
						zero_hole = false;

						// in 5 byte mode, we first have to check if we are in direct or indirect mode
						if (GFX_Objects[header_counter].ind_mode)
						{
							ch_size = 0;

							if (Core.Maria_regs[0x1C].Bit(4))
							{
								graphics_read_time = 9;
								ch_size = 2;
							}
							else
							{
								graphics_read_time = 6;
								ch_size = 1;
							}
						}
						else
						{
							graphics_read_time = 3;
						}

						if (graphics_read_time == 0)
						{
							// We have read the graphics data, for this header, now return to the header list 
							// This loop will continue until a header indicates its time to stop
							DMA_phase = DMA_HEADER;
							DMA_phase_counter = 0;
							return;
						}
					}

					if (read_time == 0)
					{
						bool skip = true;

						while (skip)
						{ 
							if (GFX_Objects[header_counter].ind_mode)
							{
								addr_t = ReadMemory((ushort)(GFX_Objects[header_counter].addr + scan_index));
								addr_t |= (ushort)((Core.Maria_regs[0x14] + current_DLL_offset) << 8);
							}
							else
							{
								addr_t = (ushort)(GFX_Objects[header_counter].addr + (current_DLL_offset << 8) + scan_index);
							}

							if (((current_DLL_H16 && addr_t.Bit(12)) || (current_DLL_H8 && addr_t.Bit(11))) && (addr_t >= 0x8000))
							{
								if (GFX_Objects[header_counter].ind_mode)
								{
									if (scan_index * ch_size < 128)
									{
										GFX_Objects[header_counter].obj[scan_index * ch_size] = 0;
									}
									if ((scan_index * ch_size + 1 < 128) && (ch_size == 2))
									{
										GFX_Objects[header_counter].obj[scan_index * ch_size + 1] = 0;
									}
								}
								else
								{
									GFX_Objects[header_counter].obj[scan_index] = 0;
								}

								if (scan_index == 0)
								{
									skip = false;
									zero_hole = true;
								}
								else
								{
									scan_index++;
									if (scan_index == GFX_Objects[header_counter].width)
									{
										skip = false;
									}
								}
							}
							else
							{
								skip = false;
							}
						}
					}

					read_time++;
					if (read_time == graphics_read_time)
					{
						if (!zero_hole)
						{
							// in 5 byte mode, we first have to check if we are in direct or indirect mode
							if (GFX_Objects[header_counter].ind_mode)
							{
								if (scan_index * ch_size < 128)
								{
									GFX_Objects[header_counter].obj[scan_index * ch_size] = ReadMemory(addr_t);
									fill_line_ram(GFX_Objects[header_counter].h_pos * 2, scan_index, 0, ch_size, GFX_Objects[header_counter].obj[scan_index * ch_size], GFX_Objects[header_counter].write_mode);
								}
								if (((scan_index * ch_size + 1) < 128) && (ch_size == 2))
								{
									GFX_Objects[header_counter].obj[scan_index * ch_size + 1] = ReadMemory((ushort)(addr_t + 1));
									fill_line_ram(GFX_Objects[header_counter].h_pos * 2, scan_index, 1, ch_size, GFX_Objects[header_counter].obj[scan_index * ch_size + 1], GFX_Objects[header_counter].write_mode);
								}
							}
							else
							{
								GFX_Objects[header_counter].obj[scan_index] = ReadMemory(addr_t);
								fill_line_ram(GFX_Objects[header_counter].h_pos * 2, scan_index, 0, 1, GFX_Objects[header_counter].obj[scan_index], GFX_Objects[header_counter].write_mode);
							}
						}

						zero_hole = false;
						scan_index++;
						read_time = 0;
					}

					if (scan_index == GFX_Objects[header_counter].width)
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
					if (DMA_phase_counter == 6)
					{
						Core.cpu_resume_pending = true;
						sl_DMA_complete = true;
						current_DLL_offset -= 1; // this is reduced by one for each scanline, which changes where graphics are fetched
						header_counter = -1;
						header_pointer = 0;
					}
					return;
				}

				if (DMA_phase == DMA_SHUTDOWN_LAST)
				{
					if (DMA_phase_counter==12)
					{
						Core.cpu_resume_pending = true;
						sl_DMA_complete = true;

						// on the last line of a zone, we load up the display list list for the next zone.
						display_zone_counter++;
						ushort temp_addr = (ushort)(display_zone_pointer + 3 * display_zone_counter);
						byte temp = ReadMemory(temp_addr);

						current_DLL_addr = (ushort)(ReadMemory((ushort)(temp_addr + 1)) << 8);
						current_DLL_addr |= ReadMemory((ushort)(temp_addr + 2));

						current_DLL_offset = (byte)(temp & 0xF);
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

		public void fill_line_ram(int temp_start, int index, int doub_size, int ch_s, byte temp_byte, bool w_m)
		{
			if (w_m)
			{
				temp_start = temp_start + index * ch_s * 4 + doub_size * 4;

				for (int z = 0; z < 4; z++)
				{
					if ((temp_start + z) % 512 < 320)
					{
						if (z < 2)
						{
							temp_check = (byte)((temp_byte & 0xC) + ((temp_byte >> 6) & 0x3));
						}
						else
						{
							temp_check = (byte)(((temp_byte & 0x3) << 2) + ((temp_byte >> 4) & 0x3));
						}

						if ((temp_check & 0xF) != 0)
						{
							line_ram[GFX_index, (temp_start + z) % 512] = temp_check;
							line_ram[GFX_index, (temp_start + z) % 512] += (byte)((GFX_Objects[header_counter].palette & 4) << 2);
						}
						else if (Core.Maria_regs[0x1C].Bit(2))
						{
							// kangaroo mode, override transparency with zero
							line_ram[GFX_index, (temp_start + z) % 512] = 0;
						}
					}
				}
			}
			else
			{
				temp_start = temp_start + index * ch_s * 8 + doub_size * 8;

				for (int z = 0; z < 8; z++)
				{
					if (((temp_start + z) % 512) < 320)
					{
						if (z < 2)
						{
							temp_check = (byte)((temp_byte >> 6) & 0x3);
						}
						else if (z < 4)
						{
							temp_check = (byte)((temp_byte >> 4) & 0x3);
						}
						else if (z < 6)
						{
							temp_check = (byte)((temp_byte >> 2) & 0x3);
						}
						else
						{
							temp_check = (byte)(temp_byte & 0x3);
						}

						if (temp_check != 0)
						{
							line_ram[GFX_index, (temp_start + z) % 512] = temp_check;
							line_ram[GFX_index, (temp_start + z) % 512] += (byte)(GFX_Objects[header_counter].palette << 2);
						}
						else if (Core.Maria_regs[0x1C].Bit(2))
						{
							// kangaroo mode, override transparency with zero
							line_ram[GFX_index, (temp_start + z) % 512] = 0;
						}
					}
				}
			}
		}

		public void Reset()
		{
			for (int i = 0; i < 128; i++)
			{
				GFX_Objects[i].obj = new byte[128];
			}		
		}

		// Most of the Maria state is captured in Maria Regs in the core
		// Only write Mode is persistent and outside of the regs
		// also since DMA is always killed at scanline boundaries, most related check variables are also not needed
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Maria));

			ser.Sync(nameof(temp_check), ref temp_check);
			ser.Sync(nameof(GFX_index), ref GFX_index);

			ser.Sync(nameof(cycle), ref cycle);
			ser.Sync(nameof(scanline), ref scanline);
			ser.Sync(nameof(DLI_countdown), ref DLI_countdown);
			ser.Sync(nameof(sl_DMA_complete), ref sl_DMA_complete);
			ser.Sync(nameof(do_dma), ref do_dma);

			ser.Sync(nameof(DMA_phase), ref DMA_phase);
			ser.Sync(nameof(DMA_phase_counter), ref DMA_phase_counter);

			ser.Sync(nameof(header_read_time), ref header_read_time);
			ser.Sync(nameof(graphics_read_time), ref graphics_read_time);
			ser.Sync(nameof(DMA_phase_next), ref DMA_phase_next);

			ser.Sync(nameof(display_zone_pointer), ref display_zone_pointer);
			ser.Sync(nameof(display_zone_counter), ref display_zone_counter);

			ser.Sync(nameof(current_DLL_offset), ref current_DLL_offset);
			ser.Sync(nameof(current_DLL_addr), ref current_DLL_addr);
			ser.Sync(nameof(current_DLL_DLI), ref current_DLL_DLI);
			ser.Sync(nameof(current_DLL_H16), ref current_DLL_H16);
			ser.Sync(nameof(current_DLL_H8), ref current_DLL_H8);

			ser.Sync(nameof(global_write_mode), ref global_write_mode);

			ser.Sync(nameof(header_counter), ref header_counter);
			ser.Sync(nameof(header_pointer), ref header_pointer);
			ser.Sync(nameof(addr_t), ref addr_t);
			ser.Sync(nameof(ch_size), ref ch_size);
			ser.Sync(nameof(scan_index), ref scan_index);
			ser.Sync(nameof(read_time), ref read_time);
			ser.Sync(nameof(zero_hole), ref zero_hole);

			ser.Sync(nameof(color), ref color);
			ser.Sync(nameof(local_GFX_index), ref local_GFX_index);
			ser.Sync(nameof(temp_palette), ref temp_palette);
			ser.Sync(nameof(temp_bit_0), ref temp_bit_0);
			ser.Sync(nameof(temp_bit_1), ref temp_bit_1);
			ser.Sync(nameof(disp_mode), ref disp_mode);
			ser.Sync(nameof(BG_latch_1), ref BG_latch_1);
			ser.Sync(nameof(pixel), ref pixel);

			ser.EndSection();
		}
	}
}
