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
			txtScreenBG1Bpp.Text = FormatBpp(si.BG.BG1.Bpp);
			txtScreenBG2Bpp.Text = FormatBpp(si.BG.BG2.Bpp);
			txtScreenBG3Bpp.Text = FormatBpp(si.BG.BG3.Bpp);
			txtScreenBG4Bpp.Text = FormatBpp(si.BG.BG4.Bpp);
			txtScreenBG1TSize.Text = FormatBpp(si.BG.BG1.TileSize);
			txtScreenBG2TSize.Text = FormatBpp(si.BG.BG2.TileSize);
			txtScreenBG3TSize.Text = FormatBpp(si.BG.BG3.TileSize);
			txtScreenBG4TSize.Text = FormatBpp(si.BG.BG4.TileSize);

			txtBG1TSizeBits.Text = si.BG.BG1.TILESIZE.ToString();
			txtBG1TSizeDescr.Text = string.Format("{0}x{0}", si.BG.BG1.TileSize);
			txtBG1Bpp.Text = FormatBpp(si.BG.BG1.Bpp);
			txtBG1SizeBits.Text = si.BG.BG1.SCSIZE.ToString();
			txtBG1SizeInTiles.Text = FormatScreenSizeInTiles(si.BG.BG1.ScreenSize);
			txtBG1SCAddrBits.Text = si.BG.BG1.SCADDR.ToString();
			txtBG1SCAddrDescr.Text = FormatVramAddress(si.BG.BG1.SCADDR << 9);
			txtBG1Colors.Text = (1 << si.BG.BG1.Bpp).ToString();
			txtBG1TDAddrBits.Text = si.BG.BG1.TDADDR.ToString();
			txtBG1TDAddrDescr.Text = FormatVramAddress(si.BG.BG1.TDADDR << 13);
			
			var sizeInPixels = SNESGraphicsDecoder.SizeInTilesForBGSize(si.BG.BG1.ScreenSize);
			sizeInPixels.Width *= si.BG.BG1.TileSize;
			sizeInPixels.Height *= si.BG.BG1.TileSize;
			txtBG1SizeInPixels.Text = string.Format("{0}x{1}", sizeInPixels.Width, sizeInPixels.Height);

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
			gd.CacheTiles();
			string selection = comboDisplayType.SelectedItem as string;
			if (selection == "Tiles as 2bpp")
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, stride / 4, 2, 0);
			}
			if (selection == "Tiles as 4bpp")
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, stride / 4, 4, 0);
			}
			if (selection == "Tiles as 8bpp")
			{
				allocate(256, 256);
				gd.RenderTilesToScreen(pixelptr, stride / 4, 8, 0);
			}
			if (selection == "BG1" || selection == "BG2" || selection == "BG3" || selection == "BG4")
			{
				int bgnum = int.Parse(selection.Substring(2));
				var si = gd.ScanScreenInfo();
				var bg = si.BG[bgnum];
				if (bg.Enabled)
				{
					var dims = bg.ScreenSizeInPixels;
					allocate(dims.Width, dims.Height);
					int numPixels = dims.Width * dims.Height;
					System.Diagnostics.Debug.Assert(stride / 4 == dims.Width);

					var map = gd.FetchTilemap(bg.ScreenAddr, bg.ScreenSize);
					int paletteStart = 0;
					gd.DecodeBG(pixelptr, stride / 4, map, bg.TiledataAddr, bg.ScreenSize, bg.Bpp, bg.TileSize, paletteStart);
					gd.Paletteize(pixelptr, 0, 0, numPixels);
					gd.Colorize(pixelptr, 0, numPixels);
				}
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
