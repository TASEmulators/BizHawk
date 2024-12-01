using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrinterView : ToolFormBase, IToolFormAutoConfig
	{
		private const int PaperWidth = 160;

		// the bg color
		private static readonly uint PaperColor = (uint)Color.AntiqueWhite.ToArgb();

		public static Icon ToolIcon
			=> Properties.Resources.GambatteIcon;

		private readonly ColorMatrix _paperAdjustment;

		[RequiredService]
		public IGameboyCommon/*?*/ _gbCore { get; set; }

		private IGameboyCommon Gb
			=> _gbCore!;

		[RequiredService]
		public IEmulator Emulator { get; set; }

		// If we've connected the printer yet
		private bool _connected;

		// the entire bitmap
		private Bitmap _printerHistory;

		protected override string WindowTitleStatic => "Printer Viewer";

		public GBPrinterView()
		{
			InitializeComponent();
			Icon = ToolIcon;

			// adjust the color of the printed output to be more papery
			_paperAdjustment = new ColorMatrix
			{
				Matrix00 = (0xFA - 0x10) / 255F,
				Matrix40 = 0x10 / 255F,
				Matrix11 = (0xEB - 0x10) / 255F,
				Matrix41 = 0x10 / 255F,
				Matrix22 = (0xD7 - 0x18) / 255F,
				Matrix42 = 0x18 / 255F
			};

			paperView.ChangeBitmapSize(PaperWidth, PaperWidth);

			ClearPaper();
		}

		private void GBPrinterView_FormClosed(object sender, FormClosedEventArgs e)
			=> Gb.SetPrinterCallback(null);

		public override void Restart()
		{
			// Really, there's not necessarily a reason to clear it at all,
			// since the paper would still be there,
			// but it just seems right to get a blank slate on reset.
			ClearPaper();
			_connected = false;
		}

		protected override void UpdateAfter()
		{
			// Automatically connect once the game is running
			if (!_connected)
			{
				Gb.SetPrinterCallback(OnPrint);
				_connected = true;
			}
		}

		// The printer callback that . See PrinterCallback for details.
		private void OnPrint(IntPtr image, byte height, byte topMargin, byte bottomMargin, byte exposure)
		{
			// In this implementation:
			//   the bottom margin and top margin are just white lines at the top and bottom
			//   exposure is ignored

			// The page received image
			var page = new Bitmap(PaperWidth, height);

			var bmp = page.LockBits(new Rectangle(0, 0, PaperWidth, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < PaperWidth; x++)
				{
					uint pixel;
					unsafe
					{
						// Pixel of the image; it's just sent from the core as a big bitmap that's 160 x height
						pixel = *(uint*)(image + (x + y * PaperWidth) * sizeof(uint));
					}

					SetPixel(bmp, x, y, pixel);
				}
			}

			page.UnlockBits(bmp);

			// add it to the bottom of the history
			int oldHeight = _printerHistory.Height;
			ResizeHistory(_printerHistory.Height + page.Height + topMargin + bottomMargin);
			using (var g = Graphics.FromImage(_printerHistory))
			{
				// Make it brown
				var a = new ImageAttributes();
				a.SetColorMatrix(_paperAdjustment);

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
		private unsafe void SetPixel(BitmapData bmp, int x, int y, uint c)
		{
			uint* pixel = (uint*)(bmp.Scan0 + x * 4 + y * bmp.Stride);
			*pixel = c;
		}

		private void ClearPaper()
		{
			ResizeHistory(8);
			RefreshView();
		}

		private void ResizeHistory(int height)
		{
			// copy to a new image of height
			var newHistory = new Bitmap(PaperWidth, height);
			using (var g = Graphics.FromImage(newHistory))
			{
				g.Clear(Color.FromArgb((int)PaperColor));
				if (_printerHistory != null)
				{
					g.DrawImage(_printerHistory, Point.Empty);
				}

				g.Flush();
			}

			_printerHistory?.Dispose();
			_printerHistory = newHistory;

			// Update scrollbar, viewport is a square
			paperScroll.Maximum = Math.Max(0, height);
		}

		private void RefreshView()
		{
			using (var g = Graphics.FromImage(paperView.Bmp))
			{
				g.Clear(Color.FromArgb((int)PaperColor));
				g.DrawImage(_printerHistory, new Point(0, -paperScroll.Value));
				g.Flush();
			}

			paperView.Refresh();
		}

		private void SaveImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// slight hack to use the nice SaveFile() feature of a BmpView

			BmpView toSave = new BmpView();
			toSave.ChangeBitmapSize(_printerHistory.Size);
			using (var g = Graphics.FromImage(toSave.Bmp))
			{
				g.DrawImage(_printerHistory, Point.Empty);
				g.Flush();
			}

			toSave.Bmp.SaveAsFile(Game, "Print", Emulator.SystemId, Config.PathEntries, this);
		}

		private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Clipboard.SetImage(_printerHistory);
		}

		private void PaperScroll_ValueChanged(object sender, EventArgs e)
		{
			RefreshView();
		}
	}
}
