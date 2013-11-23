using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace BizHawk.Client.EmuHawk
{
	public class PCEBGCanvas : Control
	{
		public Bitmap Bat;

		private const int BAT_WIDTH = 1024;
		private const int BAT_HEIGHT = 512;

		public PCEBGCanvas()
		{
			Bat = new Bitmap(BAT_WIDTH, BAT_HEIGHT, PixelFormat.Format32bppArgb);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			Size = new Size(BAT_WIDTH, BAT_HEIGHT);
			Paint += BGViewer_Paint;
		}

		private void BGViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImageUnscaled(Bat, 0, 0);
		}
	}
}
