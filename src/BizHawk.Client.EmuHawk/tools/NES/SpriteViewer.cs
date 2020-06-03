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
			g.DrawImage(Sprites, 1, 1);
		}

		private void SpriteViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}
	}
}
