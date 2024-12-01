using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("VDP Viewer")]
	public partial class SmsVdpViewer : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.SmsIcon;

		[RequiredService]
		private ISmsGpuView Vdp { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private int _palIndex;

		protected override string WindowTitleStatic => "VDP Viewer";

		public SmsVdpViewer()
		{
			InitializeComponent();
			Icon = ToolIcon;

			bmpViewTiles.ChangeBitmapSize(256, 128);
			bmpViewPalette.ChangeBitmapSize(16, 2);
			bmpViewBG.ChangeBitmapSize(256, 256);
		}

		protected override void GeneralUpdate() => UpdateBefore();

		private static unsafe void Draw8x8(byte* src, int* dest, int pitch, int* pal)
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

		private static unsafe void Draw8x8hv(byte* src, int* dest, int pitch, int* pal, bool hflip, bool vflip)
		{
			int incX = hflip ? -1 : 1;
			int incY = vflip ? -pitch : pitch;
			if (hflip)
				dest -= incX * 7;
			if (vflip)
				dest -= incY * 7;
			incY -= incX * 8;
			for (int j = 0; j < 8; j++)
			{
				for (int i = 0; i < 8; i++)
				{
					*dest = pal[*src++];
					dest += incX;
				}
				dest += incY;
			}
		}

		private unsafe void DrawTiles(int *pal)
		{
			var lockData = bmpViewTiles.Bmp.LockBits(new Rectangle(0, 0, 256, 128), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			fixed (byte* src = Vdp.PatternBuffer)
			{
				for (int tile = 0; tile < 512; tile++)
				{
					int srcAddr = tile * 64;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8(src + srcAddr, dest + destAddr, pitch, pal);
				}
			}
			bmpViewTiles.Bmp.UnlockBits(lockData);
			bmpViewTiles.Refresh();
		}

		private unsafe void DrawBG(int* pal)
		{
			int bgHeight = Vdp.FrameHeight == 192 ? 224 : 256;
			int maxTile = bgHeight * 4;
			if (bgHeight != bmpViewBG.Bmp.Height)
			{
				bmpViewBG.Height = bgHeight;
				bmpViewBG.ChangeBitmapSize(256, bgHeight);
			}

			var lockData = bmpViewBG.Bmp.LockBits(new Rectangle(0, 0, 256, bgHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			fixed (byte* src = Vdp.PatternBuffer)
			fixed (byte* vram = Vdp.VRAM)
			{
				short* map = (short*)(vram + Vdp.CalcNameTableBase());

				for (int tile = 0; tile < maxTile; tile++)
				{
					short bgent = *map++;
					bool hFlip = (bgent & 1 << 9) != 0;
					bool vFlip = (bgent & 1 << 10) != 0;
					int* tpal = pal + ((bgent & 1 << 11) >> 7);
					int srcAddr = (bgent & 511) * 64;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8hv(src + srcAddr, dest + destAddr, pitch, tpal, hFlip, vFlip);
				}
			}
			bmpViewBG.Bmp.UnlockBits(lockData);
			bmpViewBG.Refresh();
		}

		private unsafe void DrawPal(int* pal)
		{
			var lockData = bmpViewPalette.Bmp.LockBits(new Rectangle(0, 0, 16, 2), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			for (int j = 0; j < 2; j++)
			{
				for (int i = 0; i < 16; i++)
				{
					*dest++ = *pal++;
				}

				dest -= 16;
				dest += pitch;
			}
			bmpViewPalette.Bmp.UnlockBits(lockData);
			bmpViewPalette.Refresh();
		}

		protected override void UpdateBefore()
		{
			unsafe
			{
				fixed (int* pal = Vdp.Palette)
				{
					DrawTiles(pal + _palIndex * 16);
					DrawBG(pal);
					DrawPal(pal);
				}
			}
		}

		public override void Restart()
		{
			GeneralUpdate();
		}

		private void BmpViewPalette_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 1);
			_palIndex = p;
			unsafe
			{
				fixed (int* pal = Vdp.Palette)
				{
					DrawTiles(pal + _palIndex * 16);
				}
			}
		}

		private void VDPViewer_KeyDown(object sender, KeyEventArgs e)
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

		private void SaveAsFile(Bitmap bmp, string suffix)
		{
			bmp.SaveAsFile(Game, suffix, Emulator.SystemId, Config.PathEntries, this);
		}

		private void SaveTilesScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsFile(bmpViewTiles.Bmp, "Tiles");
		}

		private void SavePalettesScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsFile(bmpViewPalette.Bmp, "Palette");
		}

		private void SaveBgScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsFile(bmpViewBG.Bmp, "BG");
		}
	}
}
