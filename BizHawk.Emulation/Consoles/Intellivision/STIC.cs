using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class STIC : IVideoProvider
	{
		private bool Sr1, Sr2, Sst, Fgbg = false;
		private ushort[] Register = new ushort[64];
		private ushort ColorSP = 0x0028;

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, ushort> ReadMemory;
		public Func<ushort, ushort, bool> WriteMemory;

		public int[] FrameBuffer = new int[159 * 96];

		public int[] GetVideoBuffer()
		{
			Background();
			Mobs();
			return FrameBuffer; 
		}

		public int VirtualWidth { get { return 159; } }
		public int BufferWidth { get { return 159; } }
		public int VirtualHeight { get { return 192; } }
		public int BufferHeight { get { return 96; } }
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

		public int GetPendingCycles()
		{
			return PendingCycles;
		}

		public void AddPendingCycles(int cycles)
		{
			PendingCycles += cycles;
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
					AddPendingCycles(14934 - 3791);
				}
				else
				{
					AddPendingCycles(3791);
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
									FrameBuffer[pixel] = ColorToRGBA(colors[color]);
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
								FrameBuffer[pixel] = ColorToRGBA(fg);
							}
							else
							{
								FrameBuffer[pixel] = ColorToRGBA(bg);
							}
							row >>= 1;
						}
					}
				}
			}
		}

		public void Mobs()
		{
			// TODO
		}
	}
}
