using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class STIC : IVideoProvider
	{
		public bool Sr1, Sr2, Sst, Fgbg = false;
		public bool active_display, in_vb_1, in_vb_2 = false;
		private ushort[] Register = new ushort[64];
		public ushort ColorSP = 0x0028;

		public byte[] mobs = new byte[8];
		public byte[] y_mobs = new byte[8];

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, bool, ushort> ReadMemory;
		public Func<ushort, ushort, bool, bool> WriteMemory;

		private static readonly int BORDER_OFFSET=176*8;

		public int[] BGBuffer = new int[159 * 96];
		public int[] FrameBuffer = new int[176 * 208];
		public ushort[,] Collision = new ushort[168,210];

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(STIC));

			ser.Sync(nameof(Sr1), ref Sr1);
			ser.Sync(nameof(Sr2), ref Sr2);
			ser.Sync(nameof(Sst), ref Sst);
			ser.Sync(nameof(active_display), ref active_display);
			ser.Sync(nameof(in_vb_1), ref in_vb_1);
			ser.Sync(nameof(in_vb_2), ref in_vb_2);
			ser.Sync(nameof(Fgbg), ref Fgbg);
			ser.Sync("Toal_executed_cycles", ref TotalExecutedCycles);
			ser.Sync("Pending_Cycles", ref PendingCycles);
			ser.Sync("Registers", ref Register, false);

			Update_Border();

			ser.EndSection();
		}

		public int[] GetVideoBuffer()
		{
			
			return FrameBuffer; 
		}

		// gets called when a new border color is chosen
		private void Update_Border()
		{
			for (int i = 0; i < 176; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					FrameBuffer[i + j * 176]= ColorToRGBA(Register[0x2C] & 0xF);
					FrameBuffer[i + j * 176 + 176*200] = ColorToRGBA(Register[0x2C] & 0xF);
				}
			}

			for (int j = 8; j < (208 - 8); j++)
			{
				for (int i = 0; i < 8; i++)
				{
					FrameBuffer[i + j * 176] = ColorToRGBA(Register[0x2C] & 0xF);
					FrameBuffer[i + 168 + j * 176] = ColorToRGBA(Register[0x2C] & 0xF);
				}
			}
		}

		public int VirtualWidth => 302;
		public int BufferWidth => 176;
		public int VirtualHeight => 208;
		public int BufferHeight => 208;
		public int BackgroundColor => 0;

		public int VsyncNumerator
			=> NullVideo.DefaultVsyncNum; //TODO precise numbers or confirm the default is okay

		public int VsyncDenominator
			=> NullVideo.DefaultVsyncDen; //TODO precise numbers or confirm the default is okay

		public void Reset()
		{
			Sr1 = true;
			Sr2 = true;

			for (int i = 0; i < 64; i++)
			{
				Register[i] = 0; 
				write_reg(i, 0, false);
			}

			ColorSP = 0x0028;
		}

		public bool GetSr1() => Sr1;
		public bool GetSr2() => Sr2;

		public void ToggleSr2()
		{
			Sr2 = !Sr2;
		}

		public void SetSst(bool value)
		{
			Sst = value;
		}

		// mask off appropriate STIC bits and write to register
		private void write_reg(int reg, ushort value, bool poke)
		{
			
			if (reg < 0x8)
			{
				value = (ushort)((value & 0x7FF) | 0x3800);
			}
			else if (reg < 0x10)
			{
				value = (ushort)((value & 0xFFF) | 0x3000);
			}
			else if (reg < 0x18)
			{
				value = (ushort)(value & 0x3FFF);
			}
			else if (reg < 0x20)
			{
				value = (ushort)((value & 0x3FF) | 0x3C00);

				// self interactions can never be set, even by writing directly
				value &= (ushort)(0xFFFF - (1 << (reg - 0x18)));
			}
			else if (reg < 0x28)
			{
				value = 0x3FFF;
			}
			else if (reg < 0x2D)
			{
				value = (ushort)((value & 0xF) | 0x3FF0);
			}
			else if (reg < 0x30)
			{
				value = 0x3FFF;
			}
			else if (reg < 0x33)
			{
				if (reg == 0x32)
				{
					value = (ushort)((value & 0x3) | 0x3FFC);
				}
				else
				{
					value = (ushort)((value & 0x7) | 0x3FF8);
				}
			}
			else if (reg < 0x40)
			{
				value = 0x3FFF;
			}
			Register[reg] = value;

			if (reg == 0x21 && !poke)
			{
				Fgbg = true;
			}

			if (reg == 0x20 && !poke)
			{
				active_display = true;
			}

			if (reg==0x2C && !poke)
			{
				Update_Border();
			}
		}

		public ushort? ReadSTIC(ushort addr, bool peek)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						if (addr == 0x0021 && !peek)
						{
							Fgbg = false;
						}

						return Register[addr];
					}
					else if (addr >= 0x0040 && addr <= 0x007F && (in_vb_2 | !active_display))
					{
						return Register[addr - 0x0040];
					}
					break;
				case 0x4000:
					if ((addr <= 0x403F) && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						if (addr == 0x4021 && !peek)
						{
							Fgbg = false;
						}
					}
					break;
				case 0x8000:
					if ((addr <= 0x803F) && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						if (addr == 0x8021 && !peek)
						{
							Fgbg = false;
						}
					}
					break;
				case 0xC000:
					if ((addr <= 0xC03F) && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						if (addr == 0xC021 && !peek)
						{
							Fgbg = false;
						}
					}
					break;
			}

			return null;
		}

		public bool WriteSTIC(ushort addr, ushort value, bool poke)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						write_reg(addr, value, poke);
						return true;
					}
					break;
				case 0x4000:
					if (addr <= 0x403F && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						write_reg(addr-0x4000, value, poke);
						return true;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						write_reg(addr-0x8000, value, poke);
						return true;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F && (in_vb_1 | (!active_display & !in_vb_2)))
					{
						write_reg(addr-0xC000, value, poke);
						return true;
					}
					break;
			}

			return false;
		}

		private int ColorToRGBA(int color)
		{
			switch (color)
			{
				case 0:
					return 0x000000;
				case 1:
					return 0x002DFF;
				case 2:
					return 0xFF3D10;
				case 3:
					return 0xC9CFAB;
				case 4:
					return 0x386B3F;
				case 5:
					return 0x00A756;
				case 6:
					return 0xFAEA50;
				case 7:
					return 0xFFFCFF;
				case 8:
					return 0xBDACC8;
				case 9:
					return 0x24B8FF;
				case 10:
					return 0xFFB41F;
				case 11:
					return 0x546E00;
				case 12:
					return 0xFF4E57;
				case 13:
					return 0xA496FF;
				case 14:
					return 0x75CC80;
				case 15:
					return 0xB51A58;
				default:
					throw new ArgumentOutOfRangeException(paramName: nameof(color), color, message: "Specified color does not exist.");
			}
		}

		public void Background(int input_row)
		{
			// here we will also need to apply the 'delay' register values.
			// this shifts the displayed portion of the screen relative to the BG
			// The background is a 20x12 grid of "cards".
			int bg = 0;
			for (int card_row = input_row; card_row < (input_row+1); card_row++)
			{
				for (int card_col = 0; card_col < 20; card_col++)
				{
					int buffer_offset = (card_row * 159 * 8) + (card_col * 8);

					// The cards are stored sequentially in the System RAM.
					ushort card = ReadMemory((ushort)(0x0200 + (card_row * 20) + card_col), false);

					// Parse data from the card.
					bool gram = ((card & 0x0800) != 0);
					int card_num = card >> 3;
					int fg = card & 0x0007;
					if (Fgbg)
					{
						bg = ((card >> 9) & 0x0008) | ((card >> 11) & 0x0004) | ((card >> 9) & 0x0003);

						// Only 64 of the GROM's cards can be used in FGBG Mode.
						card_num &= 0x003F;
					}
					else
					{
						bool advance = ((card & 0x2000) != 0);
						bool squares = ((card & 0x1000) != 0);
						if (gram)
						{
							// GRAM only has 64 cards.
							card_num &= 0x003F;

							// The foreground color has an additional bit when not in Colored Squares mode.
							if (squares)
								fg |= 0x0008;
						}
						else
						{
							// All of the GROM's 256 cards can be used in Color Stack Mode.
							card_num &= 0x00FF;
						}

						if (!gram && squares)
						{
							// Colored Squares Mode.
							int[] colors = new int[4];
							int[] square_col = new int[4];
							colors[0] = fg;
							colors[1] = (card >> 3) & 0x0007;
							colors[2] = (card >> 6) & 0x0007;
							colors[3] = ((card >> 11) & 0x0004) | ((card >> 9) & 0x0003);

							for (int z = 0; z < 4; z++)
							{
								if (colors[z] == 7)
								{
									colors[z] = Register[ColorSP] & 0x000F;
									square_col[z] = 0;
								}
								else
								{
									square_col[z] = 1;
								}
							}

							for (int squares_row = 0; squares_row < 8; squares_row++)
							{
								for (int squares_col = 0; squares_col < 8; squares_col++)
								{
									// The rightmost column does not get displayed.
									if (card_col == 19 && squares_col == 7)
									{
										continue;
									}
									int color;
									int pixel = buffer_offset + (squares_row * 159) + squares_col;

									// Determine the color of the quadrant the pixel is in.
									if (squares_col < 4)
									{
										if (squares_row < 4)
										{
											color = 0;
										}
										else
										{
											color = 2;
										}
									}
									else
									{
										if (squares_row < 4)
										{
											color = 1;
										}
										else
										{
											color = 3;
										}
									}
									BGBuffer[pixel] = ColorToRGBA(colors[color]);

									// also if the pixel is on set it in the collision matrix
									// note that the collision field is attached to the lower right corner of the BG
									// so we add 8 to x and 16 to y here
									// also notice the extra condition attached to colored squares mode
									if ((card_col * 8 + squares_col + 8) < 167 && square_col[color]==1)
									{
										Collision[card_col * 8 + squares_col + 8, (card_row * 8 + squares_row) * 2 + 16] = 1 << 8;
										Collision[card_col * 8 + squares_col + 8, (card_row * 8 + squares_row) * 2 + 16 + 1] = 1 << 8;
									}
								}
							}
							continue;
						}
						else
						{
							if (advance)
							{
								// Cycle through the Color Stack registers.
								ColorSP++;
								if (ColorSP > 0x002B)
								{
									ColorSP = 0x0028;
								}
							}

							bg = Register[ColorSP] & 0x000F;
						}
					}

					for (int pict_row = 0; pict_row < 8; pict_row++)
					{
						// Each picture is stored sequentially in the GROM / GRAM, and so are their rows.
						int row_mem = (card_num * 8) + pict_row;
						byte row;
						if (gram)
						{
							row = (byte)ReadMemory((ushort)(0x3800 + row_mem), false);
						}
						else
						{
							row = (byte)ReadMemory((ushort)(0x3000 + row_mem), false);
						}
						for (int pict_col = 0; pict_col < 8; pict_col++)
						{
							// The rightmost column does not get displayed.
							if (card_col == 19 && pict_col == 0)
							{
								continue;
							}
							int pixel = buffer_offset + (pict_row * 159) + (7 - pict_col);
							// If the pixel is on, give it the FG color.
							if ((row & 0x1) != 0)
							{
								// The pixels go right as the bits get less significant.
								BGBuffer[pixel] = ColorToRGBA(fg);

								// also if the pixel is on set it in the collision matrix
								// note that the collision field is attached to the lower right corner of the BG
								// so we add 8 to x and 16 to y here
								if ((card_col * 8 + (7 - pict_col) + 8) < 167)
								{
									Collision[card_col * 8 + (7 - pict_col) + 8, (card_row * 8 + pict_row) * 2 + 16] = 1 << 8;
									Collision[card_col * 8 + (7 - pict_col) + 8, (card_row * 8 + pict_row) * 2 + 16 + 1] = 1 << 8;
								}
							}
							else
							{
								BGBuffer[pixel] = ColorToRGBA(bg);
							}

							row >>= 1;
						}
					}
				}
			}

			// now that we have the cards in BGbuffer, we can double vertical resolution to get Frame buffer
			// there is a trick here in that we move the displayed area of the screen relative to the BG buffer
			// this is done using the delay registers
			int x_delay = Register[0x30] & 0x7;
			int y_delay = Register[0x31] & 0x7;

			int x_border = (Register[0x32] & 0x0001) * 8;
			int y_border = ((Register[0x32] >> 1) & 0x0001) * 8;

			int min_x = x_border == 0 ? x_delay : x_border;
			int min_y = y_border == 0 ? y_delay : y_border;

			for (int j = input_row * 8; j < (input_row * 8) + 8; j++)
			{
				for (int i = 0; i < 159; i++)
				{
					if (i >= min_x && j >= min_y)
					{
						FrameBuffer[(j * 2) * 176 + (i+8) + BORDER_OFFSET] = BGBuffer[(j - y_delay) * 159 + i - x_delay];
						FrameBuffer[(j * 2 + 1) * 176 + (i+8) + BORDER_OFFSET] = BGBuffer[(j - y_delay) * 159 + i - x_delay];
					}
					else
					{
						FrameBuffer[(j * 2) * 176 + (i + 8) + BORDER_OFFSET] = ColorToRGBA(bg);
						FrameBuffer[(j * 2 + 1) * 176 + (i + 8) + BORDER_OFFSET] = ColorToRGBA(bg);
					} 
				}
			}
		}

		// see for more details: http://spatula-city.org/~im14u2c/intv/jzintv-1.0-beta3/doc/programming/stic.txt
		/*
		The STIC provides 3 registers for controlling each MOB, and a 4th register
		for reading its collision (or "interaction") status.  The registers are 
		laid out as follows:

		   X Register:    Address = $0000 + MOB #

			  13   12   11   10    9    8    7    6    5    4    3    2    1    0
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
			| ?? | ?? | ?? | X  |VISB|INTR|            X Coordinate               |
			|    |    |    |SIZE|    |    |             (0 to 255)                |
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

		   Y Register:    Address = $0008 + MOB #

			  13   12   11   10    9    8    7    6    5    4    3    2    1    0
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
			| ?? | ?? | Y  | X  | Y  | Y  |YRES|          Y Coordinate            |
			|    |    |FLIP|FLIP|SIZ4|SIZ2|    |           (0 to 127)             |
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

		   A Register:    Address = $0010 + MOB #

			  13   12   11   10    9    8    7    6    5    4    3    2    1    0
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
			|PRIO| FG |GRAM|      GRAM/GROM Card # (0 to 255)      |   FG Color   |
			|    |bit3|GROM|     (bits 9, 10 ignored for GRAM)     |   Bits 0-2   |
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

		   C Register:    Address = $0018 + MOB #

			  13   12   11   10    9    8    7    6    5    4    3    2    1    0
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
			| ?? | ?? | ?? | ?? |COLL|COLL|COLL|COLL|COLL|COLL|COLL|COLL|COLL|COLL|
			|    |    |    |    |BORD| BG |MOB7|MOB6|MOB5|MOB4|MOB3|MOB2|MOB1|MOB0|
			+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
		 */


		public void Mobs()
		{
			ushort x;
			ushort y;
			ushort attr;
			byte row;

			int x_delay = Register[0x30] & 0x7;
			int y_delay = Register[0x31] & 0x7;

			int cur_x, cur_y;

			// we go from 7 to zero because visibility of lower numbered MOBs have higher priority 
			for (int i = 7; i >= 0 ; i--)
			{
				x = Register[i];
				y = Register[i + 8];
				attr = Register[i + 16];

				byte card = (byte)(attr >> 3);
				bool gram = attr.Bit(11);
				byte loc_color = (byte)(attr & 7);
				bool color_3 = attr.Bit(12);

				if (color_3 && gram)
				{
					loc_color += 8;
				}

				bool priority = attr.Bit(13);
				byte loc_x = (byte)(x & 0xFF);
				byte loc_y = (byte)(y & 0x7F);
				bool vis = x.Bit(9);
				bool x_flip = y.Bit(10);
				bool y_flip = y.Bit(11);
				ushort yres = y.Bit(7) ? (ushort)2 : (ushort)1; 
				ushort ysiz2 = y.Bit(8) ? (ushort)2 : (ushort)1;
				ushort ysiz4 = y.Bit(9) ? (ushort)4 : (ushort)1;
				bool intr = x.Bit(8);
				ushort x_size = x.Bit(10) ? (ushort)2 : (ushort)1;

				ushort y_size = (ushort)(ysiz2 * ysiz4);
				// setting yres implicitly uses an even card first
				if (yres > 1)
					card &= 0xFE;

				// in GRAM mode only take the first 6 bits of the card number
				if (gram)
					card &= 0x3F;

				//pull the data from the card into the mobs array		
				for (int j=0;j<8;j++)
				{
					if (gram)
					{
						row = (byte)ReadMemory((ushort)(0x3800 + 8 * card + j), false);
					}
					else
					{
						row = (byte)ReadMemory((ushort)(0x3000 + 8 * card + j), false);
					}

					mobs[j] = row;
				}

				// assign the y_mob, used to double vertical resolution
				if (yres > 1)
				{
					for (int j = 0; j < 8; j++)
					{
						if (gram)
						{
							row = (byte)ReadMemory((ushort)(0x3800 + 8 * (card + 1) + j), false);
						}
						else
						{
							row = (byte)ReadMemory((ushort)(0x3000 + 8 * (card + 1) + j), false);
						}

						y_mobs[j] = row;
					}
				}

				// flip mobs accordingly
				if (x_flip)
				{
					for (int j = 0; j < 8; j++)
					{
						byte temp_0 = (byte)((mobs[j] & 1) << 7);
						byte temp_1 = (byte)((mobs[j] & 2) << 5);
						byte temp_2 = (byte)((mobs[j] & 4) << 3);
						byte temp_3 = (byte)((mobs[j] & 8) << 1);
						byte temp_4 = (byte)((mobs[j] & 16) >> 1);
						byte temp_5 = (byte)((mobs[j] & 32) >> 3);
						byte temp_6 = (byte)((mobs[j] & 64) >> 5);
						byte temp_7 = (byte)((mobs[j] & 128) >> 7);

						mobs[j] = (byte)(temp_0 + temp_1 + temp_2 + temp_3 + temp_4 + temp_5 + temp_6 + temp_7);
					}

					if (yres > 1)
					{
						for (int j = 0; j < 8; j++)
						{
							byte temp_0 = (byte)((y_mobs[j] & 1) << 7);
							byte temp_1 = (byte)((y_mobs[j] & 2) << 5);
							byte temp_2 = (byte)((y_mobs[j] & 4) << 3);
							byte temp_3 = (byte)((y_mobs[j] & 8) << 1);
							byte temp_4 = (byte)((y_mobs[j] & 16) >> 1);
							byte temp_5 = (byte)((y_mobs[j] & 32) >> 3);
							byte temp_6 = (byte)((y_mobs[j] & 64) >> 5);
							byte temp_7 = (byte)((y_mobs[j] & 128) >> 7);

							y_mobs[j] = (byte)(temp_0 + temp_1 + temp_2 + temp_3 + temp_4 + temp_5 + temp_6 + temp_7);
						}
					}
				}

				if (y_flip)
				{
					byte temp_0 = mobs[0];
					byte temp_1 = mobs[1];
					byte temp_2 = mobs[2];
					byte temp_3 = mobs[3];
					byte temp_4 = mobs[4];
					byte temp_5 = mobs[5];
					byte temp_6 = mobs[6];
					byte temp_7 = mobs[7];


					if (yres == 1)
					{
						mobs[0] = mobs[7];
						mobs[1] = mobs[6];
						mobs[2] = mobs[5];
						mobs[3] = mobs[4];
						mobs[4] = temp_3;
						mobs[5] = temp_2;
						mobs[6] = temp_1;
						mobs[7] = temp_0;
					}
					else
					{
						mobs[0] = y_mobs[7];
						mobs[1] = y_mobs[6];
						mobs[2] = y_mobs[5];
						mobs[3] = y_mobs[4];
						mobs[4] = y_mobs[3];
						mobs[5] = y_mobs[2];
						mobs[6] = y_mobs[1];
						mobs[7] = y_mobs[0];

						y_mobs[0] = temp_7;
						y_mobs[1] = temp_6;
						y_mobs[2] = temp_5;
						y_mobs[3] = temp_4;
						y_mobs[4] = temp_3;
						y_mobs[5] = temp_2;
						y_mobs[6] = temp_1;
						y_mobs[7] = temp_0;
					}
				}

				// draw the mob and check for collision
				for (int j = 0; j < 8; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						bool pixel = mobs[j].Bit(7 - k);

						cur_x = loc_x + k * x_size;

						for (int m = 0; m < y_size; m++)
						{
							cur_y = j * y_size + m;

							if ((cur_x) < (167 - x_delay) && (loc_y * 2 + cur_y) < (208 - y_delay * 2) && pixel && (vis && (loc_x != 0)) && cur_x >= (8 - x_delay) && (loc_y * 2 + cur_y) >= (16 - y_delay * 2))
							{
								if (!(priority && (Collision[cur_x, loc_y * 2 + cur_y]&0x100)>0))
									FrameBuffer[(loc_y * 2 + cur_y - (16 - y_delay * 2)) * 176 + cur_x + x_delay + BORDER_OFFSET] = ColorToRGBA(loc_color);
							}

							// a MOB does not need to be visible for it to be interracting
							// special case: a mob with x position 0 is counted as off
							if (intr && pixel && cur_x <= 167 && (loc_y * 2 + cur_y) < 210 && loc_x != 0)
							{
								Collision[cur_x, loc_y * 2 + cur_y] |= (ushort)(1 << i);
							}

							if (x_size == 2)
							{
								if ((cur_x + 1) < (167 - x_delay) && (loc_y * 2 + cur_y) < (208 - y_delay * 2) && pixel && (vis && (loc_x != 0)) && (cur_x + 1) >= (8 - x_delay) && (loc_y * 2 + cur_y) >= (16 - y_delay * 2))
								{
									if (!(priority && (Collision[cur_x + 1, loc_y * 2 + cur_y] & 0x100) > 0))
										FrameBuffer[(loc_y * 2 + cur_y - (16 - y_delay * 2)) * 176 + cur_x + x_delay + 1 + BORDER_OFFSET] = ColorToRGBA(loc_color);
								}
								//a MOB does not need to be visible for it to be interracting
								//special case: a mob with x position 0 is counted as off
								if (intr && pixel && (cur_x + 1) <= 167 && (loc_y * 2 + cur_y) < 210 && loc_x != 0)
								{
									Collision[cur_x + 1, loc_y * 2 + cur_y] |= (ushort)(1 << i);
								}
							}
						}
					}
				}

				// Now repeat the process if the mob is double sized
				if (yres>1)
				{
					for (int j = 0; j < 8; j++)
					{
						for (int k = 0; k < 8; k++)
						{
							bool pixel = y_mobs[j].Bit(7 - k);
							cur_x = loc_x + k * x_size;

							for (int m = 0; m < y_size; m++)
							{
								cur_y = j * y_size + m;

								if ((cur_x) < (167 - x_delay) && ((loc_y + 4 * y_size) * 2 + cur_y) < (208 - y_delay * 2) && pixel && (vis && (loc_x != 0)) && cur_x >= (8 - x_delay) && ((loc_y + 4 * y_size) * 2 + cur_y) >= (16 - y_delay * 2))
								{
									if (!(priority && (Collision[cur_x, (loc_y + 4 * y_size) * 2 + cur_y] & 0x100) > 0))
										FrameBuffer[((loc_y + 4 * y_size) * 2 + cur_y - (16 - y_delay * 2)) * 176 + cur_x + x_delay + BORDER_OFFSET] = ColorToRGBA(loc_color);
								}

								// a MOB does not need to be visible for it to be interracting
								// special case: a mob with x position 0 is counted as off
								if (intr && pixel && cur_x <= 167 && ((loc_y + 4 * y_size) * 2 + cur_y) < 210 && loc_x != 0)
								{
									Collision[cur_x, (loc_y + 4 * y_size) * 2 + cur_y] |= (ushort)(1 << i);
								}

								if (x_size == 2)
								{
									if ((cur_x + 1) < (167 - x_delay) && ((loc_y + 4 * y_size) * 2 + cur_y) < (208 - y_delay * 2) && pixel && (vis && (loc_x != 0)) && (cur_x + 1) >= (8 - x_delay) && ((loc_y + 4 * y_size) * 2 + cur_y) >= (16 - y_delay * 2))
									{
										if (!(priority && (Collision[cur_x + 1, (loc_y + 4 * y_size) * 2 + cur_y] & 0x100) > 0))
											FrameBuffer[((loc_y + 4 * y_size) * 2 + cur_y - (16 - y_delay * 2)) * 176 + cur_x + x_delay + 1 + BORDER_OFFSET] = ColorToRGBA(loc_color);
									}

									// a MOB does not need to be visible for it to be interracting
									// special case: a mob with x position 0 is counted as off
									if (intr && pixel && (cur_x + 1) <= 167 && ((loc_y + 4 * y_size) * 2 + cur_y) < 210 && loc_x != 0)
									{
										Collision[cur_x + 1, (loc_y + 4 * y_size) * 2 + cur_y] |= (ushort)(1 << i);
									}
								}
							}
						}
					}
				}
			}

			// by now we have collision information for all 8 mobs and the BG
			// so we can store data in the collision registers here
			int x_border = Register[0x32].Bit(0) ? 15 - x_delay : 7 - x_delay;
			int y_border = Register[0x32].Bit(1) ? 30 - y_delay * 2 : 14 - y_delay * 2;

			int x_border_2 = Register[0x32].Bit(0) ? 8 : 0;
			int y_border_2 = Register[0x32].Bit(1) ? 16 : 0;

			for (int i = 0; i < 168; i++)
			{
				for (int j = 0; j < 210; j++)
				{
					// while we are here we can set collision detection bits for the border region
					if (i == x_border || i == (167 - x_delay))
					{
						Collision[i, j] |= (1 << 9);
					}
					if (j == y_border || j == y_border + 1 || j == (208 - y_delay * 2) ||  j == (208 - y_delay * 2 + 1))
					{
						Collision[i, j] |= (1 << 9);
					}

					// and also make sure the border region is all the border color
					if ((i-x_delay)>=0 && (i-x_delay) <= 159 && (j-y_delay*2) >= 0 && (j-y_delay*2) < 192)
					{
						if ((i-x_delay) < x_border_2)
							FrameBuffer[(j - y_delay*2) * 176 + ((i + 8) - x_delay) + BORDER_OFFSET] = ColorToRGBA(Register[0x2C] & 0xF);

						if ((j - y_delay*2) < y_border_2)
							FrameBuffer[(j - y_delay*2) * 176 + ((i + 8) - x_delay) + BORDER_OFFSET] = ColorToRGBA(Register[0x2C] & 0xF);

						if ((i-x_delay)==159)
						{
							FrameBuffer[(j - y_delay*2) * 176 + ((i + 8) - x_delay) + BORDER_OFFSET] = ColorToRGBA(Register[0x2C] & 0xF);
						}
					}

					// the extra condition here is to ignore only border/BG collsion bit set
					if (Collision[i, j] != 0 && Collision[i,j] != (1 << 9) && Collision[i,j] != (1 << 8)) 
					{
						for (int k = 0; k < 8; k++)
						{
							for (int m = 0; m < 10; m++)
							{
								if (k != m) // mobs never self interact
								{
									Register[k + 24] |= (ushort)((Collision[i, j].Bit(k) && Collision[i, j].Bit(m)) ? 1 << m : 0);
								}
							}
						}
					}

					// after we check for collision, we can clear that value for the next frame.
					Collision[i, j] = 0;
				}
			}
		}
		// end of Mobs function, we now have collision and graphics data for the mobs
	}
}
