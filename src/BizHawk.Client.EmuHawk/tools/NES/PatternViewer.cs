using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public sealed class PatternViewer : Control
	{
		public Bitmap Pattern { get; set; }
		public int Pal0 { get; set; } = 0; // 0-7 Palette choice
		public int Pal1 { get; set; } = 0;

		public PatternViewer()
		{
			Size pSize = new(256, 128);
			Pattern = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += PatternViewer_Paint;
		}

		private void PatternViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			e.Graphics.DrawImageUnscaled(Pattern, 0, 0);
		}

		public void ScreenshotToClipboard()
		{
			Bitmap b = new(Width, Height);
			Rectangle rect = new(new Point(0, 0), Size);
			DrawToBitmap(b, rect);

			using var img = b;
			Clipboard.SetImage(img);
		}
	}
}
