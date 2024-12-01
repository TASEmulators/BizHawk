using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class GbGpuView : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.GambatteIcon;

		[RequiredService]
		public IGameboyCommon/*?*/ _gbCore { get; set; }

		private IGameboyCommon Gb
			=> _gbCore!;

		// TODO: freeze semantics are a bit weird: details for a mouseover or freeze are taken from the current
		// state, not the state at the last callback (and so can be quite different when update is set to manual).
		// I'm not quite sure what's the best thing to do...

		// TODO: color stuff.  In GB mode, the colors used are exactly whatever you set in palette config.  In GBC
		// mode, the following "real color" algorithm is used (NB: converting 555->888 at the same time):
		// r' = 6.5r + 1.0g + 0.5b
		// g' = 0.0r + 6.0g + 2.0b
		// b' = 1.5r + 1.0g + 5.5b
		//
		// other emulators use "vivid color" modes, such as:
		// r' = 8.25r
		// g' = 8.25g
		// b' = 8.25b

		private bool _cgb; // set once at start
		private int _lcdc; // set at each callback

		/// <summary>
		/// Whether the tiles are being drawn with the sprite or bg palettes
		/// </summary>
		private bool _tilesPalIsSprite;

		/// <summary>
		/// How far (in bytes, I guess?) we should offset into the tiles palette
		/// </summary>
		private int _tilesPalOffset;

		private IntPtr ComputeTilesPalFromMemory(IGPUMemoryAreas m)
		{
			var ret = _tilesPalIsSprite ? m.Sppal : m.Bgpal;
			ret += _tilesPalOffset;
			return ret;
		}

		private Color _spriteback;
		
		[ConfigPersist]
		public Color Spriteback
		{
			get => _spriteback;
			set
			{
				_spriteback = Color.FromArgb(255, value); // force fully opaque
				panelSpriteBackColor.BackColor = _spriteback;
				labelSpriteBackColor.Text = $"({_spriteback.R},{_spriteback.G},{_spriteback.B})";
			}
		}

		protected override string WindowTitleStatic => "GPU Viewer";

		public GbGpuView()
		{
			InitializeComponent();
			Icon = ToolIcon;
			bmpViewBG.ChangeBitmapSize(256, 256);
			bmpViewWin.ChangeBitmapSize(256, 256);
			bmpViewTiles1.ChangeBitmapSize(128, 192);
			bmpViewTiles2.ChangeBitmapSize(128, 192);
			bmpViewBGPal.ChangeBitmapSize(8, 4);
			bmpViewSPPal.ChangeBitmapSize(8, 4);
			bmpViewOAM.ChangeBitmapSize(320, 16);
			bmpViewOBJ.ChangeBitmapSize(256, 256);
			bmpViewDetails.ChangeBitmapSize(8, 16);
			bmpViewMemory.ChangeBitmapSize(8, 16);

			hScrollBarScanline.Value = 0;
			hScrollBarScanline_ValueChanged(null, null); // not firing in this case??
			radioButtonRefreshFrame.Checked = true;

			KeyPreview = true;

			_messageTimer.Interval = 5000;
			_messageTimer.Tick += MessageTimer_Tick;
			Spriteback = Color.Lime; // will be overridden from config after construct
		}

		public override void Restart()
		{
			_cgb = Gb.IsCGBMode;
			_lcdc = 0;

			label4.Enabled = _cgb;
			bmpViewBG.Clear();
			bmpViewWin.Clear();
			bmpViewTiles1.Clear();
			bmpViewTiles2.Clear();
			bmpViewBGPal.Clear();
			bmpViewSPPal.Clear();
			bmpViewOAM.Clear();
			bmpViewOBJ.Clear();
			bmpViewDetails.Clear();
			bmpViewMemory.Clear();
			_cbScanlineEmu = -4; // force refresh
		}


		/// <summary>
		/// draw a single 2bpp tile
		/// </summary>
		/// <param name="tile">16 byte 2bpp 8x8 tile (gb format)</param>
		/// <param name="dest">top left origin on 32bit bitmap</param>
		/// <param name="pitch">pitch of bitmap in 4 byte units</param>
		/// <param name="pal">4 palette colors</param>
		private static unsafe void DrawTile(byte* tile, int* dest, int pitch, int* pal)
		{
			for (int y = 0; y < 8; y++)
			{
				int loplane = *tile++;
				int hiplane = *tile++;
				hiplane <<= 1; // msb
				dest += 7;
				for (int x = 0; x < 8; x++) // right to left
				{
					int color = loplane & 1 | hiplane & 2;
					*dest-- = (int)(pal[color] | 0xFF000000);
					loplane >>= 1;
					hiplane >>= 1;
				}
				dest++;
				dest += pitch;
			}
		}

		/// <summary>
		/// draw a single 2bpp tile, with hflip and vflip
		/// </summary>
		/// <param name="tile">16 byte 2bpp 8x8 tile (gb format)</param>
		/// <param name="dest">top left origin on 32bit bitmap</param>
		/// <param name="pitch">pitch of bitmap in 4 byte units</param>
		/// <param name="pal">4 palette colors</param>
		/// <param name="hFlip">true to flip horizontally</param>
		/// <param name="vFlip">true to flip vertically</param>
		private static unsafe void DrawTileHv(byte* tile, int* dest, int pitch, int* pal, bool hFlip, bool vFlip)
		{
			if (vFlip)
				dest += pitch * 7;
			for (int y = 0; y < 8; y++)
			{
				int loPlane = *tile++;
				int hiPlane = *tile++;
				hiPlane <<= 1; // msb
				if (!hFlip)
					dest += 7;
				for (int x = 0; x < 8; x++) // right to left
				{
					int color = loPlane & 1 | hiPlane & 2;
					*dest = (int)(pal[color] | 0xFF000000);
					if (!hFlip)
						dest--;
					else
						dest++;
					loPlane >>= 1;
					hiPlane >>= 1;
				}
				if (!hFlip)
					dest++;
				else
					dest -= 8;
				if (!vFlip)
					dest += pitch;
				else
					dest -= pitch;
			}
		}

		/// <summary>
		/// draw a bg map, cgb format
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 256x256</param>
		/// <param name="_map">tilemap, 32x32 bytes. extended tilemap assumed to be @+8k</param>
		/// <param name="tiles">base tiledata location. second bank tiledata assumed to be @+8k</param>
		/// <param name="wrap">true if tileindexes are s8 (not u8)</param>
		/// <param name="_pal">8 palettes (4 colors each)</param>
		private static unsafe void DrawBgCgb(Bitmap b, IntPtr _map, IntPtr tiles, bool wrap, IntPtr _pal)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			byte* map = (byte*)_map;
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int); // in int*s, not bytes
			int* pal = (int*)_pal;

			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					int tileIndex = map[0];
					int tileExt = map[8192];
					if (wrap && tileIndex >= 128)
						tileIndex -= 256;
					byte* tile = (byte*)(tiles + tileIndex * 16);
					if (tileExt.Bit(3)) // second bank
						tile += 8192;

					int* thisPal = pal + 4 * (tileExt & 7);

					DrawTileHv(tile, dest, pitch, thisPal, tileExt.Bit(5), tileExt.Bit(6));
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockData);
		}

		/// <summary>
		/// draw a bg map, dmg format
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 256x256</param>
		/// <param name="_map">tilemap, 32x32 bytes</param>
		/// <param name="_tiles">base tiledata location</param>
		/// <param name="wrap">true if tileindexes are s8 (not u8)</param>
		/// <param name="_pal">1 palette (4 colors)</param>
		private static unsafe void DrawBgDmg(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap, IntPtr _pal)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			byte* map = (byte*)_map;
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int); // in int*s, not bytes
			int* pal = (int*)_pal;

			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					int tileIndex = map[0];
					if (wrap && tileIndex >= 128)
						tileIndex -= 256;
					byte* tile = (byte*)(_tiles + tileIndex * 16);
					DrawTile(tile, dest, pitch, pal);
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockData);
		}

		/// <summary>
		/// draw a full bank of 384 tiles
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 128x192</param>
		/// <param name="_tiles">base tile address</param>
		/// <param name="_pal">single palette to use on all tiles</param>
		private static unsafe void DrawTiles(Bitmap b, IntPtr _tiles, IntPtr _pal)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, 128, 192), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			int* pal = (int*)_pal;
			byte* tile = (byte*)_tiles;

			for (int ty = 0; ty < 24; ty++)
			{
				for (int tx = 0; tx < 16; tx++)
				{
					DrawTile(tile, dest, pitch, pal);
					tile += 16;
					dest += 8;
				}
				dest -= 128;
				dest += pitch * 8;
			}
			b.UnlockBits(lockData);
		}

		/// <summary>
		/// draw oam data
		/// </summary>
		/// <param name="b">bitmap to draw to.  should be 320x8 (!tall), 320x16 (tall)</param>
		/// <param name="_oam">oam data, 4 * 40 bytes</param>
		/// <param name="_tiles">base tiledata location. cgb: second bank tiledata assumed to be @+8k</param>
		/// <param name="_pal">2 (dmg) or 8 (cgb) palettes</param>
		/// <param name="tall">true for 8x16 sprites; else 8x8</param>
		/// <param name="cgb">true for cgb (more palettes, second bank tiles)</param>
		private static unsafe void DrawOam(Bitmap b, IntPtr _oam, IntPtr _tiles, IntPtr _pal, bool tall, bool cgb)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, 320, tall ? 16 : 8), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			int* pal = (int*)_pal;
			byte* oam = (byte*)_oam;

			for (int s = 0; s < 40; s++)
			{
				oam += 2; // yPos, xPos
				int tileIndex = *oam++;
				int flags = *oam++;
				bool vFlip = flags.Bit(6);
				bool hFlip = flags.Bit(5);
				if (tall)
				{
					// i assume 8x16 vFlip flips the whole thing, not just each tile?
					if (vFlip)
					{
						tileIndex |= 1;
					}
					else
					{
						tileIndex &= 0xfe;
					}
				}

				byte* tile = (byte*)(_tiles + tileIndex * 16);
				int* thisPal = pal + 4 * (cgb ? flags & 7 : flags >> 4 & 1);
				if (cgb && flags.Bit(3))
					tile += 8192;

				DrawTileHv(tile, dest, pitch, thisPal, hFlip, vFlip);

				if (tall)
				{
					DrawTileHv(tile + 16, dest + pitch * 8, pitch, thisPal, hFlip, vFlip);
				}

				dest += 8;
			}

			b.UnlockBits(lockData);
		}

		/// <summary>
		/// draw objects from oam data
		/// </summary>
		/// <param name="b">bitmap to draw to.  should be 320x8 (!tall), 320x16 (tall)</param>
		/// <param name="_oam">oam data, 4 * 40 bytes</param>
		/// <param name="_tiles">base tiledata location. cgb: second bank tiledata assumed to be @+8k</param>
		/// <param name="_pal">2 (dmg) or 8 (cgb) palettes</param>
		/// <param name="tall">true for 8x16 sprites; else 8x8</param>
		/// <param name="cgb">true for cgb (more palettes, second bank tiles)</param>
		private static unsafe void DrawObj(Bitmap b, IntPtr _oam, IntPtr _tiles, IntPtr _pal, bool tall, bool cgb)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			int* pal = (int*)_pal;
			byte* oam = (byte*)_oam;

			// clear out the old sprite data
			BmpView.Clear_Selected_Region((byte*)lockData.Scan0, (uint)(lockData.Height * lockData.Stride));

			for (int s = 0; s < 40; s++)
			{
				int yPos = *oam++;
				int xPos = *oam++;
				dest += xPos + yPos * pitch;
				int tileIndex = *oam++;
				int flags = *oam++;
				bool vFlip = flags.Bit(6);
				bool hFlip = flags.Bit(5);
				if (tall)
				{
					// i assume 8x16 vFlip flips the whole thing, not just each tile?
					if (vFlip)
					{
						tileIndex |= 1;
					}
					else
					{
						tileIndex &= 0xfe;
					}
				}

				byte* tile = (byte*)(_tiles + tileIndex * 16);
				int* thisPal = pal + 4 * (cgb ? flags & 7 : flags >> 4 & 1);
				if (cgb && flags.Bit(3))
					tile += 8192;

				// only draw tiles that are completely in bounds so to avoid out of bounds accesses
				if ((xPos <= 248) && (yPos <= 240))
				{
					DrawTileHv(tile, dest, pitch, thisPal, hFlip, vFlip);

					if (tall)
					{
						DrawTileHv(tile + 16, dest + pitch * 8, pitch, thisPal, hFlip, vFlip);
					}
				}

				dest -= xPos + yPos * pitch;
			}

			b.UnlockBits(lockData);
		}

		/// <summary>
		/// draw a palette directly
		/// </summary>
		/// <param name="b">bitmap to draw to.  should be numpals x 4</param>
		/// <param name="_pal">start of palettes</param>
		/// <param name="numpals">number of palettes (not colors)</param>
		private static unsafe void DrawPal(Bitmap b, IntPtr _pal, int numpals)
		{
			var lockData = b.LockBits(new Rectangle(0, 0, numpals, 4), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);
			int* pal = (int*)_pal;

			for (int px = 0; px < numpals; px++)
			{
				for (int py = 0; py < 4; py++)
				{
					*dest = (int)(*pal++ | 0xFF000000);
					dest += pitch;
				}
				dest -= pitch * 4;
				dest++;
			}
			b.UnlockBits(lockData);
		}

		private void ScanlineCallback(byte lcdc)
		{
			using (var memory = Gb.LockGPU())
			{
				var bgPal = memory.Bgpal;
				var spPal = memory.Sppal;
				var oam = memory.Oam;
				var vram = memory.Vram;
				var tilesPal = ComputeTilesPalFromMemory(memory);

				_lcdc = lcdc;
				// set alpha on all pixels
#if false
				// TODO: This probably shouldn't be done on any cores at all.  Let the tool make a separate copy of palettes if it needs alpha,
				// or compel the cores to send data with alpha already set.  What was this actually solving?
				unsafe
				{
					int* p = (int*)_bgpal;
					for (int i = 0; i < 32; i++)
						p[i] |= unchecked((int)0xff000000);
					p = (int*)_sppal;
					for (int i = 0; i < 32; i++)
						p[i] |= unchecked((int)0xff000000);
					int c = Spriteback.ToArgb();
					for (int i = 0; i < 32; i += 4)
						p[i] = c;
				}
#endif

				// bg maps
				if (!_cgb)
				{
					DrawBgDmg(
						bmpViewBG.Bmp,
						vram + (lcdc.Bit(3) ? 0x1c00 : 0x1800),
						vram + (lcdc.Bit(4) ? 0x0000 : 0x1000),
						!lcdc.Bit(4),
						bgPal);

					DrawBgDmg(
						bmpViewWin.Bmp,
						vram + (lcdc.Bit(6) ? 0x1c00 : 0x1800),
						vram + 0x1000, // force win to second tile bank???
						true,
						bgPal);
				}
				else
				{
					DrawBgCgb(
						bmpViewBG.Bmp,
						vram + (lcdc.Bit(3) ? 0x1c00 : 0x1800),
						vram + (lcdc.Bit(4) ? 0x0000 : 0x1000),
						!lcdc.Bit(4),
						bgPal);

					DrawBgCgb(
						bmpViewWin.Bmp,
						vram + (lcdc.Bit(6) ? 0x1c00 : 0x1800),
						vram + 0x1000, // force win to second tile bank???
						true,
						bgPal);
				}
				bmpViewBG.Refresh();
				bmpViewWin.Refresh();

				// tile display
				// TODO: user selects palette to use, instead of fixed palette 0
				// or possibly "smart" where, if a tile is in use, it's drawn with one of the palettes actually being used with it?
				DrawTiles(bmpViewTiles1.Bmp, vram, tilesPal);
				bmpViewTiles1.Refresh();
				if (_cgb)
				{
					DrawTiles(bmpViewTiles2.Bmp, vram + 0x2000, tilesPal);
					bmpViewTiles2.Refresh();
				}

				// palettes
				if (_cgb)
				{
					bmpViewBGPal.ChangeBitmapSize(8, 4);
					if (bmpViewBGPal.Width != 128)
						bmpViewBGPal.Width = 128;
					bmpViewSPPal.ChangeBitmapSize(8, 4);
					if (bmpViewSPPal.Width != 128)
						bmpViewSPPal.Width = 128;
					DrawPal(bmpViewBGPal.Bmp, bgPal, 8);
					DrawPal(bmpViewSPPal.Bmp, spPal, 8);
				}
				else
				{
					bmpViewBGPal.ChangeBitmapSize(1, 4);
					if (bmpViewBGPal.Width != 16)
						bmpViewBGPal.Width = 16;
					bmpViewSPPal.ChangeBitmapSize(2, 4);
					if (bmpViewSPPal.Width != 32)
						bmpViewSPPal.Width = 32;
					DrawPal(bmpViewBGPal.Bmp, bgPal, 1);
					DrawPal(bmpViewSPPal.Bmp, spPal, 2);
				}
				bmpViewBGPal.Refresh();
				bmpViewSPPal.Refresh();

				// oam (sprites)
				if (lcdc.Bit(2)) // 8x16
				{
					bmpViewOAM.ChangeBitmapSize(320, 16);
					if (bmpViewOAM.Height != 16)
						bmpViewOAM.Height = 16;
				}
				else
				{
					bmpViewOAM.ChangeBitmapSize(320, 8);
					if (bmpViewOAM.Height != 8)
						bmpViewOAM.Height = 8;
				}
				DrawOam(bmpViewOAM.Bmp, oam, vram, spPal, lcdc.Bit(2), _cgb);
				bmpViewOAM.Refresh();

				// oam (objects)
				DrawObj(bmpViewOBJ.Bmp, oam, vram, spPal, lcdc.Bit(2), _cgb);
				bmpViewOBJ.Refresh();
			}
			// try to run the current mouseover, to refresh if the mouse is being held over a pane while the emulator runs
			// this doesn't really work well; the update rate seems to be throttled
			var e = new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0);
			OnMouseMove(e);
		}

		private void GbGpuView_FormClosed(object sender, FormClosedEventArgs e)
			=> _gbCore?.SetScanlineCallback(null, 0);

		private void radioButtonRefreshFrame_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }
		private void radioButtonRefreshScanline_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }
		private void radioButtonRefreshManual_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }

		private void ComputeRefreshValues()
		{
			if (radioButtonRefreshFrame.Checked)
			{
				labelScanline.Enabled = false;
				hScrollBarScanline.Enabled = false;
				buttonRefresh.Enabled = false;
				_cbScanline = -1;
			}
			else if (radioButtonRefreshScanline.Checked)
			{
				labelScanline.Enabled = true;
				hScrollBarScanline.Enabled = true;
				buttonRefresh.Enabled = false;
				_cbScanline = (hScrollBarScanline.Value + 145) % 154;
			}
			else if (radioButtonRefreshManual.Checked)
			{
				labelScanline.Enabled = false;
				hScrollBarScanline.Enabled = false;
				buttonRefresh.Enabled = true;
				_cbScanline = -2;
			}
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			if (_cbScanline == -2)
			{
				Gb.SetScanlineCallback(ScanlineCallback, -2);
			}
		}

		private void hScrollBarScanline_ValueChanged(object sender, EventArgs e)
		{
			labelScanline.Text = ((hScrollBarScanline.Value + 145) % 154).ToString();
			_cbScanline = (hScrollBarScanline.Value + 145) % 154;
		}

		// 0..153: scanline number. -1: frame.  -2: manual
		private int _cbScanline;

		// what was last passed to the emu core
		private int _cbScanlineEmu = -4; // force refresh

		protected override void UpdateBefore()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}
			if (!Visible)
			{
				if (_cbScanlineEmu is -2) return;
				_cbScanlineEmu = -2;
				Gb.SetScanlineCallback(null, 0);
				return;
			}
			if (_cbScanline == _cbScanlineEmu) return;
			_cbScanlineEmu = _cbScanline;
			if (_cbScanline is -2) Gb.SetScanlineCallback(null, 0);
			else Gb.SetScanlineCallback(ScanlineCallback, _cbScanline);
		}

		private string _freezeLabel;
		private Bitmap _freezeBmp;
		private string _freezeDetails;

		private void SaveDetails()
		{
			_freezeLabel = groupBoxDetails.Text;
			_freezeBmp?.Dispose();
			_freezeBmp = (Bitmap)bmpViewDetails.Bmp.Clone();
			_freezeDetails = labelDetails.Text;
		}

		private void LoadDetails()
		{
			groupBoxDetails.Text = _freezeLabel;
			bmpViewDetails.Height = _freezeBmp.Height * 8;
			bmpViewDetails.ChangeBitmapSize(_freezeBmp.Size);
			using (var g = Graphics.FromImage(bmpViewDetails.Bmp))
				g.DrawImageUnscaled(_freezeBmp, 0, 0);
			labelDetails.Text = _freezeDetails;
			bmpViewDetails.Refresh();
		}

		private void SetFreeze()
		{
			groupBoxMemory.Text = groupBoxDetails.Text;
			bmpViewMemory.Size = bmpViewDetails.Size;
			bmpViewMemory.ChangeBitmapSize(bmpViewDetails.Bmp.Size);
			using (var g = Graphics.FromImage(bmpViewMemory.Bmp))
				g.DrawImageUnscaled(bmpViewDetails.Bmp, 0, 0);
			labelMemory.Text = labelDetails.Text;
			bmpViewMemory.Refresh();
		}

		private unsafe void PaletteMouseover(int x, int y, bool sprite)
		{
			using (var memory = Gb.LockGPU())
			{
				var bgPal = memory.Bgpal;
				var spPal = memory.Sppal;

				bmpViewDetails.ChangeBitmapSize(8, 10);
				if (bmpViewDetails.Height != 80)
					bmpViewDetails.Height = 80;
				var sb = new StringBuilder();
				x /= 16;
				y /= 16;
				int* pal = (int*)(sprite ? spPal : bgPal) + x * 4;
				int color = pal[y];

				sb.AppendLine($"Palette {x}");
				sb.AppendLine($"Color {y}");
				sb.AppendLine($"(R,G,B) = ({color >> 16 & 255},{color >> 8 & 255},{color & 255})");

				var lockData = bmpViewDetails.Bmp.LockBits(new Rectangle(0, 0, 8, 10), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				int* dest = (int*)lockData.Scan0;
				int pitch = lockData.Stride / sizeof(int);

				for (int py = 0; py < 10; py++)
				{
					for (int px = 0; px < 8; px++)
					{
						if (py < 8)
						{
							*dest++ = color;
						}
						else
						{
							*dest++ = pal[px / 2];
						}
					}
					dest -= 8;
					dest += pitch;
				}
				bmpViewDetails.Bmp.UnlockBits(lockData);
				labelDetails.Text = sb.ToString();
				bmpViewDetails.Refresh();
			}
		}

		private unsafe void TileMouseover(int x, int y, bool secondBank)
		{
			using (var memory = Gb.LockGPU())
			{
				var vram = memory.Vram;
				var tilesPal = ComputeTilesPalFromMemory(memory);

				// todo: draw with a specific palette
				bmpViewDetails.ChangeBitmapSize(8, 8);
				if (bmpViewDetails.Height != 64)
					bmpViewDetails.Height = 64;
				var sb = new StringBuilder();
				x /= 8;
				y /= 8;
				int tileIndex = y * 16 + x;
				int tileOffset = tileIndex * 16;
				sb.AppendLine(_cgb
					? $"Tile #{tileIndex} @{(secondBank ? 1 : 0)}:{tileOffset + 0x8000:x4}"
					: $"Tile #{tileIndex} @{tileOffset + 0x8000:x4}");

				var lockData = bmpViewDetails.Bmp.LockBits(new Rectangle(0, 0, 8, 8), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				DrawTile((byte*)vram + tileOffset + (secondBank ? 8192 : 0), (int*)lockData.Scan0, lockData.Stride / sizeof(int), (int*)tilesPal);
				bmpViewDetails.Bmp.UnlockBits(lockData);
				labelDetails.Text = sb.ToString();
				bmpViewDetails.Refresh();
			}
		}

		private unsafe void TileMapMouseover(int x, int y, bool win)
		{
			using (var memory = Gb.LockGPU())
			{
				var _bgpal = memory.Bgpal;
				var _vram = memory.Vram;

				bmpViewDetails.ChangeBitmapSize(8, 8);
				if (bmpViewDetails.Height != 64)
					bmpViewDetails.Height = 64;
				var sb = new StringBuilder();
				bool secondMap = win ? _lcdc.Bit(6) : _lcdc.Bit(3);
				int mapOffset = secondMap ? 0x1c00 : 0x1800;
				x /= 8;
				y /= 8;
				mapOffset += y * 32 + x;
				byte* mapBase = (byte*)_vram + mapOffset;
				int tileIndex = mapBase[0];
				if (win || !_lcdc.Bit(4)) // 0x9000 base
					if (tileIndex < 128)
						tileIndex += 256; // compute all if from 0x8000 base
				int tileOffset = tileIndex * 16;
				var lockData = bmpViewDetails.Bmp.LockBits(new Rectangle(0, 0, 8, 8), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				if (!_cgb)
				{
					sb.AppendLine($"{(win ? "Win" : "BG")} Map ({x},{y}) @{mapOffset + 0x8000:x4}");
					sb.AppendLine($"  Tile #{tileIndex} @{tileOffset + 0x8000:x4}");
					DrawTile((byte*)_vram + tileOffset, (int*)lockData.Scan0, lockData.Stride / sizeof(int), (int*)_bgpal);
				}
				else
				{
					int tileExt = mapBase[8192];

					sb.AppendLine($"{(win ? "Win" : "BG")} Map ({x},{y}) @{mapOffset + 0x8000:x4}");
					sb.AppendLine($"  Tile #{tileIndex} @{(tileExt.Bit(3) ? 1 : 0)}:{tileOffset + 0x8000:x4}");
					sb.AppendLine($"  Palette {tileExt & 7}");
					sb.AppendLine($"  Flags {(tileExt.Bit(5) ? 'H' : ' ')}{(tileExt.Bit(6) ? 'V' : ' ')}{(tileExt.Bit(7) ? 'P' : ' ')}");
					DrawTileHv((byte*)_vram + tileOffset + (tileExt.Bit(3) ? 8192 : 0), (int*)lockData.Scan0, lockData.Stride / sizeof(int), (int*)_bgpal + 4 * (tileExt & 7), tileExt.Bit(5), tileExt.Bit(6));
				}
				bmpViewDetails.Bmp.UnlockBits(lockData);
				labelDetails.Text = sb.ToString();
				bmpViewDetails.Refresh();
			}
		}

		private unsafe void SpriteMouseover(int x, int y)
		{
			using (var memory = Gb.LockGPU())
			{
				var spPal = memory.Sppal;
				var oam = memory.Oam;
				var vram = memory.Vram;

				bool tall = _lcdc.Bit(2);
				x /= 8;
				y /= 8;
				bmpViewDetails.ChangeBitmapSize(8, tall ? 16 : 8);
				if (bmpViewDetails.Height != bmpViewDetails.Bmp.Height * 8)
					bmpViewDetails.Height = bmpViewDetails.Bmp.Height * 8;
				var sb = new StringBuilder();

				byte* oament = (byte*)oam + 4 * x;
				int sy = oament[0];
				int sx = oament[1];
				int tileNum = oament[2];
				int flags = oament[3];
				bool hFlip = flags.Bit(5);
				bool vFlip = flags.Bit(6);
				if (tall)
				{
					tileNum = vFlip ? tileNum | 1 : tileNum & ~1;
				}

				int tileOffset = tileNum * 16;
				sb.AppendLine($"Sprite #{x} @{4 * x + 0xfe00:x4}");
				sb.AppendLine($"  (x,y) = ({sx},{sy})");
				var lockData = bmpViewDetails.Bmp.LockBits(new Rectangle(0, 0, 8, tall ? 16 : 8), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				if (_cgb)
				{
					sb.AppendLine($"  Tile #{(y == 1 ? tileNum ^ 1 : tileNum)} @{(flags.Bit(3) ? 1 : 0)}:{tileOffset + 0x8000:x4}");
					sb.AppendLine($"  Palette {flags & 7}");
					DrawTileHv((byte*)vram + tileOffset + (flags.Bit(3) ? 8192 : 0), (int*)lockData.Scan0, lockData.Stride / sizeof(int), (int*)spPal + 4 * (flags & 7), hFlip, vFlip);
					if (tall)
						DrawTileHv((byte*)vram + (tileOffset ^ 16) + (flags.Bit(3) ? 8192 : 0), (int*)(lockData.Scan0 + lockData.Stride * 8), lockData.Stride / sizeof(int), (int*)spPal + 4 * (flags & 7), hFlip, vFlip);
				}
				else
				{
					sb.AppendLine($"  Tile #{(y == 1 ? tileNum ^ 1 : tileNum)} @{tileOffset + 0x8000:x4}");
					sb.AppendLine($"  Palette {(flags.Bit(4) ? 1 : 0)}");
					DrawTileHv((byte*)vram + tileOffset, (int*)lockData.Scan0, lockData.Stride / sizeof(int), (int*)spPal + (flags.Bit(4) ? 4 : 0), hFlip, vFlip);
					if (tall)
						DrawTileHv((byte*)vram + (tileOffset ^ 16), (int*)(lockData.Scan0 + lockData.Stride * 8), lockData.Stride / sizeof(int), (int*)spPal + 4 * (flags.Bit(4) ? 4 : 0), hFlip, vFlip);
				}
				sb.AppendLine($"  Flags {(hFlip ? 'H' : ' ')}{(vFlip ? 'V' : ' ')}{(flags.Bit(7) ? 'P' : ' ')}");
				bmpViewDetails.Bmp.UnlockBits(lockData);
				labelDetails.Text = sb.ToString();
				bmpViewDetails.Refresh();
			}
		}

		private void bmpViewBG_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Background";
		}

		private void bmpViewBG_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewBG_MouseMove(object sender, MouseEventArgs e)
		{
			TileMapMouseover(e.X, e.Y, false);
		}

		private void bmpViewWin_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Window";
		}

		private void bmpViewWin_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewWin_MouseMove(object sender, MouseEventArgs e)
		{
			TileMapMouseover(e.X, e.Y, true);
		}

		private void bmpViewTiles1_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Tiles";
		}

		private void bmpViewTiles1_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewTiles1_MouseMove(object sender, MouseEventArgs e)
		{
			TileMouseover(e.X, e.Y, false);
		}

		private void bmpViewTiles2_MouseEnter(object sender, EventArgs e)
		{
			if (!_cgb)
				return;
			SaveDetails();
			groupBoxDetails.Text = "Details - Tiles";
		}

		private void bmpViewTiles2_MouseLeave(object sender, EventArgs e)
		{
			if (!_cgb)
				return;
			LoadDetails();
		}

		private void bmpViewTiles2_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_cgb)
				return;
			TileMouseover(e.X, e.Y, true);
		}

		private void bmpViewBGPal_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Palette";
		}

		private void bmpViewBGPal_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewBGPal_MouseMove(object sender, MouseEventArgs e)
		{
			PaletteMouseover(e.X, e.Y, false);
		}

		private void bmpViewSPPal_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Palette";
		}

		private void bmpViewSPPal_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewSPPal_MouseMove(object sender, MouseEventArgs e)
		{
			PaletteMouseover(e.X, e.Y, true);
		}

		private void bmpViewOAM_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Sprite";
		}

		private void bmpViewOAM_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewOAM_MouseMove(object sender, MouseEventArgs e)
		{
			SpriteMouseover(e.X, e.Y);
		}

		private void bmpViewOBJ_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Objects";
		}

		private void bmpViewOBJ_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewOBJ_MouseMove(object sender, MouseEventArgs e)
		{
			// TODO: pick out sprites from the object window based on their position
		}

		private void bmpView_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				SetFreeze();
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (sender == bmpViewBGPal)
				{
					_tilesPalIsSprite = false;
					_tilesPalOffset = e.X / 16 * 16;
				}
				else if (sender == bmpViewSPPal)
				{
					_tilesPalIsSprite = true;
					_tilesPalOffset =  e.X / 16 * 16;
				}
			}
		}

		private readonly Timer _messageTimer = new Timer();

		private void GbGpuView_KeyDown(object sender, KeyEventArgs e)
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
					labelClipboard.Text = $"{bv.Text} copied to clipboard.";
					_messageTimer.Stop();
					_messageTimer.Start();
				}
			}
		}

		private void MessageTimer_Tick(object sender, EventArgs e)
		{
			_messageTimer.Stop();
			labelClipboard.Text = "CTRL+C copies the pane under the mouse.";
		}

		private void ButtonChangeColor_Click(object sender, EventArgs e)
		{
			using var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				FullOpen = true,
				Color = Spriteback
			};

			if (this.ShowDialogWithTempMute(dlg).IsOk())
			{
				Spriteback = dlg.Color;
			}
		}
	}
}
