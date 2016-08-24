using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESNameTableViewer : Form, IToolFormAutoConfig
	{
		// TODO:
		// Show Scroll Lines + UI Toggle
		[RequiredService]
		private INESPPUViewable _ppu { get; set; }
		[RequiredService]
		private IEmulator _emu { get; set; }

		[ConfigPersist]
		private int RefreshRateConfig
		{
			get { return RefreshRate.Value; }
			set { RefreshRate.Value = value; }
		}

		int scanline;

		public NESNameTableViewer()
		{
			InitializeComponent();
		}

		private void NESNameTableViewer_Load(object sender, EventArgs e)
		{
			Generate(true);
		}

		#region Public API

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return true; } }

		public void Restart()
		{
			Generate(true);
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			_ppu.InstallCallback1(() => Generate(), scanline);
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		#endregion

		private unsafe void DrawTile(int* dst, int pitch, byte* pal, byte* tile, int* finalpal)
		{
			dst += 7;
			int vinc = pitch + 8;
			for (int j = 0; j < 8; j++)
			{
				int lo = tile[0];
				int hi = tile[8] << 1;
				for (int i = 0; i < 8; i++)
				{
					*dst-- = finalpal[pal[lo & 1 | hi & 2]];
					lo >>= 1;
					hi >>= 1;
				}
				dst += vinc;
				tile++;
			}
		}

		private unsafe void GenerateExAttr(int* dst, int pitch, byte[] palram, byte[] ppumem, byte[] exram)
		{
			byte[] chr = _ppu.GetExTiles();
			int chr_mask = chr.Length - 1;

			fixed (byte* chrptr = chr, palptr = palram, ppuptr = ppumem, exptr = exram)
			fixed (int* finalpal = _ppu.GetPalette())
			{
				DrawExNT(dst, pitch, palptr, ppuptr + 0x2000, exptr, chrptr, chr_mask, finalpal);
				DrawExNT(dst + 256, pitch, palptr, ppuptr + 0x2400, exptr, chrptr, chr_mask, finalpal);
				dst += pitch * 240;
				DrawExNT(dst, pitch, palptr, ppuptr + 0x2800, exptr, chrptr, chr_mask, finalpal);
				DrawExNT(dst + 256, pitch, palptr, ppuptr + 0x2c00, exptr, chrptr, chr_mask, finalpal);
			}
		}

		private unsafe void GenerateAttr(int* dst, int pitch, byte[] palram, byte[] ppumem)
		{
			fixed (byte* palptr = palram, ppuptr = ppumem)
			fixed (int* finalpal = _ppu.GetPalette())
			{
				byte* chrptr = ppuptr + (_ppu.BGBaseHigh ? 0x1000 : 0);
				DrawNT(dst, pitch, palptr, ppuptr + 0x2000, chrptr, finalpal);
				DrawNT(dst + 256, pitch, palptr, ppuptr + 0x2400, chrptr, finalpal);
				dst += pitch * 240;
				DrawNT(dst, pitch, palptr, ppuptr + 0x2800, chrptr, finalpal);
				DrawNT(dst + 256, pitch, palptr, ppuptr + 0x2c00, chrptr, finalpal);
			}
		}

		private unsafe void DrawNT(int* dst, int pitch, byte* palram, byte* nt, byte* chr, int* finalpal)
		{
			byte* at = nt + 0x3c0;

			for (int ty = 0; ty < 30; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					byte t = *nt++;
					byte a = at[ty >> 2 << 3 | tx >> 2];
					a >>= tx & 2;
					a >>= (ty & 2) << 1;
					int palnum = a & 3;

					int tileaddr = t << 4;
					DrawTile(dst, pitch, palram + palnum * 4, chr + tileaddr, finalpal);
					dst += 8;
				}
				dst -= 256;
				dst += pitch * 8;
			}
		}

		private unsafe void DrawExNT(int* dst, int pitch, byte* palram, byte* nt, byte* exnt, byte* chr, int chr_mask, int* finalpal)
		{
			for (int ty = 0; ty < 30; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					byte t = *nt++;
					byte ex = *exnt++;

					int tilenum = t | (ex & 0x3f) << 8;
					int palnum = ex >> 6;

					int tileaddr = tilenum << 4 & chr_mask;
					DrawTile(dst, pitch, palram + palnum * 4, chr + tileaddr, finalpal);
					dst += 8;
				}
				dst -= 256;
				dst += pitch * 8;
			}
		}

		private unsafe void Generate(bool now = false)
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (now == false && _emu.Frame % RefreshRate.Value != 0)
			{
				return;
			}

			var bmpdata = NameTableView.Nametables.LockBits(
				new Rectangle(0, 0, 512, 480),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			var dptr = (int*)bmpdata.Scan0.ToPointer();
			var pitch = bmpdata.Stride / 4;

			// Buffer all the data from the ppu, because it will be read multiple times and that is slow
			var ppuBuffer = _ppu.GetPPUBus();

			var palram = _ppu.GetPalRam();

			if (_ppu.ExActive)
			{
				byte[] exram = _ppu.GetExRam();
				GenerateExAttr(dptr, pitch, palram, ppuBuffer, exram);
			}
			else
			{
				GenerateAttr(dptr, pitch, palram, ppuBuffer);
			}

			NameTableView.Nametables.UnlockBits(bmpdata);
			NameTableView.Refresh();
		}

		#region Events

		#region Menu and Context Menu

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.Screenshot();
		}

		private void ScreenshotToClipboardMenuItem_Click(object sender, EventArgs e)
		{
			NameTableView.ScreenshotToClipboard();
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void RefreshImageContextMenuItem_Click(object sender, EventArgs e)
		{
			UpdateValues();
			NameTableView.Refresh();
		}

		#endregion

		#region Dialog and Controls

		private void NesNameTableViewer_KeyDown(object sender, KeyEventArgs e)
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

		private void NESNameTableViewer_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (_ppu != null)
			{
				_ppu.RemoveCallback1();
			}
		}

		private void ScanlineTextbox_TextChanged(object sender, EventArgs e)
		{
			if (int.TryParse(txtScanline.Text, out scanline))
			{
				_ppu.InstallCallback1(() => Generate(), scanline);
			}
		}

		private void NametableRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (rbNametableNW.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2000;
			}

			if (rbNametableNE.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2400;
			}

			if (rbNametableSW.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2800;
			}

			if (rbNametableSE.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_2C00;
			}

			if (rbNametableAll.Checked)
			{
				NameTableView.Which = NameTableViewer.WhichNametable.NT_ALL;
			}
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

			XYLabel.Text = TileX + " : " + TileY;
			int PPUAddress = 0x2000 + (NameTable * 0x400) + ((TileY % 30) * 32) + (TileX % 32);
			PPUAddressLabel.Text = string.Format("{0:X4}", PPUAddress);
			int TileID = _ppu.PeekPPU(PPUAddress);
			TileIDLabel.Text = string.Format("{0:X2}", TileID);
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
			int at = _ppu.PeekPPU(atbyte_ptr + 0x2000);
			if ((ty & 2) != 0) at >>= 4;
			if ((tx & 2) != 0) at >>= 2;
			at &= 0x03;
			PaletteLabel.Text = at.ToString();
		}

		private void NameTableView_MouseLeave(object sender, EventArgs e)
		{
			XYLabel.Text = string.Empty;
			PPUAddressLabel.Text = string.Empty;
			TileIDLabel.Text = string.Empty;
			TableLabel.Text = string.Empty;
			PaletteLabel.Text = string.Empty;
		}

		#endregion

		#endregion
	}
}
