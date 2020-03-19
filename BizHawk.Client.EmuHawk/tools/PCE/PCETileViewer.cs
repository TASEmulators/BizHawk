using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.PCEngine;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PceTileViewer : Form, IToolFormAutoConfig
	{
		[RequiredService]
		public PCEngine Emu { get; private set; }

		private VDC _vdc;
		private VCE _vce;

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
			var lockData = bmpViewSP.BMP.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			fixed (byte* src = _vdc.SpriteBuffer)
			fixed (int* pal = &_vce.Palette[256 + _spPalNum * 16])
			{
				for (int tile = 0; tile < 512; tile++)
				{
					int srcAddr = tile * 256;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 16 * pitch + tx * 16;
					Draw16x16(src + srcAddr, dest + destAddr, pitch, pal);
				}
			}

			bmpViewSP.BMP.UnlockBits(lockData);
		}

		unsafe void DrawBacks()
		{
			var lockData = bmpViewBG.BMP.LockBits(new Rectangle(0, 0, 512, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			fixed (byte* src = _vdc.PatternBuffer)
			fixed (int* pal = &_vce.Palette[0 + _bgPalNum * 16])
			{
				for (int tile = 0; tile < 2048; tile++)
				{
					int srcAddr = tile * 64;
					int tx = tile & 63;
					int ty = tile >> 6;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8(src + srcAddr, dest + destAddr, pitch, pal);
				}
			}
			bmpViewBG.BMP.UnlockBits(lockData);
		}

		unsafe void DrawPalettes()
		{
			fixed (int* pal = _vce.Palette)
			{
				DrawPalette(bmpViewBGPal.BMP, pal);
				DrawPalette(bmpViewSPPal.BMP, pal + 256);
			}
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
			_vce = Emu.VCE;

			if (Emu.SystemId == "SGX")
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

		public bool AskSaveChanges() => true;

		public bool UpdateBefore => true;

		#endregion

		private void CheckBoxVDC2_CheckedChanged(object sender, EventArgs e)
		{
			_vdc = checkBoxVDC2.Checked ? Emu.VDC2 : Emu.VDC1;
			UpdateValues();
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
					Clipboard.SetImage(bv.BMP);
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
			_vce = Emu.VCE;
		}
	}
}
