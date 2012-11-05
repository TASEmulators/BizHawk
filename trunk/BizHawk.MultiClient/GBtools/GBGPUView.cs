using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.GBtools
{
	public partial class GBGPUView : Form
	{
		Emulation.Consoles.GB.Gameboy gb;

		public GBGPUView()
		{
			InitializeComponent();

		}

		public void Restart()
		{
			if (Global.Emulator is Emulation.Consoles.GB.Gameboy)
			{
				gb = Global.Emulator as Emulation.Consoles.GB.Gameboy;
			}
			else
			{
				this.Close();
			}
		}

		/// <summary>
		/// put me in ToolsBefore
		/// </summary>
		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return;
			if (gb != null)
				if (this.Visible)
					gb.SetScanlineCallback(ScanlineCallback, 0);
				else
					gb.SetScanlineCallback(null, 0);
		}

		static unsafe void DrawTile(byte* tile, int* dest, int pitch)
		{
			for (int y = 0; y < 8; y++)
			{
				int loplane = *tile++;
				int hiplane = *tile++;
				hiplane <<= 1; // msb
				dest += 7;
				for (int x = 0; x < 8; x++) // right to left
				{
					int palcolor = loplane & 1 | hiplane & 2;
					// todo: palette transformation
					int color = palcolor * 0x555555 | unchecked((int)0xff000000);
					*dest-- = color;
					loplane >>= 1;
					hiplane >>= 1;
				}
				dest++;
				dest += pitch;
			}
		}

		static unsafe void DrawBG(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap)
		{
			if (b.Width != 256 || b.Height != 256)
				throw new Exception("GPUView screwed up.");

			var lockdata = b.LockBits(new Rectangle(0, 0, 256, 256), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte* map = (byte*)_map;

			int* dest = (int*)lockdata.Scan0;

			int pitch = lockdata.Stride / sizeof(int); // in int*s, not bytes
			
			for (int ty = 0; ty < 32; ty++)
			{			
				for (int tx = 0; tx < 32; tx++)
				{
					int tileindex = map[0];
					if (wrap && tileindex >= 128)
						tileindex -= 256;
					byte* tile = (byte*)(_tiles + tileindex * 16);
					DrawTile(tile, dest, pitch);
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		/// <summary>
		/// core calls this on scanlines
		/// </summary>
		/// <param name="vram"></param>
		/// <param name="vramlength"></param>
		/// <param name="lcdc"></param>
		void ScanlineCallback(IntPtr vram, int vramlength, int lcdc)
		{

			DrawBG(
				bmpViewBG.bmp,
				vram + (lcdc.Bit(3) ? 0x1c00 : 0x1800),
				vram + (lcdc.Bit(4) ? 0x0000 : 0x1000),
				!lcdc.Bit(4));
			bmpViewBG.Refresh();

			DrawBG(
				bmpViewWin.bmp,
				vram + (lcdc.Bit(6) ? 0x1c00 : 0x1800),
				vram + 0x1000, // force win to second tile bank???
				true);
			bmpViewWin.Refresh();

		}

		private void GBGPUView_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (gb != null)
			{
				gb.SetScanlineCallback(null, 0);
				gb = null;
			}
		}

		private void GBGPUView_Load(object sender, EventArgs e)
		{
			gb = Global.Emulator as Emulation.Consoles.GB.Gameboy;
		}
	}
}
