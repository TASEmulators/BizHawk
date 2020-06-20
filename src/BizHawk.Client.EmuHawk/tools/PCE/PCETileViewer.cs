using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.PCEngine;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("Tile Viewer")]
	public partial class PceTileViewer : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		public IPceGpuView Viewer { get; private set; }

		private int _bgPalNum;
		private int _spPalNum;

		public PceTileViewer()
		{
			InitializeComponent();
			bmpViewBG.ChangeBitmapSize(512, 256);
			bmpViewSP.ChangeBitmapSize(512, 256);
			bmpViewBGPal.ChangeBitmapSize(256, 256);
			bmpViewSPPal.ChangeBitmapSize(256, 256);
		}

		protected override void GeneralUpdate() => UpdateBefore();

		protected override void UpdateBefore()
		{
			DrawBacks();
			DrawSprites();
			DrawPalettes();
			bmpViewBG.Refresh();
			bmpViewBGPal.Refresh();
			bmpViewSP.Refresh();
			bmpViewSPPal.Refresh();
		}

		static unsafe void Draw16x16(byte* src, int* dest, int pitch, int* pal)
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

		static unsafe void Draw8x8(byte* src, int* dest, int pitch, int* pal)
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
			Viewer.GetGpuData(checkBoxVDC2.Checked ? 1 : 0, view =>
			{
				var lockData = bmpViewSP.Bmp.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				int* dest = (int*)lockData.Scan0;
				int pitch = lockData.Stride / sizeof(int);
				byte* src = (byte*)view.SpriteCache;
				int* pal = (int*)view.PaletteCache + 256 + _spPalNum * 16;
				for (int tile = 0; tile < 512; tile++)
				{
					int srcAddr = tile * 256;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 16 * pitch + tx * 16;
					Draw16x16(src + srcAddr, dest + destAddr, pitch, pal);
				}
				bmpViewSP.Bmp.UnlockBits(lockData);
			});
		}

		unsafe void DrawBacks()
		{
			Viewer.GetGpuData(checkBoxVDC2.Checked ? 1 : 0, view =>
			{
				var lockData = bmpViewBG.Bmp.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				int* dest = (int*)lockData.Scan0;
				int pitch = lockData.Stride / sizeof(int);
				byte* src = (byte*)view.BackgroundCache;
				int* pal = (int*)view.PaletteCache + _bgPalNum * 16;
				for (int tile = 0; tile < 2048; tile++)
				{
					int srcAddr = tile * 64;
					int tx = tile & 63;
					int ty = tile >> 6;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8(src + srcAddr, dest + destAddr, pitch, pal);
				}
				bmpViewBG.Bmp.UnlockBits(lockData);
			});
		}

		unsafe void DrawPalettes()
		{
			Viewer.GetGpuData(checkBoxVDC2.Checked ? 1 : 0, view =>
			{
				int* pal = (int*)view.PaletteCache;
				DrawPalette(bmpViewBGPal.Bmp, pal);
				DrawPalette(bmpViewSPPal.Bmp, pal + 256);
			});
		}

		static unsafe void DrawPalette(Bitmap bmp, int* pal)
		{
			var lockData = bmp.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			int inc = pitch - 256;
			for (int j = 0; j < 256; j++)
			{
				for (int i = 0; i < 256; i++)
				{
					int pIndex = j & 0xf0 | i >> 4;
					*dest++ = pal[pIndex];
				}

				dest += inc;
			}

			bmp.UnlockBits(lockData);
		}

		public void Restart()
		{
			if (Viewer.IsSgx)
			{
				checkBoxVDC2.Enabled = true;
			}
			else
			{
				checkBoxVDC2.Enabled = false;
				checkBoxVDC2.Checked = false;
			}

			CheckBoxVDC2_CheckedChanged(null, null);
		}

		private void CheckBoxVDC2_CheckedChanged(object sender, EventArgs e)
		{
			GeneralUpdate();
		}

		private void BmpViewBGPal_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 15);
			_bgPalNum = p;
			DrawBacks();
			bmpViewBG.Refresh();
		}

		private void BmpViewSPPal_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 15);
			_spPalNum = p;
			DrawSprites();
			bmpViewSP.Refresh();
		}

		private void PceTileViewer_KeyDown(object sender, KeyEventArgs e)
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

				if (found is BmpView bv)
				{
					Clipboard.SetImage(bv.Bmp);
				}
			}
		}

		private void CloseMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SaveBackgroundScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewBG.SaveFile();
		}

		private void SaveSpriteScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewSP.SaveFile();
		}

		private void PceTileViewer_Load(object sender, EventArgs e)
		{
		}
	}
}
