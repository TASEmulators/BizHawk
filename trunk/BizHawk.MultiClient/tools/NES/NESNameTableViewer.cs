using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESNameTableViewer : Form
	{
		//TODO:
		//Show Scroll Lines + UI Toggle

		private NES _nes;
		private readonly NES.PPU.DebugCallback Callback = new NES.PPU.DebugCallback();

		public NESNameTableViewer()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			Callback.Callback = () => Generate();
		}

		private void SaveConfigSettings()
		{
			Global.Config.NESNameTableWndx = Location.X;
			Global.Config.NESNameTableWndy = Location.Y;
			Global.Config.NESNameTableRefreshRate = RefreshRate.Value;
		}

		unsafe void Generate(bool now = false)
		{
			if (!IsHandleCreated || IsDisposed) return;
			if (_nes == null) return;

			if (now == false)
			{
				if (Global.Emulator.Frame % RefreshRate.Value != 0) return;
			}

			BitmapData bmpdata = NameTableView.Nametables.LockBits(new Rectangle(0, 0, 512, 480), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dptr = (int*)bmpdata.Scan0.ToPointer();
			int pitch = bmpdata.Stride / 4;
			int pt_add = _nes.ppu.reg_2000.bg_pattern_hi ? 0x1000 : 0;

			//buffer all the data from the ppu, because it will be read multiple times and that is slow
			byte[] p = new byte[0x3000];
			for (int x = 0; x < 0x3000; x++)
				p[x] = _nes.ppu.ppubus_peek(x);

			byte[] palram = new byte[0x20];
			for (int x = 0; x < 0x20; x++)
				palram[x] = _nes.ppu.PALRAM[x];

			int ytable = 0, yline = 0;
			for (int y = 0; y < 480; y++)
			{
				if (y == 240)
				{
					ytable += 2;
					yline = 240;
				}
				for (int x = 0; x < 512; x++, dptr++)
				{
					int table = (x >> 8) + ytable;
					int ntaddr = (table << 10);
					int px = x & 255;
					int py = y - yline;
					int tx = px >> 3;
					int ty = py >> 3;
					int ntbyte_ptr = ntaddr + (ty * 32) + tx;
					int atbyte_ptr = ntaddr + 0x3C0 + ((ty >> 2) << 3) + (tx >> 2);
					int nt = p[ntbyte_ptr + 0x2000];

					int at = p[atbyte_ptr + 0x2000];
					if ((ty & 2) != 0) at >>= 4;
					if ((tx & 2) != 0) at >>= 2;
					at &= 0x03;
					at <<= 2;

					int bgpx = x & 7;
					int bgpy = y & 7;
					int pt_addr = (nt << 4) + bgpy + pt_add;
					int pt_0 = p[pt_addr];
					int pt_1 = p[pt_addr + 8];
					int pixel = ((pt_0 >> (7 - bgpx)) & 1) | (((pt_1 >> (7 - bgpx)) & 1) << 1);

					//if the pixel is transparent, draw the backdrop color
					//TODO - consider making this optional? nintendulator does it and fceux doesnt need to do it due to buggy palette logic which creates the same effect
					if (pixel != 0)
						pixel |= at;

					pixel = palram[pixel];
					int cvalue = _nes.LookupColor(pixel);
					*dptr = cvalue;
				}
				dptr += pitch - 512;
			}

			NameTableView.Nametables.UnlockBits(bmpdata);
			NameTableView.Refresh();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;
			if (!(Global.Emulator is NES)) return;
			NES.PPU ppu = (Global.Emulator as NES).ppu;
			ppu.NTViewCallback = Callback;
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) Close();
			_nes = Global.Emulator as NES;
			Generate(true);
		}

		private void NESNameTableViewer_Load(object sender, EventArgs e)
		{
			if (Global.Config.NESNameTableSaveWindowPosition && Global.Config.NESNameTableWndx >= 0 && Global.Config.NESNameTableWndy >= 0)
				Location = new Point(Global.Config.NESNameTableWndx, Global.Config.NESNameTableWndy);

			_nes = Global.Emulator as NES;
			RefreshRate.Value = Global.Config.NESNameTableRefreshRate;
			Generate(true);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadNESNameTable ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESNameTableSaveWindowPosition ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESNameTable;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESNameTableSaveWindowPosition;
		}

		private void txtScanline_TextChanged(object sender, EventArgs e)
		{
			int temp;
			if (int.TryParse(txtScanline.Text, out temp))
			{
				Callback.Scanline = temp;
			}
		}

		private void NESNameTableViewer_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (_nes == null) return;
			if (_nes.ppu.NTViewCallback == Callback)
				_nes.ppu.NTViewCallback = null;
		}


		private void rbNametable_CheckedChanged(object sender, EventArgs e)
		{
			if (rbNametableNW.Checked) NameTableView.Which = NameTableViewer.WhichNametable.NT_2000;
			if (rbNametableNE.Checked) NameTableView.Which = NameTableViewer.WhichNametable.NT_2400;
			if (rbNametableSW.Checked) NameTableView.Which = NameTableViewer.WhichNametable.NT_2800;
			if (rbNametableSE.Checked) NameTableView.Which = NameTableViewer.WhichNametable.NT_2C00;
			if (rbNametableAll.Checked) NameTableView.Which = NameTableViewer.WhichNametable.NT_ALL;
		}

		private void NameTableView_MouseMove(object sender, MouseEventArgs e)
		{
			int TileX, TileY, NameTable;
			if (NameTableView.Which == NameTableViewer.WhichNametable.NT_ALL)
			{
				TileX = e.X / 8;
				TileY = e.Y / 8;
				NameTable = (TileX / 32) + ((TileY / 30) * 2);
			}
			else
			{
				switch (NameTableView.Which)
				{
					default:
					case NameTableViewer.WhichNametable.NT_2000:
						NameTable = 0;
						break;
					case NameTableViewer.WhichNametable.NT_2400:
						NameTable = 1;
						break;
					case NameTableViewer.WhichNametable.NT_2800:
						NameTable = 2;
						break;
					case NameTableViewer.WhichNametable.NT_2C00:
						NameTable = 3;
						break;
				}

				TileX = e.X / 16;
				TileY = e.Y / 16;
			}
			
			XYLabel.Text = TileX.ToString() + " : " + TileY.ToString();
			int PPUAddress = 0x2000 + (NameTable * 0x400) + ((TileY % 30) * 32) + (TileX % 32);
			PPUAddressLabel.Text = String.Format("{0:X4}", PPUAddress);
			int TileID = _nes.ppu.ppubus_read(PPUAddress, true);
			TileIDLabel.Text = String.Format("{0:X2}", TileID);
			TableLabel.Text = NameTable.ToString();

			int ytable = 0, yline = 0;
			if (e.Y >= 240)
			{
				ytable += 2;
				yline = 240;
			}
			int table = (e.X >> 8) + ytable;
			int ntaddr = (table << 10);
			int px = e.X & 255;
			int py = e.Y - yline;
			int tx = px >> 3;
			int ty = py >> 3;
			int atbyte_ptr = ntaddr + 0x3C0 + ((ty >> 2) << 3) + (tx >> 2);
			int at = _nes.ppu.ppubus_peek(atbyte_ptr + 0x2000);
			if ((ty & 2) != 0) at >>= 4;
			if ((tx & 2) != 0) at >>= 2;
			at &= 0x03;
			PaletteLabel.Text = at.ToString();
		}

		private void NameTableView_MouseLeave(object sender, EventArgs e)
		{
			XYLabel.Text = "";
			PPUAddressLabel.Text = "";
			TileIDLabel.Text = "";
			TableLabel.Text = "";
			PaletteLabel.Text = "";
		}

		private void screenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.Screenshot();
		}

		private void screenshotAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.Screenshot();
		}

		private void refreshImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			UpdateValues();
			NameTableView.Refresh();
		}

		private void saveImageClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.ScreenshotToClipboard();
		}

		private void NESNameTableViewer_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.C:
					if (e.Modifiers == Keys.Control)
					{
						NameTableView.ScreenshotToClipboard();
					}
					break;
			}
		}

		private void screenshotToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.ScreenshotToClipboard();
		}
	}
}
