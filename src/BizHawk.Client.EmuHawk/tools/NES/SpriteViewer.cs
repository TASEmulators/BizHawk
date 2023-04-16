using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SpriteViewer : Control
	{
		public Bitmap Sprites { get; set; }

		public SpriteViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			var pSize = new Size(256, 96);
			Sprites = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += SpriteViewer_Paint;
		}

		private void Display(Graphics g)
		{
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			g.DrawImageUnscaled(Sprites, 1, 1);
		}

		private void SpriteViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}

		public void ScreenshotToClipboard()
		{
			var b = new Bitmap(Width, Height);
			var rect = new Rectangle(new Point(0, 0), Size);
			DrawToBitmap(b, rect);

			using var img = b;
			Clipboard.SetImage(img);
		}
	}
}
