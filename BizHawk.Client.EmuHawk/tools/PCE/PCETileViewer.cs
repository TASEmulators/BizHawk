using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.PCEngine;
using System.Drawing.Imaging;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCETileViewer : Form, IToolFormAutoConfig
	{
		[RequiredService]
		public PCEngine emu { get; private set; }

		private VDC vdc;
		private VCE vce;

		private int bgpalnum;
		private int sppalnum;

		public PCETileViewer()
		{
			InitializeComponent();
			bmpViewBG.ChangeBitmapSize(512, 256);
			bmpViewSP.ChangeBitmapSize(512, 256);
			bmpViewBGPal.ChangeBitmapSize(256, 256);
			bmpViewSPPal.ChangeBitmapSize(256, 256);
		}

		#region IToolForm

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			DrawBacks();
			DrawSprites();
			DrawPalettes();
			bmpViewBG.Refresh();
			bmpViewBGPal.Refresh();
			bmpViewSP.Refresh();
			bmpViewSPPal.Refresh();
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		unsafe static void Draw16x16(byte* src, int* dest, int pitch, int* pal)
		{
			int inc = pitch - 16;
			dest -= inc;
			for (int i = 0; i < 256; i++)
			{
				if ((i & 15) == 0)
					dest += inc; 
				*dest++ = pal[*src++];
			}
		}

		unsafe static void Draw8x8(byte* src, int* dest, int pitch, int* pal)
		{
			int inc = pitch - 8;
			dest -= inc;
			for (int i = 0; i < 64; i++)
			{
				if ((i & 7) == 0)
					dest += inc;
				*dest++ = pal[*src++];
			}
		}

		unsafe void DrawSprites()
		{
			var lockdata = bmpViewSP.bmp.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			fixed (byte* src = vdc.SpriteBuffer)
			fixed (int* pal = &vce.Palette[256 + sppalnum * 16])
			{
				for (int tile = 0; tile < 512; tile++)
				{
					int srcaddr = tile * 256;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destaddr = ty * 16 * pitch + tx * 16;
					Draw16x16(src + srcaddr, dest + destaddr, pitch, pal);
				}
			}
			bmpViewSP.bmp.UnlockBits(lockdata);
		}

		unsafe void DrawBacks()
		{
			var lockdata = bmpViewBG.bmp.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			fixed (byte* src = vdc.PatternBuffer)
			fixed (int* pal = &vce.Palette[0 + bgpalnum * 16])
			{
				for (int tile = 0; tile < 2048; tile++)
				{
					int srcaddr = tile * 64;
					int tx = tile & 63;
					int ty = tile >> 6;
					int destaddr = ty * 8 * pitch + tx * 8;
					Draw8x8(src + srcaddr, dest + destaddr, pitch, pal);
				}
			}
			bmpViewBG.bmp.UnlockBits(lockdata);
		}

		unsafe void DrawPalettes()
		{
			fixed (int* pal = vce.Palette)
			{
				DrawPalette(bmpViewBGPal.bmp, pal);
				DrawPalette(bmpViewSPPal.bmp, pal + 256);
			}
		}

		unsafe static void DrawPalette(Bitmap bmp, int* pal)
		{
			var lockdata = bmp.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			int inc = pitch - 256;
			for (int j = 0; j < 256; j++)
			{
				for (int i = 0; i < 256; i++)
				{
					int pindex = j & 0xf0 | i >> 4;
					*dest++ = pal[pindex];
				}
				dest += inc;
			}
			bmp.UnlockBits(lockdata);
		}

		public void Restart()
		{
			vce = emu.VCE;

			if (emu.SystemId == "SGX")
			{
				checkBoxVDC2.Enabled = true;
			}
			else
			{
				checkBoxVDC2.Enabled = false;
				checkBoxVDC2.Checked = false;
			}
			checkBoxVDC2_CheckedChanged(null, null);
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return true; }
		}

		#endregion

		private void checkBoxVDC2_CheckedChanged(object sender, EventArgs e)
		{
			vdc = checkBoxVDC2.Checked ? emu.VDC2 : emu.VDC1;
			UpdateValues();
		}

		private void bmpViewBGPal_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 15);
			bgpalnum = p;
			DrawBacks();
			bmpViewBG.Refresh();
		}

		private void bmpViewSPPal_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 15);
			sppalnum = p;
			DrawSprites();
			bmpViewSP.Refresh();
		}

		private void PCETileViewer_KeyDown(object sender, KeyEventArgs e)
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

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveBackgroundScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewBG.SaveFile();
		}

		private void saveSpriteScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewSP.SaveFile();
		}

		private void PCETileViewer_Load(object sender, EventArgs e)
		{
			vce = emu.VCE;
		}
	}
}
