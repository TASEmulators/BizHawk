//TODO - disable scanline controls if box is unchecked
//TODO - overhaul the BG display box if its mode7 or direct color (mode7 more important)
//TODO - draw `1024` label in red if your content is being scaled down.
//TODO - maybe draw a label (in lieu of above, also) showing what scale the content is at: 2x or 1x or 1/2x

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

			tabctrlDetails.SelectedIndex = 1;
			SyncViewerSize();
		}

		LibsnesCore currentSnesCore;
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (currentSnesCore != null)
				currentSnesCore.ScanlineHookManager.Unregister(this);
			currentSnesCore = null;
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

		public void UpdateToolsAfter()
		{
			SyncCore();
			if (!checkScanlineControl.Checked) UpdateValues();
		}

		public void UpdateToolsLoadstate()
		{
			SyncCore();
			UpdateValues();
		}

		private void nudScanline_ValueChanged(object sender, EventArgs e)
		{
			if (suppression) return;
			SyncCore();
			suppression = true;
			sliderScanline.Value = 224 - (int)nudScanline.Value;
			suppression = false;
		}

		private void sliderScanline_ValueChanged(object sender, EventArgs e)
		{
			if (suppression) return;
			checkScanlineControl.Checked = true;
			SyncCore();
			suppression = true;
			nudScanline.Value = 224 - sliderScanline.Value;
			suppression = false;
		}

		void SyncCore()
		{
			LibsnesCore core = Global.Emulator as LibsnesCore;
			if (currentSnesCore != core && currentSnesCore != null)
				currentSnesCore.ScanlineHookManager.Unregister(this);

			currentSnesCore = core;

			if (currentSnesCore != null)
			{
				if (checkScanlineControl.Checked)
					currentSnesCore.ScanlineHookManager.Register(this, ScanlineHook);
				else
					currentSnesCore.ScanlineHookManager.Unregister(this);
			}
		}

		void ScanlineHook(int line)
		{
			int target = (int)nudScanline.Value;
			if (target == line) UpdateValues();
		}

		void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (currentSnesCore == null) return;

			var gd = new SNESGraphicsDecoder();
			var si = gd.ScanScreenInfo();

			checkScreenExtbg.Checked = si.SETINI_Mode7ExtBG;
			checkScreenHires.Checked = si.SETINI_HiRes;
			checkScreenOverscan.Checked = si.SETINI_Overscan;
			checkScreenObjInterlace.Checked = si.SETINI_ObjInterlace;
			checkScreenInterlace.Checked = si.SETINI_ScreenInterlace;


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
			RenderPalette();
			UpdateColorDetails();
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
			if (selection == "2bpp tiles")
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, 64, 64, stride / 4, 2, 0);
			}
			if (selection == "4bpp tiles")
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, 64, 32, stride / 4, 4, 0);
			}
			if (selection == "8bpp tiles")
			{
				allocate(256, 256);
				gd.RenderTilesToScreen(pixelptr, 32, 32, stride / 4, 8, 0);
			}
			if (selection == "Mode7 tiles")
			{
				//256 tiles
				allocate(128, 128);
				gd.RenderMode7TilesToScreen(pixelptr, stride / 4, false);
			}
			if (selection == "Mode7Ext tiles")
			{
				//256 tiles
				allocate(128, 128);
				gd.RenderMode7TilesToScreen(pixelptr, stride / 4, true);
			}
			if (selection == "BG1" || selection == "BG2" || selection == "BG3" || selection == "BG4")
			{
				int bgnum = int.Parse(selection.Substring(2));
				var si = gd.ScanScreenInfo();
				var bg = si.BG[bgnum];

				bool handled = false;
				if (bg.Enabled)
				{
					if (si.Mode.MODE == 7)
					{
						bool mode7 = bgnum == 1;
						bool mode7extbg = (bgnum == 2 && si.SETINI_Mode7ExtBG);
						if(mode7 || mode7extbg)
						{
							handled = true;
							allocate(1024, 1024);
							gd.DecodeMode7BG(pixelptr, stride / 4, mode7extbg);
							int numPixels = 128 * 128 * 8 * 8;
							gd.Paletteize(pixelptr, 0, 0, numPixels);
							gd.Colorize(pixelptr, 0, numPixels);
						}
					}
					else
					{
						handled = true;
						var dims = bg.ScreenSizeInPixels;
						dims.Height = dims.Width = Math.Max(dims.Width, dims.Height);
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

		void ClearDetails()
		{
			//grpDetails.Text = "Details";
		}

		private void paletteViewer_MouseEnter(object sender, EventArgs e)
		{
			tabctrlDetails.SelectedIndex = 0;
		}

		private void paletteViewer_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		const int paletteCellSize = 16;
		const int paletteCellSpacing = 3;

		int[] lastPalette;
		int lastColorNum = 0;

		void RenderPalette()
		{
			var gd = new SNESGraphicsDecoder();
			lastPalette = gd.GetPalette();

			int pixsize = paletteCellSize * 16 + paletteCellSpacing * 17;
			int cellTotalSize = (paletteCellSize + paletteCellSpacing);
			var bmp = new Bitmap(pixsize, pixsize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(bmp))
			{
				for (int y = 0; y < 16; y++)
				{
					for (int x = 0; x < 16; x++)
					{
						int rgb555 = lastPalette[y * 16 + x];
						int color = gd.Colorize(rgb555);
						using (var brush = new SolidBrush(Color.FromArgb(color)))
						{
							g.FillRectangle(brush, new Rectangle(3 + x * cellTotalSize, 3 + y * cellTotalSize, paletteCellSize, paletteCellSize));
						}
					}
				}
			}

			paletteViewer.SetBitmap(bmp);
		}

		void UpdateColorDetails()
		{
			int rgb555 = lastPalette[lastColorNum];
			var gd = new SNESGraphicsDecoder();
			int color = gd.Colorize(rgb555);
			pnDetailsPaletteColor.BackColor = Color.FromArgb(color);

			txtDetailsPaletteColor.Text = string.Format("${0:X4}", rgb555);
			txtDetailsPaletteColorHex.Text = string.Format("#{0:X6}", color & 0xFFFFFF);
			txtDetailsPaletteColorRGB.Text = string.Format("({0},{1},{2})", (color >> 16) & 0xFF, (color >> 8) & 0xFF, (color & 0xFF));

			if (lastColorNum < 128) lblDetailsOBJOrBG.Text = "(BG Palette:)"; else lblDetailsOBJOrBG.Text = "(OBJ Palette:)";
			txtPaletteDetailsIndexHex.Text = string.Format("${0:X2}", lastColorNum);
			txtPaletteDetailsIndexHexSpecific.Text = string.Format("${0:X2}", lastColorNum & 0x7F);
			txtPaletteDetailsIndex.Text = string.Format("{0}", lastColorNum);
			txtPaletteDetailsIndexSpecific.Text = string.Format("{0}", lastColorNum & 0x7F);

			txtPaletteDetailsAddress.Text = string.Format("${0:X3}", lastColorNum * 2);
		}

		private void paletteViewer_MouseMove(object sender, MouseEventArgs e)
		{
			var pt = e.Location;
			pt.X -= paletteCellSpacing;
			pt.Y -= paletteCellSpacing;
			int tx = pt.X / (paletteCellSize + paletteCellSpacing);
			int ty = pt.Y / (paletteCellSize + paletteCellSpacing);
			if (tx >= 16 || ty >= 16) return;
			lastColorNum = ty * 16 + tx;
			UpdateColorDetails();
		}

		private void pnDetailsPaletteColor_DoubleClick(object sender, EventArgs e)
		{
			//not workign real well...
			//var cd = new ColorDialog();
			//cd.Color = pnDetailsPaletteColor.BackColor;
			//cd.ShowDialog(this);
		}

		private void rbQuad_CheckedChanged(object sender, EventArgs e)
		{
			SyncViewerSize();
		}

		void SyncViewerSize()
		{
			if (check2x.Checked)

				viewer.Size = new Size(1024, 1024);
			else
				viewer.Size = new Size(512, 512);
		}

		private void checkScanlineControl_CheckedChanged(object sender, EventArgs e)
		{
			SyncCore();
		}

		private void check2x_CheckedChanged(object sender, EventArgs e)
		{
			SyncViewerSize();
		}


	}
}
