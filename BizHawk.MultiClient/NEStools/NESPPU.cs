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
		//Pattern viewer - 
		//      Row interleaving
		//      option for 2x view (and 4x?)
		//      Mouse over - Usage (BG vs Sprite usage)
		//Sprite viewer
		//Nametable viewer

		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		NES Nes;

		NES.PPU.DebugCallback Callback = new NES.PPU.DebugCallback();

		public NESPPU()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			Callback.Callback = () => Generate();
		}

		private void SaveConfigSettings()
		{
			Global.Config.NESPPUWndx = this.Location.X;
			Global.Config.NESPPUWndy = this.Location.Y;
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) this.Close();
			if (!this.IsHandleCreated || this.IsDisposed) return;
			Nes = Global.Emulator as NES;
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
			byte value = Nes.ppu.ppubus_peek(address);
			return (byte)(((value >> (7 - bit)) & 1));
		}

		unsafe void Generate()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			//Pattern Viewer
			for (int x = 0; x < 16; x++)
			{
				PaletteView.bgPalettesPrev[x] = new PaletteViewer.Palette(PaletteView.bgPalettes[x]);
				PaletteView.spritePalettesPrev[x] = new PaletteViewer.Palette(PaletteView.spritePalettes[x]);
				PaletteView.bgPalettes[x].SetValue(Nes.LookupColor(Nes.ppu.PALRAM[PaletteView.bgPalettes[x].address]));
				PaletteView.spritePalettes[x].SetValue(Nes.LookupColor(Nes.ppu.PALRAM[PaletteView.spritePalettes[x].address]));
			}
			if (PaletteView.HasChanged())
				PaletteView.Refresh();

			//Pattern Viewer
			int b0 = 0;
			int b1 = 0;
			byte value;
			int cvalue;
			int pal;

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
								b0 = GetBit((z * 0x1000) + (i * 256) + (j * 16) + y + 0 * 8, x);
								b1 = GetBit((z * 0x1000) + (i * 256) + (j * 16) + y + 1 * 8, x);

								value = (byte)(b0 + (b1 << 1));

								cvalue = Nes.LookupColor(Nes.ppu.PALRAM[value + (pal * 4)]);

								Color color = Color.FromArgb(cvalue);

								int adr = (x + (j * 8)) + (y + (i * 8)) * (bmpdata.Stride / 4);
								framebuf[adr + (z * 128)] = color.ToArgb();
							}
						}
					}
				}
			}
			PatternView.pattern.UnlockBits(bmpdata);
			PatternView.Refresh();
			/*
			int SpriteNum, TileNum, Attr, MemAddr;

			//Sprite Viewer
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					SpriteNum = (y << 4) | x;
					TileNum = 0; //TODO
					Attr = 0; //TODO
					if (((int)Nes.ppu.reg_2000.Value & (int)0x20) > 0) //TODO why is C# being retarded about using & with a byte?
					{
						MemAddr = ((TileNum & 0xFE) << 4) | ((TileNum & 0x01) << 12);
						//DrawTile(SprArray + y * 24 * D_SPR_W + x * 16, MemAddr, 4 | (Attr & 3), D_SPR_W);
						//DrawTile(SprArray + y * 24 * D_SPR_W + x * 16 + 8 * D_SPR_W, MemAddr + 16, 4 | (Attr & 3), D_SPR_W);

					}
					else
					{
						MemAddr = (TileNum << 4) | ((Nes.ppu.reg_2000.Value & (byte)0x08) << 9);
						//DrawTile(SprArray + y * 24 * D_SPR_W + x * 16, MemAddr, 4 | (Attr & 3), D_SPR_W);
					}
				}
			}
			 * */
		}

		public unsafe void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is NES)) return;
			//NES.PPU ppu = (Global.Emulator as NES).ppu;
			Nes.ppu.PPUViewCallback = Callback;
		}

		private void NESPPU_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			Nes = Global.Emulator as NES;
			ClearDetails();
		}

		private void ClearDetails()
		{
			SectionLabel.Text = "";
			AddressLabel.Text = "";
			ValueLabel.Text = "";
			Value2Label.Text = "";
		}

		private void PaletteView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void PaletteView_MouseEnter(object sender, EventArgs e)
		{
			SectionLabel.Text = "Section: Palette";
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
			if (baseAddr == 0x3F00)
				val = PaletteView.bgPalettes[column].GetValue();
			else
				val = PaletteView.spritePalettes[column].GetValue();
			ValueLabel.Text = "Color: 0x" + String.Format("{0:X2}", val, NumberStyles.HexNumber);

			if (baseAddr == 0x3F00)
				Value2Label.Text = "ID: BG" + (column / 4).ToString();
			else
				Value2Label.Text = "ID: SPR" + (column / 4).ToString();
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
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESPPU;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESPPUSaveWindowPosition;
		}

		private void PatternView_Click(object sender, MouseEventArgs e)
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

		private void UpdateTableLabels()
		{
			Table0PaletteLabel.Text = "Palette: " + PatternView.Pal0;
			Table1PaletteLabel.Text = "Palette: " + PatternView.Pal1;
			PatternView.Refresh();
		}

		private void PatternView_MouseEnter(object sender, EventArgs e)
		{
			SectionLabel.Text = "Section: Pattern";
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
				tile = address = (e.X - 1) / 8;

			}
			else
			{
				address = 0x1000 + ((e.X - 128) / 8);
				tile = (e.X - 128) / 8;
			}

			address += (e.Y / 8) * 256;
			tile += (e.Y / 8) * 16;

			AddressLabel.Text = "Address: " + String.Format("{0:X4}", address);
			ValueLabel.Text = "Table " + table.ToString();
			Value2Label.Text = "Tile " + String.Format("{0:X2}", tile);
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
	}
}
