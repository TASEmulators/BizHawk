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
		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		SwappableDisplaySurfaceSet surfaceSet = new SwappableDisplaySurfaceSet();

		public SNESGraphicsDebugger()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			comboDisplayType.SelectedIndex = 0;
			comboBGProps.SelectedIndex = 0;
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

			int bgnum = comboBGProps.SelectedIndex + 1;

			txtBG1TSizeBits.Text = si.BG[bgnum].TILESIZE.ToString();
			txtBG1TSizeDescr.Text = string.Format("{0}x{0}", si.BG[bgnum].TileSize);
			txtBG1Bpp.Text = FormatBpp(si.BG[bgnum].Bpp);
			txtBG1SizeBits.Text = si.BG[bgnum].SCSIZE.ToString();
			txtBG1SizeInTiles.Text = FormatScreenSizeInTiles(si.BG[bgnum].ScreenSize);
			txtBG1SCAddrBits.Text = si.BG[bgnum].SCADDR.ToString();
			txtBG1SCAddrDescr.Text = FormatVramAddress(si.BG[bgnum].SCADDR << 9);
			txtBG1Colors.Text = (1 << si.BG[bgnum].Bpp).ToString();
			txtBG1TDAddrBits.Text = si.BG[bgnum].TDADDR.ToString();
			txtBG1TDAddrDescr.Text = FormatVramAddress(si.BG[bgnum].TDADDR << 13);

			var sizeInPixels = SNESGraphicsDecoder.SizeInTilesForBGSize(si.BG[bgnum].ScreenSize);
			sizeInPixels.Width *= si.BG[bgnum].TileSize;
			sizeInPixels.Height *= si.BG[bgnum].TileSize;
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

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadSNESGraphicsDebugger;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SNESGraphicsDebuggerSaveWindowPosition;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadSNESGraphicsDebugger ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SNESGraphicsDebuggerSaveWindowPosition ^= true;
		}

		private void SNESGraphicsDebugger_Load(object sender, EventArgs e)
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;

			if (Global.Config.SNESGraphicsDebuggerSaveWindowPosition && Global.Config.SNESGraphicsDebuggerWndx >= 0 && Global.Config.SNESGraphicsDebuggerWndy >= 0)
			{
				this.Location = new Point(Global.Config.SNESGraphicsDebuggerWndx, Global.Config.SNESGraphicsDebuggerWndy);
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.SNESGraphicsDebuggerWndx = this.Location.X;
			Global.Config.SNESGraphicsDebuggerWndy = this.Location.Y;
		}

		bool suppression = false;
		private void rbBGX_CheckedChanged(object sender, EventArgs e)
		{
			if (suppression) return;
			//sync the comboBGProps dropdown with the result of this check
			suppression = true;
			if (rbBG1.Checked) comboBGProps.SelectedIndex = 0;
			if (rbBG2.Checked) comboBGProps.SelectedIndex = 1;
			if (rbBG3.Checked) comboBGProps.SelectedIndex = 2;
			if (rbBG4.Checked) comboBGProps.SelectedIndex = 3;
			suppression = false;
			UpdateValues();
		}

		private void comboBGProps_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (suppression) return;

			//sync the radiobuttons with this selection
			suppression = true;
			if (comboBGProps.SelectedIndex == 0) rbBG1.Checked = true;
			if (comboBGProps.SelectedIndex == 1) rbBG2.Checked = true;
			if (comboBGProps.SelectedIndex == 2) rbBG3.Checked = true;
			if (comboBGProps.SelectedIndex == 3) rbBG4.Checked = true;
			suppression = false;
			UpdateValues();
		}
	}
}
