using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using BizHawk.Emulation.Consoles.Nintendo;
using System.Diagnostics;

namespace BizHawk.MultiClient
{
	public partial class NESPPU : Form
	{
		//TODO:
		//If 8/16 sprite mode, mouse over should put 32x64 version of prite

		//Speedups
		//Smarter refreshing?  only refresh when things of changed, perhaps peek at the ppu to when the pattern table has changed, or sprites have moved
		//Maybe 48 individual bitmaps for sprites is faster than the overhead of redrawing all that transparent space

		Bitmap ZoomBoxDefaultImage = new Bitmap(64, 64);
		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		NES Nes;

		byte[] PPUBus = new byte[0x2000];
		byte[] PPUBusprev = new byte[0x2000];
		byte[] PALRAM = new byte[0x20];
		byte[] PALRAMprev = new byte[0x20];

		NES.PPU.DebugCallback Callback = new NES.PPU.DebugCallback();

		public NESPPU()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			Callback.Callback = () => Generate();
			for (int x = 0; x < 0x2000; x++)
			{
				PPUBus[x] = 0;
				PPUBusprev[x] = 0;
			}

			for (int x = 0; x < 0x20; x++)
			{
				PALRAM[x] = 0;
				PALRAMprev[x] = 0;
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.NESPPUWndx = this.Location.X;
			Global.Config.NESPPUWndy = this.Location.Y;
			Global.Config.NESPPURefreshRate = RefreshRate.Value;
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) this.Close();
			if (!this.IsHandleCreated || this.IsDisposed) return;
			Nes = Global.Emulator as NES;
			Generate(true);
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.NESPPUSaveWindowPosition && Global.Config.NESPPUWndx >= 0 && Global.Config.NESPPUWndy >= 0)
				Location = new Point(Global.Config.NESPPUWndx, Global.Config.NESPPUWndy);
		}

		private byte GetBit(int address, int bit)
		{
			return (byte)(((PPUBus[address] >> (7 - bit)) & 1));
		}

		unsafe void Generate(bool now = false)
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			if (Global.Emulator.Frame % RefreshRate.Value == 0 || now)
			{
				bool Changed = false;

				for (int x = 0; x < 0x20; x++)
				{
					PALRAMprev[x] = PALRAM[x];
					PALRAM[x] = Nes.ppu.PALRAM[x];
					if (PALRAM[x] != PALRAMprev[x])
					{
						Changed = true;
					}
				}
				
				if (!Changed)
				{
					for (int x = 0; x < 0x2000; x++)
					{
						PPUBusprev[x] = PPUBus[x];
						PPUBus[x] = Nes.ppu.ppubus_peek(x);
						if (PPUBus[x] != PPUBusprev[x])
						{
							Changed = true;
						}
					}
				}

				int b0 = 0;
				int b1 = 0;
				byte value;
				int cvalue;

				if (Changed)
				{
					//Pattern Viewer
					int pal;
					for (int x = 0; x < 16; x++)
					{
						PaletteView.bgPalettesPrev[x].Value = PaletteView.bgPalettes[x].Value;
						PaletteView.spritePalettesPrev[x].Value = PaletteView.spritePalettes[x].Value;
						PaletteView.bgPalettes[x].Value = Nes.LookupColor(Nes.ppu.PALRAM[PaletteView.bgPalettes[x].Address]);
						PaletteView.spritePalettes[x].Value = Nes.LookupColor(Nes.ppu.PALRAM[PaletteView.spritePalettes[x].Address]);
					}
					if (PaletteView.HasChanged())
					{
						PaletteView.Refresh();
					}

					System.Drawing.Imaging.BitmapData bmpdata = PatternView.pattern.LockBits(new Rectangle(new Point(0, 0), PatternView.pattern.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					int* framebuf = (int*)bmpdata.Scan0.ToPointer();
					for (int z = 0; z < 2; z++)
					{
						if (z == 0)
							pal = PatternView.Pal0;
						else
							pal = PatternView.Pal1;

						for (int i = 0; i < 16; i++)
						{
							for (int j = 0; j < 16; j++)
							{
								for (int x = 0; x < 8; x++)
								{
									for (int y = 0; y < 8; y++)
									{
										int address = (z << 12) + (i << 8) + (j << 4) + y;
										b0 = (byte)(((PPUBus[address] >> (7 - x)) & 1));
										b1 = (byte)(((PPUBus[address + 8] >> (7 - x)) & 1));

										value = (byte)(b0 + (b1 << 1));
										cvalue = Nes.LookupColor(Nes.ppu.PALRAM[value + (pal << 2)]);
										int adr = (x + (j << 3)) + (y + (i << 3)) * (bmpdata.Stride >> 2);
										framebuf[adr + (z << 7)] = cvalue;
									}
								}
							}
						}
					}
					PatternView.pattern.UnlockBits(bmpdata);
					PatternView.Refresh();
				}

				System.Drawing.Imaging.BitmapData bmpdata2 = SpriteView.sprites.LockBits(new Rectangle(new Point(0, 0), SpriteView.sprites.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				int* framebuf2 = (int*)bmpdata2.Scan0.ToPointer();
				int BaseAddr, TileNum, Attributes, Palette;

				int pt_add = Nes.ppu.reg_2000.obj_pattern_hi ? 0x1000 : 0;
				bool is8x16 = Nes.ppu.reg_2000.obj_size_16;


				//Sprite Viewer
				for (int n = 0; n < 4; n++)
				{
					for (int r = 0; r < 16; r++)
					{
						BaseAddr = (r << 2) +  (n << 6);
						TileNum = Nes.ppu.OAM[BaseAddr + 1];
						int PatAddr = 0;

						if (is8x16)
						{
							PatAddr = ((TileNum >> 1) * 0x20);
							PatAddr += (0x1000 * (TileNum & 1));
						}
						else
						{
							PatAddr = TileNum * 0x10;
							PatAddr += pt_add;
						}


						Attributes = Nes.ppu.OAM[BaseAddr + 2];
						Palette = Attributes & 0x03;

						for (int x = 0; x < 8; x++)
						{
							for (int y = 0; y < 8; y++)
							{
								int address = PatAddr + y;
								b0 = (byte)(((PPUBus[address] >> (7 - x)) & 1));
								b1 = (byte)(((PPUBus[address + 8] >> (7 - x)) & 1));
								value = (byte)(b0 + (b1 << 1));
								cvalue = Nes.LookupColor(Nes.ppu.PALRAM[16 + value + (Palette << 2)]);

								int adr = (x + (r * 16)) + (y + (n * 24)) * (bmpdata2.Stride >> 2);
								framebuf2[adr] = cvalue;
							}
							if (is8x16)
							{
								PatAddr += 0x10;
								for (int y = 0; y < 8; y++)
								{
									int address = PatAddr + y;
									b0 = (byte)(((PPUBus[address] >> (7 - x)) & 1));
									b1 = (byte)(((PPUBus[address + 8] >> (7 - x)) & 1));
									value = (byte)(b0 + (b1 << 1));
									cvalue = Nes.LookupColor(Nes.ppu.PALRAM[16 + value + (Palette << 2)]);

									int adr = (x + (r << 4)) + ((y+8) + (n * 24)) * (bmpdata2.Stride >> 2);
									framebuf2[adr] = cvalue;
								}
								PatAddr -= 0x10;
							}
						}
					}
				}
				SpriteView.sprites.UnlockBits(bmpdata2);
				SpriteView.Refresh();
			}
		}

		public unsafe void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is NES)) return;
			Nes.ppu.PPUViewCallback = Callback;
		}

		private void NESPPU_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			Nes = Global.Emulator as NES;
			ClearDetails();
			RefreshRate.Value = Global.Config.NESPPURefreshRate;
			Generate(true);
		}

		private void ClearDetails()
		{
			DetailsBox.Text = "Details";
			AddressLabel.Text = "";
			ValueLabel.Text = "";
			Value2Label.Text = "";
			Value3Label.Text = "";
			Value4Label.Text = "";
			Value5Label.Text = "";
			ZoomBox.Image = ZoomBoxDefaultImage;
		}

		private void PaletteView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void PaletteView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Palettes";
		}

		private void PaletteView_MouseMove(object sender, MouseEventArgs e)
		{
			int baseAddr = 0x3F00;
			if (e.Y > 16)
				baseAddr += 16;
			int column = (e.X - PaletteView.Location.X) / 16;
			int addr = column + baseAddr;
			AddressLabel.Text = "Address: 0x" + String.Format("{0:X4}", addr, NumberStyles.HexNumber);
			int val;
			int offset = addr & 0x03;

			Bitmap bmp = new Bitmap(64, 64);
			Graphics g= Graphics.FromImage(bmp);

			if (baseAddr == 0x3F00)
			{
				val = Nes.ppu.PALRAM[PaletteView.bgPalettes[column].Address];
				ValueLabel.Text = "ID: BG" + (column / 4).ToString();
				g.FillRectangle(new SolidBrush(PaletteView.bgPalettes[column].Color), 0, 0, 64, 64);
			}
			else
			{
				val = Nes.ppu.PALRAM[PaletteView.spritePalettes[column].Address];
				ValueLabel.Text = "ID: SPR" + (column / 4).ToString();
				g.FillRectangle(new SolidBrush(PaletteView.spritePalettes[column].Color), 0, 0, 64, 64);
			}
			g.Dispose();

			Value3Label.Text = "Color: 0x" + String.Format("{0:X2}", val, NumberStyles.HexNumber);
			Value4Label.Text = "Offset: " + offset.ToString();
			ZoomBox.Image = bmp;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadNESPPU ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESPPUSaveWindowPosition ^= true;
		}

		private void toolStripDropDownButton1_DropDownOpened(object sender, EventArgs e)
		{
			autoLoadToolStripMenuItem1.Checked = Global.Config.AutoLoadNESPPU;
			saveWindowPositionToolStripMenuItem1.Checked = Global.Config.NESPPUSaveWindowPosition;
		}

		private void PatternView_Click(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (e.X < PatternView.Width / 2)
				{
					PatternView.Pal0++;
					if (PatternView.Pal0 > 7) PatternView.Pal0 = 0;
				}
				else
				{
					PatternView.Pal1++;
					if (PatternView.Pal1 > 7) PatternView.Pal1 = 0;
				}
				UpdateTableLabels();
			}
			HandleDefaultImage(e);
		}

		private void UpdateTableLabels()
		{
			Table0PaletteLabel.Text = "Palette: " + PatternView.Pal0;
			Table1PaletteLabel.Text = "Palette: " + PatternView.Pal1;
		}

		private void PatternView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Patterns";
		}

		private void PatternView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void PatternView_MouseMove(object sender, MouseEventArgs e)
		{
			int table = 0;
			int address = 0;
			int tile = 0;
			if (e.X > PatternView.Width / 2)
				table = 1;

			if (table == 0)
			{
				tile = (e.X - 1) / 8;
				address = tile * 16;

			}
			else
			{
				tile = (e.X - 128) / 8;
				address = 0x1000 + (tile * 16);
				
			}

			address += (e.Y / 8) * 256;
			tile += (e.Y / 8) * 16;
			string Usage = "Usage: ";

			if ((Nes.ppu.reg_2000.Value & 0x10) << 4 == ((address >> 4) & 0x100))
				Usage = "BG";
			else if (((Nes.ppu.reg_2000.Value & 0x08) << 5) == ((address >> 4) & 0x100))
				Usage = "SPR";

			if ((Nes.ppu.reg_2000.Value & 0x20) > 0)
				Usage += " (SPR16)";
			
			AddressLabel.Text = "Address: " + String.Format("{0:X4}", address);
			ValueLabel.Text = "Table " + table.ToString();
			Value3Label.Text = "Tile " + String.Format("{0:X2}", tile);
			Value4Label.Text = Usage;

			ZoomBox.Image = Section(PatternView.pattern, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 8) * 8), new Size(8, 8)), false);
		}

		static public Bitmap Section(Bitmap srcBitmap, Rectangle section, bool Is8x16)
		{
			// Create the new bitmap and associated graphics object
			Bitmap bmp = new Bitmap(64, 64);
			Graphics g = Graphics.FromImage(bmp);

			// Draw the specified section of the source bitmap to the new one
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			Rectangle rect;
			if (Is8x16)
				rect = new Rectangle(0, 0, 32, 64);
			else
				rect = new Rectangle(0, 0, 64, 64);
			g.DrawImage(srcBitmap, rect, section, GraphicsUnit.Pixel);
			g.Dispose();

			// Return the bitmap
			return bmp;
		}

		private void toolStripDropDownButton2_DropDownOpened(object sender, EventArgs e)
		{
			Table0P0.Checked = false;
			Table0P1.Checked = false;
			Table0P2.Checked = false;
			Table0P3.Checked = false;
			Table0P4.Checked = false;
			Table0P5.Checked = false;
			Table0P6.Checked = false;
			Table0P7.Checked = false;
			Table1P0.Checked = false;
			Table1P1.Checked = false;
			Table1P2.Checked = false;
			Table1P3.Checked = false;
			Table1P4.Checked = false;
			Table1P5.Checked = false;
			Table1P6.Checked = false;
			Table1P7.Checked = false;

			Table0P0.Checked = false;

			switch (PatternView.Pal0)
			{
				case 0:
					Table0P0.Checked = true;
					break;
				case 1:
					Table0P1.Checked = true;
					break;
				case 2:
					Table0P2.Checked = true;
					break;
				case 3:
					Table0P3.Checked = true;
					break;
				case 4:
					Table0P4.Checked = true;
					break;
				case 5:
					Table0P5.Checked = true;
					break;
				case 6:
					Table0P6.Checked = true;
					break;
				case 7:
					Table0P7.Checked = true;
					break;
			}

			switch (PatternView.Pal1)
			{
				case 0:
					Table1P0.Checked = true;
					break;
				case 1:
					Table1P1.Checked = true;
					break;
				case 2:
					Table1P2.Checked = true;
					break;
				case 3:
					Table1P3.Checked = true;
					break;
				case 4:
					Table1P4.Checked = true;
					break;
				case 5:
					Table1P5.Checked = true;
					break;
				case 6:
					Table1P6.Checked = true;
					break;
				case 7:
					Table1P7.Checked = true;
					break;
			}
		}

		private void Palette_Click(object sender, EventArgs e)
		{
			if (sender == Table0P0) PatternView.Pal0 = 0;
			if (sender == Table0P1) PatternView.Pal0 = 1;
			if (sender == Table0P2) PatternView.Pal0 = 2;
			if (sender == Table0P3) PatternView.Pal0 = 3;
			if (sender == Table0P4) PatternView.Pal0 = 4;
			if (sender == Table0P5) PatternView.Pal0 = 5;
			if (sender == Table0P6) PatternView.Pal0 = 6;
			if (sender == Table0P7) PatternView.Pal0 = 7;

			if (sender == Table1P0) PatternView.Pal1 = 0;
			if (sender == Table1P1) PatternView.Pal1 = 1;
			if (sender == Table1P2) PatternView.Pal1 = 2;
			if (sender == Table1P3) PatternView.Pal1 = 3;
			if (sender == Table1P4) PatternView.Pal1 = 4;
			if (sender == Table1P5) PatternView.Pal1 = 5;
			if (sender == Table1P6) PatternView.Pal1 = 6;
			if (sender == Table1P7) PatternView.Pal1 = 7;

			UpdateTableLabels();
		}

		private void txtScanline_TextChanged(object sender, EventArgs e)
		{
			int temp = 0;
			if (int.TryParse(txtScanline.Text, out temp))
			{
				Callback.Scanline = temp;
			}
		}

		private void NESPPU_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (Nes == null) return;
			if (Nes.ppu.PPUViewCallback == Callback)
				Nes.ppu.PPUViewCallback = null;
		}

		private void SpriteView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Sprites";
		}

		private void SpriteView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void SpriteView_MouseMove(object sender, MouseEventArgs e)
		{
			bool is8x16 = Nes.ppu.reg_2000.obj_size_16;
			int SpriteNumber = ((e.Y / 24) * 16) + (e.X / 16);
			int X = Nes.ppu.OAM[(SpriteNumber * 4) + 3];
			int Y = Nes.ppu.OAM[SpriteNumber * 4];
			int Color = Nes.ppu.OAM[(SpriteNumber * 4) + 2] & 0x03;
			int Attributes = Nes.ppu.OAM[(SpriteNumber * 4) + 2];

			string flags = "Flags: ";
			int h = GetBit(Attributes, 6);
			int v = GetBit(Attributes, 7);
			int priority = GetBit(Attributes, 5);
			if (h > 0)
				flags += "H ";
			if (v > 0)
				flags += "V ";
			if (priority > 0)
				flags += "Behind";
			else
				flags += "Front";

			int Tile = Nes.ppu.OAM[SpriteNumber * 1]; ;

			AddressLabel.Text = "Number: " + String.Format("{0:X2}", SpriteNumber);
			ValueLabel.Text = "X: " + String.Format("{0:X2}", X);
			Value2Label.Text = "Y: " + String.Format("{0:X2}", Y);
			Value3Label.Text = "Tile: " + String.Format("{0:X2}", Tile);
			Value4Label.Text = "Color: " + Color.ToString();
			Value5Label.Text = flags;

			if (is8x16)
				ZoomBox.Image = Section(SpriteView.sprites, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 24) * 24), new Size(8, 16)), true);
			else
				ZoomBox.Image = Section(SpriteView.sprites, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 8) * 8), new Size(8, 8)), false);
		}

		private void PaletteView_MouseClick(object sender, MouseEventArgs e)
		{
			HandleDefaultImage(e);
		}

		private void SpriteView_MouseClick(object sender, MouseEventArgs e)
		{
			HandleDefaultImage(e);
		}

		private void HandleDefaultImage(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				ZoomBoxDefaultImage = ZoomBox.Image as Bitmap;
			}
		}

		private void NESPPU_MouseClick(object sender, MouseEventArgs e)
		{
			ZoomBox.Image = new Bitmap(64, 64);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void savePaletteScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.Screenshot();
		}

		private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.Screenshot();
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.Refresh();
		}

		private void saveImageToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PatternView.Screenshot();
		}

		private void refreshToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PatternView.Refresh();
		}

		private void saveImageToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			SpriteView.Screenshot();
		}

		private void refreshToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			SpriteView.Refresh();
		}

		private void imageToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SpriteView.ScreenshotToClipboard();
		}

		private void imageToClipboardToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PatternView.ScreenshotToClipboard();
		}

		private void imageToClipboardToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			PaletteView.ScreenshotToClipboard();
		}

		private void savePaletteToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.ScreenshotToClipboard();
		}

		private void copyPatternToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PatternView.ScreenshotToClipboard();
		}

		private void copySpriteToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SpriteView.ScreenshotToClipboard();
		}
	}
}
