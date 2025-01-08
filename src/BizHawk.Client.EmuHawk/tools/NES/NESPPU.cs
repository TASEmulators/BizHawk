using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NesPPU : ToolFormBase, IToolFormAutoConfig
	{
		// TODO:
		// If 8/16 sprite mode, mouse over should put 32x64 version of sprite
		// Speedups
		// Smarter refreshing?  only refresh when things of changed, perhaps peek at the ppu to when the pattern table has changed, or sprites have moved
		// Maybe 48 individual bitmaps for sprites is faster than the overhead of redrawing all that transparent space

		public static Icon ToolIcon
			=> Properties.Resources.NesControllerIcon;

		private readonly byte[] _ppuBusPrev = new byte[0x3000];
		private readonly byte[] _palRamPrev = new byte[0x20];

		private int _scanline;

		private Bitmap _zoomBoxDefaultImage = new Bitmap(64, 64);
		private bool _forceChange;

		[RequiredService]
		public INESPPUViewable _nesCore { get; set; }

		private INESPPUViewable _ppu
			=> _nesCore!;

		[RequiredService]
		public IEmulator _core { get; set; }

		private IEmulator _emu
			=> _core!;

		[ConfigPersist]
		private int RefreshRateConfig
		{
			get => RefreshRate.Value;
			set => RefreshRate.Value = value;
		}

		private bool _chrRomView;
		[ConfigPersist]
		private bool ChrRomView
		{
			get => _chrRomView;
			set { _chrRomView = value; CalculateFormSize(); }
		}

		protected override string WindowTitleStatic => "PPU Viewer";

		public NesPPU()
		{
			InitializeComponent();
			Icon = ToolIcon;
			CalculateFormSize();
		}

		private void NesPPU_Load(object sender, EventArgs e)
		{
			ClearDetails();
			Generate(true);
			ChrRomViewReload();
		}

		protected override void UpdateBefore()
		{
			_ppu.InstallCallback2(() => Generate(), _scanline);
		}

		public override void Restart()
		{
			Generate(true);
			ChrRomViewReload();
		}

		private byte GetBit(byte[] ppuBus, int address, int bit)
		{
			return (byte)((ppuBus[address] >> (7 - bit)) & 1);
		}

		private bool CheckChange(byte[] palRam, byte[] ppuBus)
		{
			bool changed = false;
			for (int i = 0; i < 0x20; i++)
			{
				if (_palRamPrev[i] != palRam[i])
				{
					changed = true;
					break;
				}
			}

			if (!changed)
			{
				for (int i = 0; i < 0x2000; i++)
				{
					if (_ppuBusPrev[i] != ppuBus[i])
					{
						changed = true;
						break;
					}
				}
			}

			Buffer.BlockCopy(palRam, 0, _palRamPrev, 0, 0x20);
			Buffer.BlockCopy(ppuBus, 0, _ppuBusPrev, 0, 0x3000);

			if (_forceChange)
			{
				return true;
			}

			return changed;
		}

		private unsafe void DrawPatternView(PatternViewer dest, byte[] src, int[] FinalPalette, byte[] PALRAM)
		{
			int b0;
			int b1;
			byte value;
			int cvalue;

			var bmpdata = dest.Pattern.LockBits(
				new Rectangle(new Point(0, 0), dest.Pattern.Size),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			int* frameBuf = (int*)bmpdata.Scan0;
			for (int z = 0; z < 2; z++)
			{
				int pal;
				pal = z == 0 ? PatternView.Pal0 : PatternView.Pal1; // change?

				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						for (int x = 0; x < 8; x++)
						{
							for (int y = 0; y < 8; y++)
							{
								int address = (z << 12) + (i << 8) + (j << 4) + y;
								b0 = (byte)((src[address] >> (7 - x)) & 1);
								b1 = (byte)((src[address + 8] >> (7 - x)) & 1);

								value = (byte)(b0 + (b1 << 1));
								cvalue = FinalPalette[PALRAM[value + (pal << 2)]];
								int adr = (x + (j << 3)) + (y + (i << 3)) * (bmpdata.Stride >> 2);
								frameBuf[adr + (z << 7)] = cvalue;
							}
						}
					}
				}
			}

			dest.Pattern.UnlockBits(bmpdata);
			dest.Refresh();
		}

		private unsafe void Generate(bool now = false)
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_emu.Frame % RefreshRate.Value != 0 && !now)
				return;

			byte[] palRam = _ppu.GetPalRam();
			int[] finalPalette = _ppu.GetPalette();
			byte[] oam = _ppu.GetOam();
			byte[] ppuBus = _ppu.GetPPUBus();

			int b0;
			int b1;
			byte value;
			int cvalue;

			if (CheckChange(palRam, ppuBus))
			{
				_forceChange = false;

				// Pattern Viewer
				for (var i = 0; i < 16; i++)
				{
					PaletteView.BgPalettesPrev[i].Value = PaletteView.BgPalettes[i].Value;
					PaletteView.SpritePalettesPrev[i].Value = PaletteView.SpritePalettes[i].Value;
					PaletteView.BgPalettes[i].Value = finalPalette[palRam[PaletteView.BgPalettes[i].Address]];
					PaletteView.SpritePalettes[i].Value = finalPalette[palRam[PaletteView.SpritePalettes[i].Address]];
				}

				if (PaletteView.HasChanged())
				{
					PaletteView.Refresh();
				}

				DrawPatternView(PatternView, ppuBus, finalPalette, palRam);
			}

			var bmpData2 = SpriteView.Sprites.LockBits(new Rectangle(new Point(0, 0), SpriteView.Sprites.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			var frameBuf2 = (int*)bmpData2.Scan0.ToPointer();

			int ptAdd = _ppu.SPBaseHigh ? 0x1000 : 0;
			bool is8x16 = _ppu.SPTall;


			// Sprite Viewer
			for (int n = 0; n < 4; n++)
			{
				for (int r = 0; r < 16; r++)
				{
					int baseAddr = (r << 2) + (n << 6);
					int tileNum = oam[baseAddr + 1];
					int patternAddr;

					if (is8x16)
					{
						patternAddr = (tileNum >> 1) * 0x20;
						patternAddr += 0x1000 * (tileNum & 1);
					}
					else
					{
						patternAddr = tileNum * 0x10;
						patternAddr += ptAdd;
					}

					int attributes = oam[baseAddr + 2];
					int palette = attributes & 0x03;

					for (int x = 0; x < 8; x++)
					{
						for (int y = 0; y < 8; y++)
						{
							int address = patternAddr + y;
							b0 = (byte)((ppuBus[address] >> (7 - x)) & 1);
							b1 = (byte)((ppuBus[address + 8] >> (7 - x)) & 1);
							value = (byte)(b0 + (b1 << 1));
							cvalue = finalPalette[palRam[16 + value + (palette << 2)]];

							int adr = (x + (r * 16)) + (y + (n * 24)) * (bmpData2.Stride >> 2);
							frameBuf2[adr] = cvalue;
						}

						if (is8x16)
						{
							patternAddr += 0x10;
							for (int y = 0; y < 8; y++)
							{
								int address = patternAddr + y;
								b0 = (byte)((ppuBus[address] >> (7 - x)) & 1);
								b1 = (byte)((ppuBus[address + 8] >> (7 - x)) & 1);
								value = (byte)(b0 + (b1 << 1));
								cvalue = finalPalette[palRam[16 + value + (palette << 2)]];

								int adr = (x + (r << 4)) + ((y + 8) + (n * 24)) * (bmpData2.Stride >> 2);
								frameBuf2[adr] = cvalue;
							}

							patternAddr -= 0x10;
						}
					}
				}
			}

			SpriteView.Sprites.UnlockBits(bmpData2);
			SpriteView.Refresh();

			HandleSpriteViewMouseMove(SpriteView.PointToClient(MousePosition));
			HandlePaletteViewMouseMove(PaletteView.PointToClient(MousePosition));
		}

		private void ClearDetails()
		{
			DetailsBox.Text = "Details";
			AddressLabel.Text = "";
			ValueLabel.Text = "";
			Value2Label.Text = "";
			Value3Label.Text = "";
			Value4Label.Text = "";
			Value5Label.Text = "";
			ZoomBox.Image = _zoomBoxDefaultImage;
		}

		private void UpdatePaletteSelection()
		{
			_forceChange = true;
			Table0PaletteLabel.Text = $"Palette: {PatternView.Pal0}";
			Table1PaletteLabel.Text = $"Palette: {PatternView.Pal1}";
		}

		private static Bitmap Section(Image srcBitmap, Rectangle section, bool is8x16)
		{
			// Create the new bitmap and associated graphics object
			var bmp = new Bitmap(64, 64);
			var g = Graphics.FromImage(bmp);

			// Draw the specified section of the source bitmap to the new one
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

			var rect = is8x16 ? new Rectangle(0, 0, 32, 64) : new Rectangle(0, 0, 64, 64);
			g.DrawImage(srcBitmap, rect, section, GraphicsUnit.Pixel);
			g.Dispose();

			return bmp;
		}

		private void HandleDefaultImage()
		{
			if (ModifierKeys == Keys.Shift)
			{
				_zoomBoxDefaultImage = ZoomBox.Image as Bitmap;
			}
		}

		private void Screenshot(Bitmap b, string suffix)
		{
			b.SaveAsFile(Game, suffix, VSystemID.Raw.NES, Config.PathEntries, this);
		}

		private void SavePaletteScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			Screenshot(PaletteView.ToBitMap(), "Palettes");
		}

		private void SavePatternScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			Screenshot(PatternView.ToBitMap(), "Patterns");
		}

		private void SaveSpriteScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			Screenshot(SpriteView.ToBitMap(), "Sprites");
		}

		private void CopyPaletteToClipboardMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.ToBitMap().ToClipBoard();
		}

		private void CopyPatternToClipboardMenuItem_Click(object sender, EventArgs e)
		{
			PatternView.ToBitMap().ToClipBoard();
		}

		private void CopySpriteToClipboardMenuItem_Click(object sender, EventArgs e)
		{
			SpriteView.ToBitMap().ToClipBoard();
		}

		private void Table0PaletteSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Table0P0MenuItem.Checked = PatternView.Pal0 == 0;
			Table0P1MenuItem.Checked = PatternView.Pal0 == 1;
			Table0P2MenuItem.Checked = PatternView.Pal0 == 2;
			Table0P3MenuItem.Checked = PatternView.Pal0 == 3;
			Table0P4MenuItem.Checked = PatternView.Pal0 == 4;
			Table0P5MenuItem.Checked = PatternView.Pal0 == 5;
			Table0P6MenuItem.Checked = PatternView.Pal0 == 6;
			Table0P7MenuItem.Checked = PatternView.Pal0 == 7;
		}

		private void Table1PaletteSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Table1P0MenuItem.Checked = PatternView.Pal1 == 0;
			Table1P1MenuItem.Checked = PatternView.Pal1 == 1;
			Table1P2MenuItem.Checked = PatternView.Pal1 == 2;
			Table1P3MenuItem.Checked = PatternView.Pal1 == 3;
			Table1P4MenuItem.Checked = PatternView.Pal1 == 4;
			Table1P5MenuItem.Checked = PatternView.Pal1 == 5;
			Table1P6MenuItem.Checked = PatternView.Pal1 == 6;
			Table1P7MenuItem.Checked = PatternView.Pal1 == 7;
		}

		private void Palette_Click(object sender, EventArgs e)
		{
			if (sender == Table0P0MenuItem)
			{
				PatternView.Pal0 = 0;
			}
			else if (sender == Table0P1MenuItem)
			{
				PatternView.Pal0 = 1;
			}
			else if (sender == Table0P2MenuItem)
			{
				PatternView.Pal0 = 2;
			}
			else if (sender == Table0P3MenuItem)
			{
				PatternView.Pal0 = 3;
			}
			else if (sender == Table0P4MenuItem)
			{
				PatternView.Pal0 = 4;
			}
			else if (sender == Table0P5MenuItem)
			{
				PatternView.Pal0 = 5;
			}
			else if (sender == Table0P6MenuItem)
			{
				PatternView.Pal0 = 6;
			}
			else if (sender == Table0P7MenuItem)
			{
				PatternView.Pal0 = 7;
			}
			else if (sender == Table1P0MenuItem)
			{
				PatternView.Pal1 = 0;
			}
			else if (sender == Table1P1MenuItem)
			{
				PatternView.Pal1 = 1;
			}
			else if (sender == Table1P2MenuItem)
			{
				PatternView.Pal1 = 2;
			}
			else if (sender == Table1P3MenuItem)
			{
				PatternView.Pal1 = 3;
			}
			else if (sender == Table1P4MenuItem)
			{
				PatternView.Pal1 = 4;
			}
			else if (sender == Table1P5MenuItem)
			{
				PatternView.Pal1 = 5;
			}
			else if (sender == Table1P6MenuItem)
			{
				PatternView.Pal1 = 6;
			}
			else if (sender == Table1P7MenuItem)
			{
				PatternView.Pal1 = 7;
			}

			UpdatePaletteSelection();
		}

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			cHRROMTileViewerToolStripMenuItem.Checked = ChrRomView;
		}

		private void PaletteRefreshMenuItem_Click(object sender, EventArgs e)
		{
			PaletteView.Refresh();
		}

		private void PatternRefreshMenuItem_Click(object sender, EventArgs e)
		{
			PatternView.Refresh();
		}

		private void SpriteRefreshMenuItem_Click(object sender, EventArgs e)
		{
			SpriteView.Refresh();
		}

		private void NesPPU_MouseClick(object sender, MouseEventArgs e)
		{
			ZoomBox.Image = new Bitmap(64, 64);
		}

		private void NesPPU_KeyDown(object sender, KeyEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
			{
				// find the control under the mouse
				var m = Cursor.Position;
				Control top = this;
				Control found;
				do
				{
					found = top.GetChildAtPoint(top.PointToClient(m));
					top = found;
				}
				while (found != null && found.HasChildren);

				if (found != null)
				{
					var method = found.GetType().GetMethod("ScreenshotToClipboard", Type.EmptyTypes);
					if (method != null)
					{
						method.Invoke(found, null);
					}
					else if (found is PictureBox box)
					{
						Clipboard.SetImage(box.Image);
					}
					else
					{
						return;
					}

					toolStripStatusLabel1.Text = $"{found.Text} copied to clipboard.";

					Messagetimer.Stop();
					Messagetimer.Start();
				}
			}
		}

		private void MessageTimer_Tick(object sender, EventArgs e)
		{
			Messagetimer.Stop();
			toolStripStatusLabel1.Text = "Use CTRL+C to copy the pane under the mouse to the clipboard.";
		}

		private void PaletteView_MouseClick(object sender, MouseEventArgs e)
		{
			HandleDefaultImage();
		}

		private void SpriteView_MouseClick(object sender, MouseEventArgs e)
		{
			HandleDefaultImage();
		}

		private void SpriteView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Sprites";
		}

		private void SpriteView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}
		
		private void SpriteView_MouseMove(object sender, MouseEventArgs e)
		{
			HandleSpriteViewMouseMove(e.Location);
		}

		private void HandleSpriteViewMouseMove(Point e)
		{
			if (e.X < SpriteView.ClientRectangle.Left) return;
			if (e.Y < SpriteView.ClientRectangle.Top) return;
			if (e.X >= SpriteView.ClientRectangle.Right) return;
			if (e.Y >= SpriteView.ClientRectangle.Bottom) return;

			byte[] oam = _ppu.GetOam();
			byte[] ppuBus = _ppu.GetPPUBus(); // caching is quicker, but not really correct in this case

			bool is8x16 = _ppu.SPTall;
			
			//figure out which sprite we're over
			int spriteSlotY = e.Y / 8;
			int spriteSlotX = e.X / 8;

			//exclude mouse over empty area (vertical). this depends on how big the sprites are
			if (is8x16)
			{
				if (spriteSlotY % 3 == 2)
					return;
			}
			else
			{
				if (spriteSlotY % 3 != 0)
					return;
			}

			//exclude mouse over empty area to the right of sprites
			if (spriteSlotX % 2 == 1)
				return;

			//convert these 8x8 "slots" we've been working on to more sensible slots
			spriteSlotX /= 2;
			spriteSlotY /= 3;

			//if these were utterly senseless slots (so far out of range) then bail
			if (spriteSlotX < 0 || spriteSlotX >= 16)
				return;

			if (spriteSlotY < 0 || spriteSlotY >= 4)
				return;

			//find the final sprite number that's being hovered
			var spriteNumber = spriteSlotY * 16 + spriteSlotX;

			int x = oam[(spriteNumber * 4) + 3];
			int y = oam[spriteNumber * 4];
			var color = oam[(spriteNumber * 4) + 2] & 0x03;
			var attributes = oam[(spriteNumber * 4) + 2];

			var flags = "Flags: ";
			int h = GetBit(ppuBus, attributes, 6);
			int v = GetBit(ppuBus, attributes, 7);
			int priority = GetBit(ppuBus, attributes, 5);
			if (h > 0)
			{
				flags += "H ";
			}

			if (v > 0)
			{
				flags += "V ";
			}

			if (priority > 0)
			{
				flags += "Behind";
			}
			else
			{
				flags += "Front";
			}

			int tile = oam[spriteNumber * 4 + 1];
			if (is8x16)
			{
				if ((tile & 1) != 0)
					tile += 256;
				tile &= ~1;
			}

			AddressLabel.Text = $"Number: {spriteNumber:X2}";
			ValueLabel.Text = $"X: {x:X2}";
			Value2Label.Text = $"Y: {y:X2}";
			Value3Label.Text = $"Tile: {tile:X2}";
			Value4Label.Text = $"Color: {color}";
			Value5Label.Text = flags;

			if (is8x16)
			{
				ZoomBox.Image = Section(
					SpriteView.Sprites, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 24) * 24), new Size(8, 16)), true);
			}
			else
			{
				ZoomBox.Image = Section(
					SpriteView.Sprites, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 8) * 8), new Size(8, 8)), false);
			}
		}

		private void PaletteView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void PaletteView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Palettes";
		}

		private void PaletteView_MouseMove(object sender, MouseEventArgs e)
		{
			HandlePaletteViewMouseMove(e.Location);
		}

		private readonly SolidBrush _brush = new(default);

		private void HandlePaletteViewMouseMove(Point e)
		{
			if (e.X < PaletteView.ClientRectangle.Left) return;
			if (e.Y < PaletteView.ClientRectangle.Top) return;
			if (e.X >= PaletteView.ClientRectangle.Right) return;
			if (e.Y >= PaletteView.ClientRectangle.Bottom) return;

			int baseAddr = 0x3F00;
			if (e.Y > 16)
			{
				baseAddr += 16;
			}

			int column = e.X / 16;
			int addr = column + baseAddr;
			AddressLabel.Text = $"Address: 0x{addr:X4}";
			int val;

			var bmp = new Bitmap(64, 64);
			var g = Graphics.FromImage(bmp);

			byte[] palRam = _ppu.GetPalRam();

			PaletteViewer.Palette pal;
			if (baseAddr == 0x3F00)
			{
				ValueLabel.Text = $"ID: BG{column / 4}";
				pal = PaletteView.BgPalettes[column];
			}
			else
			{
				ValueLabel.Text = $"ID: SPR{column / 4}";
				pal = PaletteView.SpritePalettes[column];
			}
			val = palRam[pal.Address];
			_brush.Color = pal.Color;
			g.FillRectangle(_brush, 0, 0, 64, 64);

			g.Dispose();

			Value3Label.Text = $"Color: 0x{val:X2}";
			Value4Label.Text = $"Offset: {addr & 0x03}";
			ZoomBox.Image = bmp;
		}

		private void PatternView_Click(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (e.X < PatternView.Width / 2)
				{
					PatternView.Pal0++;
					if (PatternView.Pal0 > 7)
					{
						PatternView.Pal0 = 0;
					}
				}
				else
				{
					PatternView.Pal1++;
					if (PatternView.Pal1 > 7)
					{
						PatternView.Pal1 = 0;
					}
				}

				UpdatePaletteSelection();
			}

			HandleDefaultImage();
		}

		private void PatternView_MouseEnter(object sender, EventArgs e)
		{
			DetailsBox.Text = "Details - Patterns";
		}

		private void PatternView_MouseLeave(object sender, EventArgs e)
		{
			ClearDetails();
		}

		private void PatternView_MouseMove(object sender, MouseEventArgs e)
		{
			int table = 0;
			int address;
			int tile;
			if (e.X > PatternView.Width / 2)
			{
				table = 1;
			}

			if (table == 0)
			{
				tile = (e.X - 1) / 8;
				address = tile * 16;
			}
			else
			{
				tile = (e.X - 128) / 8;
				address = 0x1000 + (tile * 16);
			}

			address += (e.Y / 8) * 256;
			tile += (e.Y / 8) * 16;
			var usage = "Usage: ";

			if (_ppu.BGBaseHigh == address >= 0x1000) // bghigh
			{
				usage = "BG";
			}
			else if (_ppu.SPBaseHigh == address >= 0x1000) // sphigh
			{
				usage = "SPR";
			}

			if (_ppu.SPTall) // spbig
			{
				usage += " (SPR16)";
			}

			AddressLabel.Text = $"Address: {address:X4}";
			ValueLabel.Text = $"Table {table}";
			Value3Label.Text = $"Tile {tile:X2}";
			Value4Label.Text = usage;

			ZoomBox.Image = Section(PatternView.Pattern, new Rectangle(new Point((e.X / 8) * 8, (e.Y / 8) * 8), new Size(8, 8)), false);
		}

		private void ScanlineTextBox_TextChanged(object sender, EventArgs e)
		{
			if (int.TryParse(txtScanline.Text, out _scanline))
			{
				_ppu.InstallCallback2(() => Generate(), _scanline);
			}
		}

		private void NesPPU_FormClosed(object sender, FormClosedEventArgs e)
			=> _ppu.RemoveCallback2();

		private MemoryDomain _chrRom;
		private readonly byte[] _chrRomCache = new byte[8192];

		private void ChrROMTileViewerToolStripMenuItem_Click(object sender, EventArgs e)
			=> ChrRomView = !ChrRomView;

		private void CalculateFormSize()
		{
			Width = ChrRomView ? 861 : 580;
		}

		private void ChrRomViewReload()
		{
			_chrRom = _ppu.GetCHRROM();
			if (_chrRom == null)
			{
				numericUpDownCHRROMBank.Enabled = false;
				Array.Clear(_chrRomCache, 0, 8192);
			}
			else
			{
				numericUpDownCHRROMBank.Enabled = true;
				numericUpDownCHRROMBank.Minimum = 0;
				numericUpDownCHRROMBank.Maximum = _chrRom.Size / 8192 - 1;
				numericUpDownCHRROMBank.Value = Math.Min(numericUpDownCHRROMBank.Value, numericUpDownCHRROMBank.Maximum);
			}
			ChrRomViewRefresh();
		}

		private void ChrRomViewRefresh()
		{
			if (_chrRom != null)
			{
				int offs = 8192 * (int)numericUpDownCHRROMBank.Value;
				for (int i = 0; i < 8192; i++)
					_chrRomCache[i] = _chrRom.PeekByte(offs + i);

				DrawPatternView(CHRROMView, _chrRomCache, _ppu.GetPalette(), _ppu.GetPalRam());
			}
		}

		private void NumericUpDownChrRomBank_ValueChanged(object sender, EventArgs e)
		{
			ChrRomViewRefresh();
		}
	}
}
