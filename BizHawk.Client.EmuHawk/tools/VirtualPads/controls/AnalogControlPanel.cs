using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class VirtualPadAnalogStick : Panel, IVirtualPadControl
	{
		public int X = 0;
		public int Y = 0;
		public bool HasValue = false;
		public string Controller = "P1";

		private readonly Brush _white_brush = Brushes.White;
		private readonly Brush _black_brush = Brushes.Black;
		private readonly Brush _gray_brush = Brushes.LightGray;
		private readonly Brush _red_brush = Brushes.Red;
		private readonly Brush _blue_brush = Brushes.DarkBlue;
		private readonly Pen _black_pen;
		private readonly Pen _blue_pen;

		private readonly Bitmap dot = new Bitmap(7, 7);
		private readonly Bitmap graydot = new Bitmap(7, 7);

		public VirtualPadAnalogStick()
		{
			Size = new Size(129, 129);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			new Pen(_white_brush);
			_black_pen = new Pen(_black_brush);
			new Pen(_gray_brush);
			new Pen(_red_brush);
			_blue_pen = new Pen(_blue_brush);
			BorderStyle = BorderStyle.Fixed3D;
			
			// Draw the dot into a bitmap
			Graphics g = Graphics.FromImage(dot);
			g.Clear(Color.Transparent);
			g.FillRectangle(_red_brush, 2, 0, 3, 7);
			g.FillRectangle(_red_brush, 1, 1, 5, 5);
			g.FillRectangle(_red_brush, 0, 2, 7, 3);

			Graphics gg = Graphics.FromImage(graydot);
			gg.Clear(Color.Transparent);
			gg.FillRectangle(Brushes.Gray, 2, 0, 3, 7);
			gg.FillRectangle(Brushes.Gray, 1, 1, 5, 5);
			gg.FillRectangle(Brushes.Gray, 0, 2, 7, 3);
		}

		private int RealToGFX(int val)
		{
			return (val + 128)/2;
		}

		private int GFXToReal(int val)
		{
			int ret = (val * 2);
			if (ret > Max)
			{
				ret = Max;
			}

			if (ret < Min)
			{
				ret = Min;
			}

			return ret;
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				//Background
				e.Graphics.FillRectangle(_gray_brush, 0, 0, 128, 128);
				e.Graphics.FillEllipse(_white_brush, 0, 0, 127, 127);
				e.Graphics.DrawEllipse(_black_pen, 0, 0, 127, 127);
				e.Graphics.DrawLine(_black_pen, 64, 0, 64, 127);
				e.Graphics.DrawLine(_black_pen, 0, 63, 127, 63);

				if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
				{
					var input = Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1);

					var x = input.GetFloat(Controller + " X Axis");
					var y = input.GetFloat(Controller + " Y Axis");

					var xx = RealToGFX((int)x);
					var yy = RealToGFX((int)y);

					e.Graphics.DrawLine(new Pen(Brushes.Gray), 64, 63, xx, 127 - yy);
					e.Graphics.DrawImage(graydot, xx - 3, 127 - yy - 3);
				}

				//Line
				if (HasValue)
				{
					e.Graphics.DrawLine(_blue_pen, 64, 63, RealToGFX(X), 127 - RealToGFX(Y));
					e.Graphics.DrawImage(dot, RealToGFX(X) - 3, 127 - RealToGFX(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X - 64);
				Y = GFXToReal(-(e.Y - 63));
				HasValue = true;
			}
			if (e.Button == MouseButtons.Right)
			{
				Clear();
			}

			Refresh();

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (Capture)
				Capture = false;
		}


		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x007B) //WM_CONTEXTMENU
			{
				//dont let parent controls get this.. we handle the right mouse button ourselves
				return;
			}
			base.WndProc(ref m);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X - 64);
				Y = GFXToReal(-(e.Y - 63));
				HasValue = true;
			}
			if (e.Button == MouseButtons.Right)
			{
				Clear();
			}

			Refresh();
		}


		public void Clear()
		{
			X = Y = 0;
			HasValue = false;
			Refresh();
		}

		public void SetPosition(int xval, int yval)
		{
			X = xval;
			Y = yval;
			HasValue = true;
			
			Refresh();
		}

		public static int Max = 127;
		public static int Min = -127;
	}
}
