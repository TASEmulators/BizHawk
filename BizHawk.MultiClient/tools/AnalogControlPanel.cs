using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public sealed class AnalogControlPanel : Panel
	{
		public int X = 1;
		public int Y = 1;

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

		private const int DOTW = 8;

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
		}

		private int RealToGFX(int val)
		{
			return (val + 128)/2;
		}

		private int GFXToReal(int val)
		{
			return (val * 2) - 128;
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				//Background
				e.Graphics.FillRectangle(GrayBrush, 0, 0, 128, 128);
				e.Graphics.FillEllipse(WhiteBrush, 0, 0, 127, 127);
				e.Graphics.DrawEllipse(BlackPen, 0, 0, 127, 127);
				e.Graphics.DrawLine(BlackPen, 65, 0, 65, 127);
				e.Graphics.DrawLine(BlackPen, 0, 65, 127, 65);

				//Line
				e.Graphics.DrawLine(BluePen, 64, 64, RealToGFX(X), RealToGFX(Y));
				e.Graphics.FillEllipse(RedBrush, RealToGFX(X) - (DOTW / 2), RealToGFX(Y) - (DOTW / 2), DOTW, DOTW);
			}
		}

		private void Event_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X);
				Y = GFXToReal(e.Y);
			}

			Refresh();
		}

		private void Event_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X);
				Y = GFXToReal(e.Y);
			}

			Refresh();
		}
	}
}
