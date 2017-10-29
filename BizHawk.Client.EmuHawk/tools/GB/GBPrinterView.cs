using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrinterView : Form, IToolFormAutoConfig
	{
		const int PaperWidth = 160;

		// the bg color
		private static readonly uint PaperColor = (uint)Color.AntiqueWhite.ToArgb();

		private ColorMatrix PaperAdjustment;

		[RequiredService]
		public IGameboyCommon Gb { get; private set; }

		// If we've connected the printer yet
		bool connected = false;

		// the entire bitmap
		Bitmap printerHistory;

		public GBPrinterView()
		{
			InitializeComponent();

			// adjust the color of the printed output to be more papery
			PaperAdjustment = new ColorMatrix();
			PaperAdjustment.Matrix00 = (0xFA - 0x10) / 255F;
			PaperAdjustment.Matrix40 = 0x10 / 255F;
			PaperAdjustment.Matrix11 = (0xEB - 0x10) / 255F;
			PaperAdjustment.Matrix41 = 0x10 / 255F;
			PaperAdjustment.Matrix22 = (0xD7 - 0x18) / 255F;
			PaperAdjustment.Matrix42 = 0x18 / 255F;

			paperView.ChangeBitmapSize(PaperWidth, PaperWidth);

			ClearPaper();
		}

		private void GBPrinterView_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (Gb != null)
			{
				Gb.SetPrinterCallback(null);
			}
		}

		public bool UpdateBefore => false;

		public bool AskSaveChanges() => true;

		public void FastUpdate()
		{
		}

		public void NewUpdate(ToolFormUpdateType type)
		{
		}

		public void Restart()
		{
			// Really, there's not necessarilly a reason to clear it at all,
			// since the paper would still be there,
			// but it just seems right to get a blank slate on reset.
			ClearPaper();

			connected = false;
		}

		public void UpdateValues()
		{
			// Automatically connect once the game is running
			if (!connected)
			{
				Gb.SetPrinterCallback(OnPrint);
				connected = true;
			}
		}

		/// <summary>
		/// The printer callback that . See PrinterCallback for details.
		/// </summary>
		void OnPrint(IntPtr image, byte height, byte topMargin, byte bottomMargin, byte exposure)
		{
			// In this implementation:
			//   the bottom margin and top margin are just white lines at the top and bottom
			//   exposure is ignored

			// The page received image
			Bitmap page = new Bitmap(PaperWidth, height);

			var bmp = page.LockBits(new Rectangle(0, 0, PaperWidth, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < PaperWidth; x++)
				{
					uint pixel;
					unsafe
					{
						// Pixel of the image; it's just sent from the core as a big bitmap that's 160xheight
						pixel = *(uint*)(image + (x + y * PaperWidth) * sizeof(uint));
					}

					SetPixel(bmp, x, y, pixel);
				}
			}

			page.UnlockBits(bmp);

			// add it to the bottom of the history
			int oldHeight = printerHistory.Height;
			ResizeHistory(printerHistory.Height + page.Height + topMargin + bottomMargin);
			using (var g = Graphics.FromImage(printerHistory))
			{
				// Make it brown
				ImageAttributes a = new ImageAttributes();
				a.SetColorMatrix(PaperAdjustment);

				g.DrawImage(page, new Rectangle(0, oldHeight + topMargin, page.Width, page.Height), 0F, 0F, page.Width, page.Height, GraphicsUnit.Pixel, a);
				g.Flush();
			}
			RefreshView();
		}

		/// <summary>
		/// Set a 2x pixel
		/// </summary>
		/// <param name="bmp">The bitmap data to draw to</param>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="c">The ARGB color to set that pixel to</param>
		unsafe void SetPixel(BitmapData bmp, int x, int y, uint c)
		{
			uint* pixel = (uint*)(bmp.Scan0 + x * 4 + y * bmp.Stride);
			*pixel = c;
		}

		void ClearPaper()
		{
			ResizeHistory(8);
			RefreshView();
		}

		void ResizeHistory(int height)
		{
			// copy to a new image of height
			var newHistory = new Bitmap(PaperWidth, height);
			using (var g = Graphics.FromImage(newHistory))
			{
				g.Clear(Color.FromArgb((int)PaperColor));
				if (printerHistory != null)
					g.DrawImage(printerHistory, Point.Empty);
				g.Flush();
			}

			if (printerHistory != null)
				printerHistory.Dispose();
			printerHistory = newHistory;

			// Update scrollbar, viewport is a square
			paperScroll.Maximum = Math.Max(0, height);
		}

		void RefreshView()
		{
			using (Graphics g = Graphics.FromImage(paperView.BMP))
			{
				g.Clear(Color.FromArgb((int)PaperColor));
				g.DrawImage(printerHistory, new Point(0, -paperScroll.Value));
				g.Flush();
			}

			paperView.Refresh();
		}

		private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// slight hack to use the nice SaveFile() feature of a BmpView

			BmpView toSave = new BmpView();
			toSave.ChangeBitmapSize(printerHistory.Size);
			using (var g = Graphics.FromImage(toSave.BMP))
			{
				g.DrawImage(printerHistory, Point.Empty);
				g.Flush();
			}
			toSave.SaveFile();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Clipboard.SetImage(printerHistory);
		}

		private void PaperScroll_ValueChanged(object sender, System.EventArgs e)
		{
			RefreshView();
		}
	}
}
