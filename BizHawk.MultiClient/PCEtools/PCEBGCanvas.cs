using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public partial class PCEBGCanvas : Control
	{
		public Bitmap bat;

		private const int BAT_WIDTH = 1024;
		private const int BAT_HEIGHT = 512;		

		public PCEBGCanvas()
		{
			bat = new Bitmap(BAT_WIDTH, BAT_HEIGHT, PixelFormat.Format32bppArgb);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			//SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			Size = new Size(BAT_WIDTH, BAT_HEIGHT);
			//this.BackColor = Color.Transparent;
			Paint += BGViewer_Paint;
		}

		private void BGViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImageUnscaled(bat, 0, 0);
		}
	}
}
