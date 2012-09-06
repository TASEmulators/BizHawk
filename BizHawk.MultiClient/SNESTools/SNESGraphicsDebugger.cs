using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo.SNES;

namespace BizHawk.MultiClient
{
	public unsafe partial class SNESGraphicsDebugger : Form
	{
		SwappableDisplaySurfaceSet surfaceSet = new SwappableDisplaySurfaceSet();

		public SNESGraphicsDebugger()
		{
			InitializeComponent();
			comboDisplayType.SelectedIndex = 0;
		}

		string FormatBpp(int bpp)
		{
			if (bpp == 0) return "---";
			else return bpp.ToString();
		}

		string FormatScreenSizeInTiles(SNESGraphicsDecoder.ScreenSize screensize)
		{
			var dims = SNESGraphicsDecoder.SizeInTilesForBGSize(screensize);
			int size = dims.Width * dims.Height * 2 / 1024;
			return string.Format("{0} ({1}K)", dims, size);
		}

		string FormatVramAddress(int address)
		{
			int excess = address & 1023;
			if (excess != 0) return "@" + address.ToHexString(4);
			else return string.Format("@{0} ({1}K)", address.ToHexString(4), address / 1024);
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			var snes = Global.Emulator as LibsnesCore;
			if (snes == null) return;

			var gd = new SNESGraphicsDecoder();
			var si = gd.ScanScreenInfo();

			txtModeBits.Text = si.Mode.MODE.ToString();
			txtBG1Bpp.Text = txtScreenBG1Bpp.Text = FormatBpp(si.BG.BG1.Bpp);
			txtScreenBG2Bpp.Text = FormatBpp(si.BG.BG2.Bpp);
			txtScreenBG3Bpp.Text = FormatBpp(si.BG.BG3.Bpp);
			txtScreenBG4Bpp.Text = FormatBpp(si.BG.BG4.Bpp);

			txtBG1SizeBits.Text = si.BG.BG1.SCSIZE.ToString();
			txtBG1SizeInTiles.Text = FormatScreenSizeInTiles(si.BG.BG1.ScreenSize);
			txtBG1SCAddrBits.Text = si.BG.BG1.SCADDR.ToString();
			txtBG1SCAddrDescr.Text = FormatVramAddress(si.BG.BG1.SCADDR << 9);
			txtBG1Colors.Text = (1 << si.BG.BG1.Bpp).ToString();
			txtBG1TDAddrBits.Text = si.BG.BG1.TDADDR.ToString();
			txtBG1TDAddrDescr.Text = FormatVramAddress(si.BG.BG1.TDADDR << 13);

			RenderView();
		}

		//todo - something smarter to cycle through bitmaps without repeatedly trashing them (use the dispose callback on the viewer)
		void RenderView()
		{
			Bitmap bmp = null;
			System.Drawing.Imaging.BitmapData bmpdata = null;
			int* pixelptr = null;
			int stride = 0;

			Action<int,int> allocate = (w, h) =>
			{
				bmp = new Bitmap(w, h);
				bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				pixelptr = (int*)bmpdata.Scan0.ToPointer();
				stride = bmpdata.Stride;
			};

			var gd = new SNESGraphicsDecoder();
			string selection = comboDisplayType.SelectedItem as string;
			if (selection == "Tiles as 2bpp")
			{
				allocate(512, 512);
				gd.DecodeTiles2bpp(pixelptr, stride / 4, 0);
			}
			if (selection == "Tiles as 4bpp")
			{
				allocate(512, 512);
				gd.DecodeTiles4bpp(pixelptr, stride / 4, 0);
			}
			if (selection == "Tiles as 8bpp")
			{
				allocate(256, 256);
				gd.DecodeTiles8bpp(pixelptr, stride / 4, 0);
			}

			if (bmp != null)
			{
				bmp.UnlockBits(bmpdata);
				viewer.SetBitmap(bmp);
			}
		}


		private void comboDisplayType_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateValues();
		}


	}
}
