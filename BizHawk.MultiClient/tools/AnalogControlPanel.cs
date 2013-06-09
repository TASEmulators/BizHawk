using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public sealed class AnalogControlPanel : Panel
	{
		public int X = 0;
		public int Y = 0;

		private readonly Brush WhiteBrush = Brushes.White;
		private readonly Brush BlackBrush = Brushes.Black;
		private readonly Brush GrayBrush = Brushes.LightGray;
		private readonly Brush RedBrush = Brushes.Red;
		private readonly Brush BlueBrush = Brushes.DarkBlue;
		private readonly Pen _white_pen;
		private readonly Pen BlackPen;
		private readonly Pen GrayPen;
		private readonly Pen RedPen;
		private readonly Pen BluePen;

		private Bitmap dot = new Bitmap(7, 7);

		public AnalogControlPanel()
		{
			Size = new Size(129, 129);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			MouseClick += Event_MouseClick;
			MouseMove += Event_MouseMove;
			_white_pen = new Pen(WhiteBrush);
			BlackPen = new Pen(BlackBrush);
			GrayPen = new Pen(GrayBrush);
			RedPen = new Pen(RedBrush);
			BluePen = new Pen(BlueBrush);
			BorderStyle = BorderStyle.Fixed3D;
			
			// Draw the dot into a bitmap
			Graphics g = Graphics.FromImage(dot);
			g.Clear(Color.Transparent);
			g.FillRectangle(RedBrush, 2, 0, 3, 7);
			g.FillRectangle(RedBrush, 1, 1, 5, 5);
			g.FillRectangle(RedBrush, 0, 2, 7, 3);
		}

		private int RealToGFX(int val)
		{
			return (val + 128)/2;
		}

		private int GFXToReal(int val)
		{
			int ret = (val * 2);
			if (ret > 127) ret = 127;
			if (ret < -128) ret = -128;
			return ret;
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				//Background
				e.Graphics.FillRectangle(GrayBrush, 0, 0, 128, 128);
				e.Graphics.FillEllipse(WhiteBrush, 0, 0, 127, 127);
				e.Graphics.DrawEllipse(BlackPen, 0, 0, 127, 127);
				e.Graphics.DrawLine(BlackPen, 64, 0, 64, 127);
				e.Graphics.DrawLine(BlackPen, 0, 63, 127, 63);

				//Line
				e.Graphics.DrawLine(BluePen, 64, 63, RealToGFX(X), 127 - RealToGFX(Y));
				e.Graphics.DrawImage(dot, RealToGFX(X) - 3, 127 - RealToGFX(Y) - 3);

			}
		}

		private void Event_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X - 64);
				Y = GFXToReal(-(e.Y - 63));
			}

			Refresh();
		}

		private void Event_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X - 64);
				Y = GFXToReal(-(e.Y - 63));
			}

			Refresh();
		}

		public void SetPosition(int Xval, int Yval)
		{
			X = Xval;
			Y = Yval;
			
			Refresh();
		}
	}
}
