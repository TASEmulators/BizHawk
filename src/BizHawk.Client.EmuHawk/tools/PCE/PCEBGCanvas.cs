using System.Drawing;
using System.Windows.Forms;

using BizHawk.Bizware.Graphics;

namespace BizHawk.Client.EmuHawk
{
	public class PceBgCanvas : Control
	{
		public Bitmap Bat { get; set; }

		private const int BAT_WIDTH = 1024;
		private const int BAT_HEIGHT = 512;

		public PceBgCanvas()
		{
			Size = new(BAT_WIDTH, BAT_HEIGHT);
			Bat = BitmapBuffer.CreateBitmapObject(Size);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			Paint += BGViewer_Paint;
		}

		private void BGViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImageUnscaled(Bat, 0, 0);
		}
	}
}
