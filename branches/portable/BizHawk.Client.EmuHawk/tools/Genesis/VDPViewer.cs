using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using System.Drawing.Imaging;

namespace BizHawk.Client.EmuHawk.tools.Genesis
{
	public partial class VDPViewer : Form, IToolForm
	{
		private LibGPGX.VDPView View = new LibGPGX.VDPView();
		private GPGX Emu;
		int palindex = 0;

		public VDPViewer()
		{
			InitializeComponent();
			bmpViewTiles.ChangeBitmapSize(512, 256);
			bmpViewPal.ChangeBitmapSize(16, 4);
			Restart();
		}

		unsafe static void DrawTile(int* dest, int pitch, byte* src, int* pal)
		{
			for (int j = 0; j < 8; j++)
			{
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				*dest++ = pal[*src++];
				dest += pitch - 8;
			}
		}

		unsafe static void DrawNameTable(LibGPGX.VDPNameTable NT, ushort* vram, byte* tiles, int* pal, BmpView bv)
		{
			ushort* nametable = vram + NT.Baseaddr / 2;
			int tilew = NT.Width;
			int tileh = NT.Height;

			Size pixsize = new Size(tilew * 8, tileh * 8);
			bv.Size = pixsize;
			bv.ChangeBitmapSize(pixsize);

			var lockdata = bv.bmp.LockBits(new Rectangle(Point.Empty, pixsize), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int pitch = lockdata.Stride / sizeof(int);
			int* dest = (int*)lockdata.Scan0;

			for (int tiley = 0; tiley < tileh; tiley++)
			{
				for (int tilex = 0; tilex < tilew; tilex++)
				{
					ushort bgent = *nametable++;
					int palidx = bgent >> 9 & 0x30;
					int tileent = bgent & 0x1fff; // h and v flip are stored separately in cache
					DrawTile(dest, pitch, tiles + tileent * 64, pal + palidx);
					dest += 8;
				}
				dest -= 8 * tilew;
				dest += 8 * pitch;
			}
			bv.bmp.UnlockBits(lockdata);
			bv.Refresh();
		}

		unsafe void DrawPalettes(int *pal)
		{
			var lockdata = bmpViewPal.bmp.LockBits(new Rectangle(0, 0, 16, 4), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int pitch = lockdata.Stride / sizeof(int);
			int* dest = (int*)lockdata.Scan0;

			for (int j = 0; j < 4; j++)
			{
				for (int i = 0; i < 16; i++)
					*dest++ = *pal++;
				dest += pitch - 16;
			}
			bmpViewPal.bmp.UnlockBits(lockdata);
			bmpViewPal.Refresh();
		}

		unsafe void DrawTiles()
		{
			var lockdata = bmpViewTiles.bmp.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int pitch = lockdata.Stride / sizeof(int);
			int* dest = (int*)lockdata.Scan0;
			byte* src = (byte*)View.PatternCache;

			int* pal = 0x10 * palindex + (int*)View.ColorCache;

			for (int tile = 0; tile < 2048;)
			{
				DrawTile(dest, pitch, src, pal);
				dest += 8;
				src += 64;
				tile++;
				if ((tile & 63) == 0)
					dest += 8 * pitch - 512;
			}
			bmpViewTiles.bmp.UnlockBits(lockdata);
			bmpViewTiles.Refresh();
		}


		public void UpdateValues()
		{
			if (Emu == null)
				return;
			Emu.UpdateVDPViewContext(View);
			unsafe
			{
				int* pal = (int*)View.ColorCache;
				//for (int i = 0; i < 0x40; i++)
				//	pal[i] |= unchecked((int)0xff000000);
				DrawPalettes(pal);
				DrawTiles();
				ushort *VRAMNT = (ushort*)View.VRAM;
				byte *tiles = (byte*)View.PatternCache;
				DrawNameTable(View.NTA, VRAMNT, tiles, pal, bmpViewNTA);
				DrawNameTable(View.NTB, VRAMNT, tiles, pal, bmpViewNTB);
				DrawNameTable(View.NTW, VRAMNT, tiles, pal, bmpViewNTW);
			}
		}

		public void Restart()
		{
			Emu = Global.Emulator as GPGX;
			if (Emu == null)
				Close();
			UpdateValues();
		}

		public bool AskSave()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return true; }
		}

		private void bmpViewPal_MouseClick(object sender, MouseEventArgs e)
		{
			int idx = e.Y / 16;
			idx = Math.Min(3, Math.Max(idx, 0));
			palindex = idx;
			DrawTiles();
		}

		private void VDPViewer_KeyDown(object sender, KeyEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
			{
				// find the control under the mouse
				Point m = Cursor.Position;
				Control top = this;
				Control found;
				do
				{
					found = top.GetChildAtPoint(top.PointToClient(m));
					top = found;
				} while (found != null && found.HasChildren);

				if (found is BmpView)
				{
					var bv = found as BmpView;
					Clipboard.SetImage(bv.bmp);
				}
			}
		}
	}
}
