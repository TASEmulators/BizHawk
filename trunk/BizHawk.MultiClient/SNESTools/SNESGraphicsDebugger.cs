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
			viewerTile.ScaleImage = true;
			viewer.ScaleImage = false;

			var displayTypeItems = new List<DisplayTypeItem>();
			displayTypeItems.Add(new DisplayTypeItem("BG1", eDisplayType.BG1));
			displayTypeItems.Add(new DisplayTypeItem("BG2",eDisplayType.BG2));
			displayTypeItems.Add(new DisplayTypeItem("BG3",eDisplayType.BG3));
			displayTypeItems.Add(new DisplayTypeItem("BG4",eDisplayType.BG4));
			displayTypeItems.Add(new DisplayTypeItem("OBJ",eDisplayType.OBJ));
			displayTypeItems.Add(new DisplayTypeItem("2bpp tiles",eDisplayType.Tiles2bpp));
			displayTypeItems.Add(new DisplayTypeItem("4bpp tiles",eDisplayType.Tiles4bpp));
			displayTypeItems.Add(new DisplayTypeItem("8bpp tiles",eDisplayType.Tiles8bpp));
			displayTypeItems.Add(new DisplayTypeItem("Mode7 tiles",eDisplayType.TilesMode7));
			displayTypeItems.Add(new DisplayTypeItem("Mode7Ext tiles",eDisplayType.TilesMode7Ext));
			displayTypeItems.Add(new DisplayTypeItem("Mode7 tiles (DC)", eDisplayType.TilesMode7DC));

			comboDisplayType.DataSource = displayTypeItems;
			comboDisplayType.SelectedIndex = 0;
			comboBGProps.SelectedIndex = 0;

			tabctrlDetails.SelectedIndex = 1;
			SyncViewerSize();
			SyncColorSelection();
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
			if (this.Visible && !checkScanlineControl.Checked)
			{
				RegenerateData();
				UpdateValues();
			}
		}

		public void UpdateToolsLoadstate()
		{
			SyncCore();
			RegenerateData();
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
				if (this.Visible && checkScanlineControl.Checked)
					currentSnesCore.ScanlineHookManager.Register(this, ScanlineHook);
				else
					currentSnesCore.ScanlineHookManager.Unregister(this);
			}
		}

		void ScanlineHook(int line)
		{
			int target = (int)nudScanline.Value;
			if (target == line)
			{
				RegenerateData();
				UpdateValues();
			}
		}

		SNESGraphicsDecoder gd = new SNESGraphicsDecoder();
		SNESGraphicsDecoder.ScreenInfo si;

		void RegenerateData()
		{
			gd = null;
			if (currentSnesCore == null) return;
			gd = new SNESGraphicsDecoder();
			gd.CacheTiles();
			si = gd.ScanScreenInfo();
		}

		void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (currentSnesCore == null) return;

			checkScreenExtbg.Checked = si.SETINI_Mode7ExtBG;
			checkScreenHires.Checked = si.SETINI_HiRes;
			checkScreenOverscan.Checked = si.SETINI_Overscan;
			checkScreenObjInterlace.Checked = si.SETINI_ObjInterlace;
			checkScreenInterlace.Checked = si.SETINI_ScreenInterlace;

			txtScreenCGWSEL_ColorMask.Text = si.CGWSEL_ColorMask.ToString();
			txtScreenCGWSEL_ColorSubMask.Text = si.CGWSEL_ColorSubMask.ToString();
			txtScreenCGWSEL_MathFixed.Text = si.CGWSEL_AddSubMode.ToString();
			checkScreenCGWSEL_DirectColor.Checked = si.CGWSEL_DirectColor;

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
			if (si.BG[bgnum].Bpp == 8 && si.CGWSEL_DirectColor) txtBG1Colors.Text = "(Direct Color)";
			txtBG1TDAddrBits.Text = si.BG[bgnum].TDADDR.ToString();
			txtBG1TDAddrDescr.Text = FormatVramAddress(si.BG[bgnum].TDADDR << 13);

			var sizeInPixels = SNESGraphicsDecoder.SizeInTilesForBGSize(si.BG[bgnum].ScreenSize);
			sizeInPixels.Width *= si.BG[bgnum].TileSize;
			sizeInPixels.Height *= si.BG[bgnum].TileSize;
			txtBG1SizeInPixels.Text = string.Format("{0}x{1}", sizeInPixels.Width, sizeInPixels.Height);

			SyncColorSelection();
			RenderView();
			RenderPalette();
			RenderTileView();
			UpdateColorDetails();
		}

		eDisplayType CurrDisplaySelection { get { return (comboDisplayType.SelectedValue as eDisplayType?).Value; } }

		//todo - something smarter to cycle through bitmaps without repeatedly trashing them (use the dispose callback on the viewer)
		void RenderView()
		{
			Bitmap bmp = null;
			System.Drawing.Imaging.BitmapData bmpdata = null;
			int* pixelptr = null;
			int stride = 0;

			Action<int, int> allocate = (w, h) =>
			{
				bmp = new Bitmap(w, h);
				bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				pixelptr = (int*)bmpdata.Scan0.ToPointer();
				stride = bmpdata.Stride;
			};

			var selection = CurrDisplaySelection;
			if (selection == eDisplayType.Tiles2bpp)
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, 64, 64, stride / 4, 2, currPaletteSelection.start);
			}
			if (selection == eDisplayType.Tiles4bpp)
			{
				allocate(512, 512);
				gd.RenderTilesToScreen(pixelptr, 64, 32, stride / 4, 4, currPaletteSelection.start);
			}
			if (selection == eDisplayType.Tiles8bpp)
			{
				allocate(256, 256);
				gd.RenderTilesToScreen(pixelptr, 32, 32, stride / 4, 8, currPaletteSelection.start);
			}
			if (selection == eDisplayType.TilesMode7)
			{
				//256 tiles
				allocate(128, 128);
				gd.RenderMode7TilesToScreen(pixelptr, stride / 4, false, false);
			}
			if (selection == eDisplayType.TilesMode7Ext)
			{
				//256 tiles
				allocate(128, 128);
				gd.RenderMode7TilesToScreen(pixelptr, stride / 4, true, false);
			}
			if (selection == eDisplayType.TilesMode7DC)
			{
				//256 tiles
				allocate(128, 128);
				gd.RenderMode7TilesToScreen(pixelptr, stride / 4, false, true);
			}
			if (IsDisplayTypeBG(selection))
			{
				int bgnum = (int)selection;
				var si = gd.ScanScreenInfo();
				var bg = si.BG[bgnum];

				bool handled = false;
				if (bg.Enabled)
				{
					//TODO - directColor in normal BG renderer
					bool DirectColor = si.CGWSEL_DirectColor && bg.Bpp == 8; //any exceptions?
					int numPixels = 0;
					if (si.Mode.MODE == 7)
					{
						bool mode7 = bgnum == 1;
						bool mode7extbg = (bgnum == 2 && si.SETINI_Mode7ExtBG);
						if (mode7 || mode7extbg)
						{
							handled = true;
							allocate(1024, 1024);
							gd.DecodeMode7BG(pixelptr, stride / 4, mode7extbg);
							numPixels = 128 * 128 * 8 * 8;
							if (DirectColor) gd.DirectColorify(pixelptr, numPixels);
							else gd.Paletteize(pixelptr, 0, 0, numPixels);
						}
					}
					else
					{
						handled = true;
						var dims = bg.ScreenSizeInPixels;
						dims.Height = dims.Width = Math.Max(dims.Width, dims.Height);
						allocate(dims.Width, dims.Height);
						numPixels = dims.Width * dims.Height;
						System.Diagnostics.Debug.Assert(stride / 4 == dims.Width);

						var map = gd.FetchTilemap(bg.ScreenAddr, bg.ScreenSize);
						int paletteStart = 0;
						gd.DecodeBG(pixelptr, stride / 4, map, bg.TiledataAddr, bg.ScreenSize, bg.Bpp, bg.TileSize, paletteStart);
						gd.Paletteize(pixelptr, 0, 0, numPixels);
					}

					gd.Colorize(pixelptr, 0, numPixels);
				}
			}

			if (bmp != null)
			{
				bmp.UnlockBits(bmpdata);
				viewer.SetBitmap(bmp);
			}
		}

		enum eDisplayType
		{
			BG1=1, BG2=2, BG3=3, BG4=4, OBJ, Tiles2bpp, Tiles4bpp, Tiles8bpp, TilesMode7, TilesMode7Ext, TilesMode7DC
		}
		static bool IsDisplayTypeBG(eDisplayType type) { return type == eDisplayType.BG1 || type == eDisplayType.BG2 || type == eDisplayType.BG3 || type == eDisplayType.BG4; }
		static int DisplayTypeBGNum(eDisplayType type) { if(IsDisplayTypeBG(type)) return (int)type; else return -1; }

		class DisplayTypeItem
		{
			public eDisplayType type { get; set; }
			public string descr { get; set; }
			public DisplayTypeItem(string descr, eDisplayType type)
			{
				this.type = type;
				this.descr = descr;
			}
		}

		private void comboDisplayType_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateValues();

			//change the bg props viewer to match
			if (IsDisplayTypeBG(CurrDisplaySelection))
				comboBGProps.SelectedIndex = DisplayTypeBGNum(CurrDisplaySelection) - 1;
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


		const int paletteCellSize = 16;
		const int paletteCellSpacing = 3;

		int[] lastPalette;
		int lastColorNum = 0;
		int selectedColorNum = 0;
		SNESGraphicsDecoder.PaletteSelection currPaletteSelection;

		Rectangle GetPaletteRegion(int start, int num)
		{
			var ret = new Rectangle();
			ret.X = start % 16;
			ret.Y = start / 16;
			ret.Width = num;
			ret.Height = num / 16;
			if (ret.Height == 0) ret.Height = 1;
			if (ret.Width > 16) ret.Width = 16;
			return ret;
		}

		Rectangle GetPaletteRegion(SNESGraphicsDecoder.PaletteSelection sel)
		{
			int start = sel.start, num = sel.size;
			return GetPaletteRegion(start, num);
		}

		void DrawPaletteRegion(Graphics g, Color color, Rectangle region)
		{
			int cellTotalSize = (paletteCellSize + paletteCellSpacing);

			int x = paletteCellSpacing + region.X * cellTotalSize - 2;
			int y = paletteCellSpacing + region.Y * cellTotalSize - 2;
			int width = cellTotalSize * region.Width;
			int height = cellTotalSize * region.Height;

			var rect = new Rectangle(x, y, width, height);
			using (var pen = new Pen(color))
				g.DrawRectangle(pen, rect);
		}

		//if a tile set is being displayed, this will adapt the user's color selection into a palette to be used for rendering the tiles
		SNESGraphicsDecoder.PaletteSelection GetPaletteSelectionForTileDisplay(int colorSelection)
		{
			int bpp = 0;
			var selection = CurrDisplaySelection;
			if (selection == eDisplayType.Tiles2bpp) bpp=2;
			if (selection == eDisplayType.Tiles4bpp) bpp = 4;
			if (selection == eDisplayType.Tiles8bpp) bpp = 8;
			if (selection == eDisplayType.TilesMode7) bpp = 8;
			if (selection == eDisplayType.TilesMode7Ext) bpp = 7;

			SNESGraphicsDecoder.PaletteSelection ret = new SNESGraphicsDecoder.PaletteSelection();
			if(bpp == 0) return ret;

			//mode7 ext is fixed to use the top 128 colors
			if (bpp == 7)
			{
				ret.size = 128;
				ret.start = 0;
				return ret;
			}
			
			ret.size = 1 << bpp;
			ret.start = colorSelection & (~(ret.size - 1));
			return ret;
		}

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
							g.FillRectangle(brush, new Rectangle(paletteCellSpacing + x * cellTotalSize, paletteCellSpacing + y * cellTotalSize, paletteCellSize, paletteCellSize));
						}
					}
				}

				//draw selection boxes:
				//first, draw the current selection
				var region = GetPaletteRegion(selectedColorNum, 1);
				DrawPaletteRegion(g, Color.Red, region);
				//next, draw the rectangle that advises you which colors could possibly be used for a bg
				if (IsDisplayTypeBG(CurrDisplaySelection))
				{
					var si = gd.ScanScreenInfo();
					var ps = si.BG[DisplayTypeBGNum(CurrDisplaySelection)].PaletteSelection;
					region = GetPaletteRegion(ps);
					DrawPaletteRegion(g, Color.FromArgb(192, 128, 255, 255), region);
				}
				//finally, draw the palette the user has chosen, in case he's viewing tiles
				if (currPaletteSelection.size != 0)
				{
					region = GetPaletteRegion(currPaletteSelection.start, currPaletteSelection.size);
					DrawPaletteRegion(g, Color.FromArgb(192,255,255,255), region);
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


		bool TranslatePaletteCoord(Point pt, out Point outpoint)
		{
			pt.X -= paletteCellSpacing;
			pt.Y -= paletteCellSpacing;
			int tx = pt.X / (paletteCellSize + paletteCellSpacing);
			int ty = pt.Y / (paletteCellSize + paletteCellSpacing);
			outpoint = new Point(tx, ty);
			if (tx >= 16 || ty >= 16) return false;
			return true;
		}

		private void paletteViewer_MouseClick(object sender, MouseEventArgs e)
		{
			Point pt;
			bool valid = TranslatePaletteCoord(e.Location, out pt);
			if (!valid) return;
			selectedColorNum = pt.Y * 16 + pt.X;

			SyncColorSelection();
			UpdateValues();
		}

		void SyncColorSelection()
		{
			currPaletteSelection = GetPaletteSelectionForTileDisplay(selectedColorNum);
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

		bool viewerPan = false;
		Point panStartLocation;
		private void viewer_MouseDown(object sender, MouseEventArgs e)
		{
			viewer.Capture = true;
			if ((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0)
			{
				viewerPan = true;
				panStartLocation = viewer.PointToScreen(e.Location);
				this.Cursor = Cursors.Hand;
			}
		}

		private void viewer_MouseUp(object sender, MouseEventArgs e)
		{
			viewerPan = false;
			viewer.Capture = false;
		}

		private void viewer_MouseMove(object sender, MouseEventArgs e)
		{
			if (viewerPan)
			{
				var loc = viewer.PointToScreen(e.Location);
				int dx = loc.X - panStartLocation.X;
				int dy = loc.Y - panStartLocation.Y;
				panStartLocation = loc;

				int x = viewerPanel.AutoScrollPosition.X;
				int y = viewerPanel.AutoScrollPosition.Y;
				x += dx;
				y += dy;
				viewerPanel.AutoScrollPosition = new Point(-x, -y);
			}
			else
			{
				UpdateViewerMouseover(e.Location);
			}
		}

		class TileViewerBGState
		{
			public SNESGraphicsDecoder.TileEntry entry;
			public int bgnum;
		}
		TileViewerBGState currTileViewerBGState;
		int currViewingTile = -1;
		int currViewingTileBpp = -1;
		void RenderTileView(bool force=false)
		{
			//TODO - blech - handle invalid some other way with a dedicated black-setter
			bool valid = currViewingTile != -1;
			valid |= (currTileViewerBGState != null);
			if (!valid && !force) return;

			if (currTileViewerBGState != null)
			{
				//view a BG tile (no mode7 support yet) - itd be nice if we could generalize this code a bit
				//TODO - choose correct palette (commonize that code)
				int paletteStart = 0;
				var bmp = new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bmpdata = bmp.LockBits(new Rectangle(0, 0, 8, 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bgs = currTileViewerBGState;
				var oneTileEntry = new SNESGraphicsDecoder.TileEntry[] { bgs.entry };
				if (valid)
				{
					gd.DecodeBG((int*)bmpdata.Scan0, bmpdata.Stride / 4, oneTileEntry, si.BG[bgs.bgnum].TiledataAddr, SNESGraphicsDecoder.ScreenSize.Hacky_1x1, si.BG[bgs.bgnum].Bpp, 8, paletteStart);
					gd.Paletteize((int*)bmpdata.Scan0, 0, 0, 64);
					gd.Colorize((int*)bmpdata.Scan0, 0, 64);
				}

				bmp.UnlockBits(bmpdata);
				viewerTile.SetBitmap(bmp);
			}
			else
			{
				//view a tileset tile
				int bpp = currViewingTileBpp;

				var bmp = new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bmpdata = bmp.LockBits(new Rectangle(0, 0, 8, 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				if (valid) gd.RenderTilesToScreen((int*)bmpdata.Scan0, 1, 1, bmpdata.Stride / 4, bpp, currPaletteSelection.start, currViewingTile, 1);
				bmp.UnlockBits(bmpdata);
				viewerTile.SetBitmap(bmp);
			}
		}

		void UpdateViewerMouseover(Point loc)
		{
			currTileViewerBGState = null;
			currViewingTile = -1;
			currViewingTileBpp = -1;
			int tx = loc.X / 8;
			int ty = loc.Y / 8;
			switch (CurrDisplaySelection)
			{
			  case eDisplayType.Tiles4bpp:
					currViewingTileBpp = 4;
					currViewingTile = ty * 64 + tx;
					if (currViewingTile < 0 || currViewingTile >= (8192 / currViewingTileBpp))
						currViewingTile = -1;
					break;
				case eDisplayType.BG1:
				case eDisplayType.BG2:
				case eDisplayType.BG3:
				case eDisplayType.BG4:
					{
						var bg = si.BG[(int)CurrDisplaySelection];
						var map = gd.FetchTilemap(bg.ScreenAddr, bg.ScreenSize);
						if (bg.TileSize == 16) { tx /= 2; ty /= 2; } //worry about this later. need to pass a different flag into `currViewingTile`

						int tloc = ty * bg.ScreenSizeInTiles.Width + tx;
						if (tloc > map.Length) break;

						currTileViewerBGState = new TileViewerBGState();
						currTileViewerBGState.bgnum = (int)CurrDisplaySelection;
						currTileViewerBGState.entry = map[tloc];

						//public void DecodeBG(int* screen, int stride, TileEntry[] map, int tiledataBaseAddr, ScreenSize size, int bpp, int tilesize, int paletteStart)


						//var map = gd.FetchTilemap(bg.ScreenAddr, bg.ScreenSize);
						//int paletteStart = 0;
						//gd.DecodeBG(pixelptr, stride / 4, map, bg.TiledataAddr, bg.ScreenSize, bg.Bpp, bg.TileSize, paletteStart);
						//gd.Paletteize(pixelptr, 0, 0, numPixels);
					}
					break;
			}

			RenderTileView(true);
		}

		private void viewer_MouseEnter(object sender, EventArgs e)
		{
			tabctrlDetails.SelectedIndex = 1;
		}

		private void viewer_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void paletteViewer_MouseDown(object sender, MouseEventArgs e)
		{

		}

		private void paletteViewer_MouseEnter(object sender, EventArgs e)
		{
			tabctrlDetails.SelectedIndex = 0;
		}

		private void paletteViewer_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void paletteViewer_MouseMove(object sender, MouseEventArgs e)
		{
			Point pt;
			bool valid = TranslatePaletteCoord(e.Location, out pt);
			if (!valid) return;
			lastColorNum = pt.Y * 16 + pt.X;
			UpdateColorDetails();
		}

		void SyncBackdropColor()
		{
			if (checkBackdropColor.Checked)
			{
				int r = pnBackdropColor.BackColor.R;
				int g = pnBackdropColor.BackColor.G;
				int b = pnBackdropColor.BackColor.B;
				r >>= 3;
				g >>= 3;
				b >>= 3;
				int col = r | (g << 5) | (b << 10);
				LibsnesDll.snes_set_backdropColor(col);
			}
			else
			{
				LibsnesDll.snes_set_backdropColor(-1);
			}
		}

		private void checkBackdropColor_CheckedChanged(object sender, EventArgs e)
		{
			SyncBackdropColor();
		}

		private void pnBackdropColor_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var cd = new ColorDialog();
			cd.Color = pnBackdropColor.BackColor;
			if (cd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				pnBackdropColor.BackColor = cd.Color;
				SyncBackdropColor();
			}
		}


	}
}
