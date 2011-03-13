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
		//      Proper color reading
		//      option for 2x view (and 4x?)
		//      Mouse over events
		//      Drop down menu for pattern viewer palette selections
		//Sprite viewer
		//Nametable viewer

		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		NES Nes;

		public NESPPU()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
		}

		private void SaveConfigSettings()
		{
			Global.Config.NESPPUWndx = this.Location.X;
			Global.Config.NESPPUWndy = this.Location.Y;
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) this.Close();
			Nes = Global.Emulator as NES;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.NESPPUWndx >= 0 && Global.Config.NESPPUWndy >= 0)
				Location = new Point(Global.Config.NESPPUWndx, Global.Config.NESPPUWndy);
		}

		private byte GetBit(int address, int bit)
		{
			byte value = Nes.ppu.ppubus_read(address);
			return (byte)(((value >> (7 - bit)) & 1));
		}

		public unsafe void UpdateValues()
		{
			if (!(Global.Emulator is NES)) return;
			if (!this.IsHandleCreated || this.IsDisposed) return;

			//Pattern Viewer
			for (int x = 0; x < 16; x++)
			{
				PaletteView.bgPalettes[x].SetValue(Nes.ConvertColor(Nes.ppu.PALRAM[PaletteView.bgPalettes[x].address]));
				PaletteView.spritePalettes[x].SetValue(Nes.ppu.PALRAM[PaletteView.spritePalettes[x].address]);
			}
			PaletteView.Refresh();

			//Pattern Viewer
			System.Drawing.Imaging.BitmapData bmpdata = PatternView.pattern.LockBits(new Rectangle(new Point(0, 0), PatternView.pattern.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* framebuf = (int*)bmpdata.Scan0.ToPointer();
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					for (int x = 0; x < 8; x++)
					{
						for (int y = 0; y < 8; y++)
						{
							Bit b0 = new Bit();
							Bit b1 = new Bit();

							Bit b2 = new Bit();
							Bit b3 = new Bit(); //2nd page of patterns 

							b0 = GetBit((i * 256) + (j * 16) + y + b0 * 8, x);
							b1 = GetBit((i * 256) + (j * 16) + y + b1 * 8, x);
							b2 = GetBit(0x1000 + (i * 256) + (j * 16) + y + b2 * 8, x);
							b3 = GetBit(0x1000 + (i * 256) + (j * 16) + y + b3 * 8, x);
							byte value = (byte)(b0 + (b1 * 2));
							byte value2 = (byte)(b2 + (b3 * 2));

							value += Nes.ppu.PALRAM[value + (PatternView.Pal0 * 4)];     //TODO: 0 = user selection 0-7
							value2 += Nes.ppu.PALRAM[value2 + (PatternView.Pal1 * 4)];    //TODO: 0 = user selection 0-7

							int cvalue = Nes.ConvertColor(value);
							int cvalue2 = Nes.ConvertColor(value2);
							unchecked
							{
								cvalue = cvalue | (int)0xFF000000;
								cvalue2 = cvalue2 | (int)0xFF000000;
							}
							Color color = Color.FromArgb(cvalue);
							Color color2 = Color.FromArgb(cvalue2);
							int adr = (x + (j * 8)) + (y + (i * 8)) * (bmpdata.Stride / 4);
							framebuf[adr] = color.ToArgb();
							framebuf[adr + 128] = color2.ToArgb();
						}
					}
				}
			}
			PatternView.pattern.UnlockBits(bmpdata);
			PatternView.Refresh();
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
			//TODO: more info labels
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

        private void PatternView_Click(object sender, EventArgs e)
        {
            Table1PaletteLabel.Text = "Palette: " + PatternView.Pal0;
            Table2PaletteLabel.Text = "Palette: " + PatternView.Pal1;
        }
	}
}
