using System;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public class PPU : ISoundProvider
	{
		public O2Hawk Core { get; set; }

		public byte[] Sprites = new byte[16];
		public byte[] Sprite_Shapes = new byte[32];
		public byte[] Foreground = new byte[48];
		public byte[] Quad_Chars = new byte[64];
		public byte[] Grid_H = new byte[16];
		public byte[] Grid_V = new byte[8];

		public byte VDC_ctrl, VDC_status, VDC_collision, VDC_color;
		public byte Frame_Col, Pixel_Stat;
		
		public uint[] BG_palette = new uint[32];
		public uint[] OBJ_palette = new uint[32];

		public bool HDMA_active;

		// register variables
		public byte LCDC;
		public byte STAT;
		public byte scroll_y;
		public byte scroll_x;
		public int LY;
		public byte LY_actual;
		public byte LY_inc;
		public byte LYC;
		public byte DMA_addr;
		public byte BGP;
		public byte obj_pal_0;
		public byte obj_pal_1;
		public byte window_y;
		public byte window_x;
		public bool DMA_start;
		public int DMA_clock;
		public int DMA_inc;
		public byte DMA_byte;
		public int cycle;
		public bool VBL;
		public bool HBL;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			if (addr < 0x10)
			{
				ret = Sprites[addr];
			}
			else if (addr < 0x40)
			{
				ret = Foreground[addr - 0x10];
			}
			else if (addr < 0x80)
			{
				ret = Quad_Chars[addr - 0x40];
			}
			else if (addr < 0xA0)
			{
				ret = Sprite_Shapes[addr - 0x80];
			}
			else if (addr == 0xA0)
			{
				ret = VDC_ctrl;
			}
			else if (addr == 0xA1)
			{
				ret = VDC_status;
			}
			else if (addr == 0xA2)
			{
				ret = VDC_collision;
				//Console.WriteLine("col: " + ret + " " + Core.cpu.TotalExecutedCycles);
			}
			else if(addr == 0xA3)
			{
				ret = VDC_color;
			}
			else if (addr <= 0xAA)
			{
				ret = AudioReadReg(addr);
			}
			else if ((addr >= 0xC0) && (addr < 0xC8))
			{
				ret = Grid_H[addr - 0xC0];
			}
			else if ((addr >= 0xD0) && (addr < 0xD8))
			{
				ret = Grid_H[addr - 0xD0 + 8];
			}
			else if ((addr >= 0xE0) && (addr < 0xE8))
			{
				ret = Grid_V[addr - 0xE0];
			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			if (addr < 0x10)
			{
				Sprites[addr] = value;
                //Console.WriteLine("spr: " + addr + " " + value + " " + Core.cpu.TotalExecutedCycles);
			}
			else if (addr < 0x40)
			{
				Foreground[addr - 0x10] = value;
			}
			else if (addr < 0x80)
			{
				Quad_Chars[addr - 0x40] = value;
			}
			else if (addr < 0xA0)
			{
				Sprite_Shapes[addr - 0x80] = value;
			}
			else if (addr == 0xA0)
			{
				VDC_ctrl = value;
				//Console.WriteLine(value + " " + Core.cpu.TotalExecutedCycles);
			}
			else if (addr == 0xA1)
			{
				VDC_status = value;
			}
			else if (addr == 0xA2)
			{
				VDC_collision = value;
			}
			else if (addr == 0xA3)
			{
				VDC_color = value;
			}
			else if (addr <= 0xAA)
			{
				AudioWriteReg(addr, value);
			}
			else if ((addr >= 0xC0) && (addr < 0xC8))
			{
				Grid_H[addr - 0xC0] = value;
			}
			else if ((addr >= 0xD0) && (addr < 0xD8))
			{
				Grid_H[addr - 0xD0 + 8] = value;
			}
			else if ((addr >= 0xE0) && (addr < 0xE8))
			{
				Grid_V[addr - 0xE0] = value;
			}
		}

		public void tick()
		{
			cycle++;
			Pixel_Stat = 0;
			// drawing cycles
			if (cycle >= 43)
			{
				if (cycle == 43)
				{
					HBL = false;
					// trigger timer tick if enabled
					if (Core.cpu.counter_en) { Core.cpu.T1 = false; }
					//if (VDC_ctrl.Bit(0)) { Core.cpu.IRQPending = false; }
					
					if (LY == 240) { VDC_status |= 0x08; }
					if (LY == 241) { VDC_status &= 0xF7; }
				}

				// draw a pixel
				if (LY < 240)
				{
					// sprites
					for (int i = 0; i < 4; i++)
					{
						if ((LY >= Sprites[i * 4]) && (LY < (Sprites[i * 4] + 8)))
						{
							if (((cycle - 43) >= Sprites[i * 4 + 1]) && ((cycle - 43) < (Sprites[i * 4 + 1] + 8)))
							{
								// character is in drawing region, pick a pixel
								int offset_y = LY - Sprites[i * 4];
								int offset_x = 7 - ((cycle - 43) - Sprites[i * 4 + 1]);

								int pixel_pick = (Sprite_Shapes[i * 8 + offset_y] >> offset_x) & 1;

								if (pixel_pick == 1)
								{
									Core._vidbuffer[LY * 186 + (cycle - 43)] = (int) Color_Palette[(Sprites[i * 4 + 2] >> 3) & 0x7];
									Pixel_Stat |= (byte)(i << 1);
								}
							}
						}
					}

					// single characters
					for (int i = 0; i < 12; i++)
					{
						if ((LY >= Foreground[i * 4]) && (LY < (Foreground[i * 4] + 8)))
						{
							if (((cycle - 43) >= Foreground[i * 4 + 1]) && ((cycle - 43) < (Foreground[i * 4 + 1] + 8)))
							{
								// sprite is in drawing region, pick a pixel
								int offset_y = LY - Foreground[i * 4];
								int offset_x = 7 - ((cycle - 43) - Foreground[i * 4 + 1]);
								int char_sel = Foreground[i * 4 + 2] + ((Foreground[i * 4 + 3] & 1) << 8);

								int pixel_pick = (Internal_Graphics[(char_sel + offset_y) % 0x200] >> offset_x) & 1;

								if (pixel_pick == 1)
								{
									Core._vidbuffer[LY * 186 + (cycle - 43)] = (int)Color_Palette[(Foreground[i * 4 + 3] >> 1) & 0x7];
									Pixel_Stat |= 0x80;
								}
							}
						}
					}

					// quads
					// background
					// calculate collision

				}
			}

			// end of scanline
			if (cycle == 228)
			{
				cycle = 0;
				HBL = true;
				

				// trigger timer tick if enabled
				if (Core.cpu.counter_en) { Core.cpu.T1 = true; }

				LY++;
				if (LY == 240)
				{
					VBL = true;
					Core.in_vblank = true;
					if (!VDC_ctrl.Bit(0)) { Core.cpu.IRQPending = true; }
				}
				if (LY == 241)
				{
					if (!VDC_ctrl.Bit(0)) { Core.cpu.IRQPending = false; }
				}
				if (LY == 262)
				{
					LY = 0;
					HBL = false;
					VBL = false;
					Core.in_vblank = false;
					if (!VDC_ctrl.Bit(0)) { Core.cpu.IRQPending = false; }
					Frame_Col = 0;
				}
			}
		}

		// might be needed, not sure yet
		public void latch_delay()
		{

		}

		public void render(int render_cycle)
		{

		}

		public void process_sprite()
		{

		}

		// normal DMA moves twice as fast in double speed mode on GBC
		// So give it it's own function so we can seperate it from PPU tick
		public void DMA_tick()
		{

		}

		public void OAM_scan(int OAM_cycle)
		{

		}

		public void Reset()
		{
			AudioReset();
		}

		public static readonly byte[] Internal_Graphics = { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 00, // 0				0x00
															0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x3C, 00, // 1				0x01
															0x3C, 0x66, 0x0C, 0x18, 0x30, 0x60, 0x7E, 00, // 2				0x02
															0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C, 00, // 3				0x03
															0xCC, 0xCC, 0xCC, 0xFE, 0x0C, 0x0C, 0x0C, 00, // 4				0x04
															0x7E, 0x60, 0x60, 0x3C, 0x60, 0x66, 0x3C, 00, // 5				0x05
															0x3C, 0x66, 0x60, 0x7C, 0x66, 0x66, 0x3C, 00, // 6				0x06
															0xFE, 0x06, 0x0C, 0x18, 0x30, 0x60, 0xC0, 00, // 7				0x07
															0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, 00, // 8				0x08
															0x3C, 0x66, 0x66, 0x3E, 0x02, 0x66, 0x3C, 00, // 9				0x09
															0x00, 0x18, 0x18, 0x00, 0x18, 0x18, 0x00, 00, // :				0x0A
															0x18, 0x7E, 0x58, 0x7E, 0x1A, 0x7E, 0x18, 00, // $				0x0B
															0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 00, //  				0x0C
															0x3C, 0x66, 0x0C, 0x18, 0x18, 0x00, 0x18, 00, // ?				0x0D
															0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, 00, // L				0x0E
															0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60, 00, // P				0x0F
															0x00, 0x18, 0x18, 0x7E, 0x18, 0x18, 0x00, 00, // +				0x10
															0xC6, 0xC6, 0xC6, 0xD6, 0xFE, 0xEE, 0xC6, 00, // W				0x11
															0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x7E, 00, // E				0x12
															0xFC, 0xC6, 0xC6, 0xFC, 0xD8, 0xCC, 0xC6, 00, // R				0x13
															0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 00, // T				0x14
															0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x7C, 00, // U				0x15
															0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C, 00, // I				0x16
															0x7C, 0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x7C, 00, // O				0x17
															0x7C, 0xC6, 0xC6, 0xC6, 0xD6, 0xCC, 0x76, 00, // Q				0x18
															0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C, 00, // S				0x19
															0x7C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x7C, 00, // D				0x1A
															0xFE, 0xC0, 0xC0, 0xF8, 0xC0, 0xC0, 0xC0, 00, // F				0x1B
															0x7C, 0xC6, 0xC0, 0xC0, 0xCE, 0xC6, 0x7E, 00, // G				0x1C
															0xC6, 0xC6, 0xC6, 0xFE, 0xC6, 0xC6, 0xC6, 00, // H				0x1D
															0x06, 0x06, 0x06, 0x06, 0x06, 0xC6, 0x7C, 00, // J				0x1E
															0xC6, 0xCC, 0xD8, 0xF0, 0xD8, 0xCC, 0xC6, 00, // K				0x1F
															0x38, 0x6C, 0xC6, 0xC6, 0xF7, 0xC6, 0xC6, 00, // A				0x20
															0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E, 00, // Z				0x21
															0xC6, 0xC6, 0x6C, 0x38, 0x6C, 0xC6, 0xC6, 00, // X				0x22
															0x7C, 0xC6, 0xC0, 0xC0, 0xC0, 0xC6, 0x7C, 00, // C				0x23
															0xC6, 0xC6, 0xC6, 0xC6, 0xC6, 0x6C, 0x38, 00, // V				0x24
															0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C, 00, // B				0x25
															0xC6, 0xEE, 0xFE, 0xD6, 0xC6, 0xC6, 0xC6, 00, // M				0x26
															0x00, 0x00, 0x00, 0x00, 0x00, 0x38, 0x38, 00, // .				0x27
															0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00, 00, // -				0x28
															0x00, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x00, 00, // x				0x29
															0x00, 0x18, 0x00, 0x7E, 0x00, 0x18, 0x00, 00, // (div)			0x2A
															0x00, 0x00, 0x7E, 0x00, 0x7E, 0x00, 0x00, 00, // =				0x2B
															0x66, 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, 00, // Y				0x2C
															0xC6, 0xE6, 0xF6, 0xFE, 0xDE, 0xCE, 0xC6, 00, // N				0x2D
															0x03, 0x06, 0xC0, 0x18, 0x30, 0x60, 0xC0, 00, // /				0x2E
															0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 00, // (box)			0x2F
															0xCE, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xCE, 00, // 10				0x30
															0x00, 0x00, 0x3C, 0x7E, 0x7E, 0x7E, 0x3C, 00, // (ball)			0x31
															0x38, 0x38, 0x30, 0x3C, 0x30, 0x30, 0x38, 00, // (person R)		0x32
															0x38, 0x38, 0x30, 0x3C, 0x30, 0x68, 0x4C, 00, // (runner R)		0x33
															0x38, 0x38, 0x18, 0x78, 0x18, 0x2C, 0x64, 00, // (runner L)		0x34
															0x38, 0x38, 0x18, 0x78, 0x18, 0x18, 0x38, 00, // (person L)		0x35
															0x00, 0x18, 0xC0, 0xF7, 0xC0, 0x18, 0x00, 00, // (arrow R)		0x36
															0x18, 0x3C, 0x7E, 0xFF, 0xFF, 0x18, 0x18, 00, // (tree)			0x37
															0x01, 0x03, 0x07, 0x0F, 0x1F, 0x3F, 0x7F, 00, // (ramp R)		0x38
															0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, 00, // (ramp L)		0x39
															0x38, 0x38, 0x12, 0xFE, 0xB8, 0x28, 0x6C, 00, // (person F)		0x3A
															0xC0, 0x60, 0x30, 0x18, 0x0C, 0x06, 0x03, 00, // \				0x3B
															0x00, 0x00, 0x18, 0x10, 0x10, 0xF7, 0x7C, 00, // (boat 1)		0x3C
															0x00, 0x03, 0x63, 0xFF, 0xFF, 0x18, 0x08, 00, // (plane)		0x3D
															0x00, 0x00, 0x00, 0x01, 0x38, 0xFF, 0x7E, 00, // (boat 2)		0x3E
															0x00, 0x00, 0x00, 0x54, 0x54, 0xFF, 0x7E, 00  // (boat 3 unk)	0x3F
															};

		public static readonly uint[] Color_Palette =
		{
			0xFF006D07, //green
			0xFF56C469, // light green
			0xFF2AAABE, // blue-green
			0xFF77E6EB, // light blue-green
			0xFF1A37BE, // blue
			0xFF5C80F6, // light blue
			0xFF94309F, // violet
			0xFFDC84D4, // light violet
			0xFF790000, // red
			0xFFC75151, // light red
			0xFF77670B, // yellow
			0xFFC6B869, // light yellow
			0xFF676767, // grey
			0xFFCECECE, // light grey
			0xFF000000, // black
			0xFFFFFFFF, // white

		};


		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(Sprites), ref Sprites, false);
			ser.Sync(nameof(Sprite_Shapes), ref Sprite_Shapes, false);
			ser.Sync(nameof(Foreground), ref Foreground, false);
			ser.Sync(nameof(Quad_Chars), ref Quad_Chars, false);
			ser.Sync(nameof(Grid_H), ref Grid_H, false);
			ser.Sync(nameof(Grid_V), ref Grid_V, false);

			ser.Sync(nameof(VDC_ctrl), ref VDC_ctrl);
			ser.Sync(nameof(VDC_status), ref VDC_status);
			ser.Sync(nameof(VDC_collision), ref VDC_collision);
			ser.Sync(nameof(VDC_color), ref VDC_color);
			ser.Sync(nameof(Frame_Col), ref Frame_Col);
			ser.Sync(nameof(Pixel_Stat), ref Pixel_Stat);

			ser.Sync(nameof(BG_palette), ref BG_palette, false);
			ser.Sync(nameof(OBJ_palette), ref OBJ_palette, false);
			ser.Sync(nameof(HDMA_active), ref HDMA_active);

			ser.Sync(nameof(LCDC), ref LCDC);
			ser.Sync(nameof(STAT), ref STAT);
			ser.Sync(nameof(scroll_y), ref scroll_y);
			ser.Sync(nameof(scroll_x), ref scroll_x);
			ser.Sync(nameof(LY), ref LY);
			ser.Sync(nameof(LY_actual), ref LY_actual);
			ser.Sync(nameof(LY_inc), ref LY_inc);
			ser.Sync(nameof(LYC), ref LYC);
			ser.Sync(nameof(DMA_addr), ref DMA_addr);
			ser.Sync(nameof(BGP), ref BGP);
			ser.Sync(nameof(obj_pal_0), ref obj_pal_0);
			ser.Sync(nameof(obj_pal_1), ref obj_pal_1);
			ser.Sync(nameof(window_y), ref window_y);
			ser.Sync(nameof(window_x), ref window_x);
			ser.Sync(nameof(DMA_start), ref DMA_start);
			ser.Sync(nameof(DMA_clock), ref DMA_clock);
			ser.Sync(nameof(DMA_inc), ref DMA_inc);
			ser.Sync(nameof(DMA_byte), ref DMA_byte);
			ser.Sync(nameof(cycle), ref cycle);
			ser.Sync(nameof(VBL), ref VBL);
			ser.Sync(nameof(HBL), ref HBL);

			AudioSyncState(ser);
		}

		private BlipBuffer _blip_C = new BlipBuffer(15000);

		public byte sample;

		public byte shift_0, shift_1, shift_2, aud_ctrl;
		public byte shift_reg_0, shift_reg_1, shift_reg_2;

		public uint master_audio_clock;

		public int tick_cnt, output_bit;

		public int latched_sample_C;

		public byte AudioReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0xA7: ret = shift_reg_0; break;
				case 0xA8: ret = shift_reg_1; break;
				case 0xA9: ret = shift_reg_2; break;
				case 0xAA: ret = aud_ctrl; break;
			}

			return ret;
		}

		public void AudioWriteReg(int addr, byte value)
		{
			switch (addr)
			{
				case 0xA7: shift_0 = shift_reg_0 = value; break;
				case 0xA8: shift_1 = shift_reg_1 = value; break;
				case 0xA9: shift_2 = shift_reg_2 = value; break;
				case 0xAA: aud_ctrl = value; break;
			}

			

		}

		public void Audio_tick()
		{
			int C_final = 0;

			if (aud_ctrl.Bit(7))
			{
				tick_cnt++;
				if (tick_cnt > (aud_ctrl.Bit(5) ? 455 : 1820))
				{
					tick_cnt = 0;

					output_bit = shift_2 & 1;

					shift_2 = (byte)((shift_2 >> 1) | ((shift_1 & 1) << 7));
					shift_1 = (byte)((shift_1 >> 1) | ((shift_0 & 1) << 7));

					if (aud_ctrl.Bit(6))
					{
						shift_0 = (byte)((shift_0 >> 1) | (output_bit << 7));
					}
					else
					{
						shift_0 = (byte)(shift_0 >> 1);
					}
				}

				C_final = output_bit;
				C_final *= ((aud_ctrl & 0xF) + 1) * 3200;
			}

			if (C_final != latched_sample_C)
			{
				_blip_C.AddDelta(master_audio_clock, C_final - latched_sample_C);
				latched_sample_C = C_final;
			}

			master_audio_clock++;
		}

		public void AudioReset()
		{
			master_audio_clock = 0;

			sample = 0;

			_blip_C.SetRates(1792000, 44100);
		}

		public void AudioSyncState(Serializer ser)
		{
			ser.Sync(nameof(master_audio_clock), ref master_audio_clock);

			ser.Sync(nameof(sample), ref sample);
			ser.Sync(nameof(latched_sample_C), ref latched_sample_C);

			ser.Sync(nameof(aud_ctrl), ref aud_ctrl);
			ser.Sync(nameof(shift_0), ref shift_0);
			ser.Sync(nameof(shift_1), ref shift_1);
			ser.Sync(nameof(shift_2), ref shift_2);
			ser.Sync(nameof(shift_reg_0), ref shift_reg_0);
			ser.Sync(nameof(shift_reg_1), ref shift_reg_1);
			ser.Sync(nameof(shift_reg_2), ref shift_reg_2);
			ser.Sync(nameof(tick_cnt), ref tick_cnt);
			ser.Sync(nameof(output_bit), ref output_bit);
		}

		#region audio

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported_");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_blip_C.EndFrame(master_audio_clock);

			nsamp = _blip_C.SamplesAvailable();

			samples = new short[nsamp * 2];

			if (nsamp != 0)
			{
				_blip_C.ReadSamples(samples, nsamp, true);
			}

			master_audio_clock = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_blip_C.Clear();
			master_audio_clock = 0;
		}

		private void GetSamples(short[] samples)
		{

		}

		public void DisposeSound()
		{
			_blip_C.Clear();
			_blip_C.Dispose();
			_blip_C = null;
		}

		#endregion
	}
}
