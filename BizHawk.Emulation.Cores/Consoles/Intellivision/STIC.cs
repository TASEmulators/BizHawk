using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class STIC : IVideoProvider
	{
		private bool Sr1, Sr2, Sst, Fgbg = false;
		private ushort[] Register = new ushort[64];
		private ushort ColorSP = 0x0028;

		public byte[] mobs = new byte[8];
		public byte[] y_mobs = new byte[8];

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, ushort> ReadMemory;
		public Func<ushort, ushort, bool> WriteMemory;

		public int[] BGBuffer = new int[159 * 96];
		public int[] FrameBuffer = new int[159 * 192];
		public ushort[,] Collision = new ushort[167,210];

		public int[] GetVideoBuffer()
		{
			
			return FrameBuffer; 
		}

		public int VirtualWidth { get { return 159; } }
		public int BufferWidth { get { return 159; } }
		public int VirtualHeight { get { return 192; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }
		
		public void Reset()
		{
			Sr1 = true;
			Sr2 = true;
		}

		public bool GetSr1()
		{
			return Sr1;
		}

		public bool GetSr2()
		{
			return Sr2;
		}

		public void SetSst(bool value)
		{
			Sst = value;
		}

		public ushort? ReadSTIC(ushort addr)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x0021)
						{
							Fgbg = false;
						}
						return Register[addr];
					}
					else if (addr <= 0x007F)
					{
						// TODO: OK only during VBlank Period 2.
						return Register[addr - 0x0040];
					}
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x4021)
						{
							Fgbg = false;
						}
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
						{
							Fgbg = false;
						}
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
						{
							Fgbg = false;
						}
					}
					break;
			}
			return null;
		}

		public bool WriteSTIC(ushort addr, ushort value)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x0021)
						{
							Fgbg = true;
						}
						Register[addr] = value;
						return true;
					}
					else if (addr <= 0x007F)
					{
						// Read-only STIC.
						break;
					}
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x4021)
						{
							Fgbg = true;
						}
						Register[addr - 0x4000] = value;
						return true;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
						{
							Fgbg = true;
						}
						Register[addr & 0x003F] = value;
						return true;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
						{
							Fgbg = true;
						}
						Register[addr - 0xC000] = value;
						return true;
					}
					break;
			}
			return false;
		}

		public void Execute(int cycles)
		{
			PendingCycles -= cycles;
			TotalExecutedCycles += cycles;
			if (PendingCycles <= 0)
			{
				Sr1 = !Sr1;
				if (Sr1)
				{
					PendingCycles = 14934 - 3791;
				}
				else
				{
					PendingCycles += 3791;
				}
			}
		}

		public int ColorToRGBA(int color)
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
			}
			throw new ArgumentException("Specified color does not exist.");
		}

		public void Background()
		{
			// here we will also need to apply the 'delay' register values.
			// this shifts the background portion of the screen relative to the mobs
			
			// The background is a 20x12 grid of "cards".
			for (int card_row = 0; card_row < 12; card_row++)
			{
				for (int card_col = 0; card_col < 20; card_col++)
				{
					int buffer_offset = (card_row * 159 * 8) + (card_col * 8);
					// The cards are stored sequentially in the System RAM.
					ushort card = ReadMemory((ushort)(0x0200 + (card_row * 20) + card_col));
					// Parse data from the card.
					bool gram = ((card & 0x0800) != 0);
					int card_num = card >> 3;
					int fg = card & 0x0007;
					int bg;
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
							colors[0] = fg;
							colors[1] = (card >> 3) & 0x0007;
							colors[2] = (card >> 6) & 0x0007;
							colors[3] = ((card >> 11) & 0x0004) | ((card >> 9) & 0x0003);
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
							bg = ReadMemory(ColorSP) & 0x000F;
						}
					}
					for (int pict_row = 0; pict_row < 8; pict_row++)
					{
						// Each picture is stored sequentially in the GROM / GRAM, and so are their rows.
						int row_mem = (card_num * 8) + pict_row;
						byte row;
						if (gram)
						{
							row = (byte)ReadMemory((ushort)(0x3800 + row_mem));
						}
						else
						{
							row = (byte)ReadMemory((ushort)(0x3000 + row_mem));
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
								if ((card_col * 8 + pict_col + 8) < 167)
								{
									Collision[card_col * 8 + pict_col + 8, (card_row * 8 + pict_row) * 2 + 16] = 1 << 8;
									Collision[card_col * 8 + pict_col + 8, (card_row * 8 + pict_row) * 2 + 16 + 1] = 1 << 8;
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

			int x_delay = Register[0x30];
			int y_delay = Register[0x31];

			for (int j=0;j<96;j++)
			{
				for (int i = 0; i < 159; i++)
				{
					if (i >= x_delay && j >= y_delay)
					{
						FrameBuffer[(j * 2) * 159 + i] = BGBuffer[(j - y_delay) * 159 + i - x_delay];
						FrameBuffer[(j * 2 + 1) * 159 + i] = BGBuffer[(j - y_delay) * 159 + i - x_delay];
					}
					else
					{
						FrameBuffer[(j * 2) * 159 + i] = ColorToRGBA(Register[0x2C]);
						FrameBuffer[(j * 2 + 1) * 159 + i] = ColorToRGBA(Register[0x2C]);
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

			int x_delay = Register[0x30];
			int y_delay = Register[0x31];


			for (int i = 0; i < 8; i++)
			{
				x = Register[i];
				y = Register[i + 8];
				attr = Register[i + 16];

				byte card = (byte)(attr >> 3);
				bool gram = attr.Bit(11);
				byte loc_color = (byte)(attr & 3);
				bool color_3 = attr.Bit(12);
				if (color_3)
					loc_color += 4;

				byte loc_x = (byte)(x & 0xFF);
				byte loc_y = (byte)(y & 0x7F);
				bool vis = x.Bit(9);
				bool x_flip = y.Bit(10);
				bool y_flip = y.Bit(11);
				bool yres = y.Bit(7);
				bool ysiz2 = y.Bit(8);
				bool ysiz4 = y.Bit(9);
				bool intr = x.Bit(8);

				// setting yres implicitly uses an even card first
				if (yres)
					card &= 0xFE;

				// in GRAM mode only take the first 6 bits of the card number
				if (gram)
					card &= 0x3F;

				//pull the data from the card into the mobs array		
				for (int j=0;j<8;j++)
				{
					if (gram)
					{
						row = (byte)ReadMemory((ushort)(0x3800 + 8 * card + j));
					}
					else
					{
						row = (byte)ReadMemory((ushort)(0x3000 + 8 * card + j));
					}

					mobs[j] = row;
				}

				// assign the y_mob, used to double vertical resolution
				if (yres)
				{
					for (int j = 0; j < 8; j++)
					{
						if (gram)
						{
							row = (byte)ReadMemory((ushort)(0x3800 + 8 * (card + 1) + j));
						}
						else
						{
							row = (byte)ReadMemory((ushort)(0x3000 + 8 * (card + 1) + j));
						}

						y_mobs[j] = row;
					}
				}

				//flip mobs accordingly
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
					if (yres)
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

					mobs[0] = mobs[7];
					mobs[1] = mobs[6];
					mobs[2] = mobs[5];
					mobs[3] = mobs[4];
					mobs[4] = temp_3;
					mobs[5] = temp_2;
					mobs[6] = temp_1;
					mobs[7] = temp_0;
				}

				//TODO:stretch

				//TODO:pixel priority

				//draw the mob and check for collision
				//we already have the BG at this point, so for now let's assume mobs have priority for testing

				for (int j = 0; j < 8; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						bool pixel = mobs[j].Bit(7 - k);

						if ((loc_x + k) < (167-x_delay) && (loc_y*2 + j) < (208-y_delay*2) && pixel && (loc_x + k ) >= (8 - x_delay) && (loc_y * 2 + j) >= (16 - y_delay*2))
						{
							if (vis)
								FrameBuffer[(loc_y * 2 + j - (16 - y_delay * 2)) * 159 + loc_x + k - (8 - x_delay)] = ColorToRGBA(loc_color);

							//a MOB does not need to be visible for it to be interracting
							if (intr)
								Collision[loc_x + k, loc_y * 2 + j] |= (ushort)(1 << i);
						}
					}
				}

				if (yres)
				{
					for (int j = 0; j < 8; j++)
					{
						for (int k = 0; k < 8; k++)
						{
							bool pixel = y_mobs[j].Bit(7 - k);

							if ((loc_x + k) < (167-x_delay) && ((loc_y + 4) * 2 + j) < (208-y_delay*2) && pixel && (loc_x + k) >= (8 - x_delay) && ((loc_y + 4) * 2 + j) >= (16 - y_delay * 2))
							{
								if (vis)
									FrameBuffer[((loc_y + 4) * 2 + j - (16 - y_delay * 2)) * 159 + loc_x + k - (8 - x_delay)] = ColorToRGBA(loc_color);
								
								//a MOB does not need to be visible for it to be interracting
								if (intr)
									Collision[loc_x + k, (loc_y+4) * 2 + j] |= (ushort)(1 << i);
							}
						}
					}
				}
			}
			
			// by now we have collision information for all 8 mobs and the BG
			// so we can store data in the collision registers here
			
			for (int i = 0;i<159;i++)
			{
				for (int j=0;j<192;j++)
				{
					for (int k=0;k<8;k++)
					{
						for (int m=0;m<9;m++)
						{
							if (k!=m) // mobs never self interact
							{
								Register[k + 24] |= (ushort)((Collision[i, j].Bit(k) && Collision[i, j].Bit(m)) ? 1<<m : 0);
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
