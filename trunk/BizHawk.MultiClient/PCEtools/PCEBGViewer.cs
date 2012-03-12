using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using BizHawk.Emulation.Consoles.TurboGrafx;

namespace BizHawk.MultiClient
{
	public partial class PCEBGViewer : Form
	{
		PCEngine pce;

		public PCEBGViewer()
		{
			InitializeComponent();
			Activated += (o, e) => Generate();
		}

		private unsafe void Generate()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (pce == null) return;

			int width = 8 * pce.VDC1.BatWidth;
			int height = 8 * pce.VDC1.BatHeight;
			BitmapData buf = canvas.bat.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, canvas.bat.PixelFormat);
			int pitch = buf.Stride / 4;
			int* begin = (int*)buf.Scan0.ToPointer();
			int* p = begin;

			// TODO: this does not clear background, why?
			for (int i = 0; i < pitch * buf.Height; ++i, ++p)
				*p = canvas.BackColor.ToArgb();

			p = begin;
			for (int y = 0; y < height; ++y)
			{
				int yTile = y / 8;
				int yOfs = y % 8;
				for (int x = 0; x < width; ++x, ++p)
				{
					int xTile = x / 8;
					int xOfs = x % 8;
					int tileNo = pce.VDC1.VRAM[(ushort)(((yTile * pce.VDC1.BatWidth) + xTile))] & 0x07FF;
					int paletteNo = pce.VDC1.VRAM[(ushort)(((yTile * pce.VDC1.BatWidth) + xTile))] >> 12;
					int paletteBase = paletteNo * 16;

					byte c = pce.VDC1.PatternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
					if (c == 0)
						*p = pce.VCE.Palette[0];
					else
					{
						*p = pce.VCE.Palette[paletteBase + c];
					}
				}
				p += pitch - width;
			}

			canvas.bat.UnlockBits(buf);
			canvas.Refresh();
		}

		public void Restart()
		{
			if (!(Global.Emulator is PCEngine))
			{
				this.Close();
				return;
			}
			else
			    pce = Global.Emulator as PCEngine;
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is PCEngine)) return;

			
		}

		private void PCEBGViewer_Load(object sender, EventArgs e)
		{
			pce = Global.Emulator as PCEngine;
		}

		private void PCEBGViewer_FormClosed(object sender, FormClosedEventArgs e)
		{

		}
	}
}
