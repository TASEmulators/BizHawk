using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class AnalogStickPanel : Panel
	{
		public int X = 0;
		public int Y = 0;
		public bool HasValue = false;

		public string XName = string.Empty;
		public string YName = string.Empty;

		public int MaxX
		{
			get { return _maxX; }
			set
			{
				_maxX = value;
				CheckMax();
			}
		}

		public int MaxY
		{
			get { return _maxY; }
			set
			{
				_maxY = value;
				CheckMax();
			}
		}

		private int _maxX = 127;
		private int _maxY = 127;

		private int MinX { get { return 0 - MaxX; } }
		private int MinY { get { return 0 - MaxY; } }

		private readonly Brush _whiteBrush = Brushes.White;
		private readonly Brush _blackBrush = Brushes.Black;
		private readonly Brush _grayBrush = Brushes.LightGray;
		private readonly Brush _redBrush = Brushes.Red;
		private readonly Brush _blueBrush = Brushes.DarkBlue;
		private readonly Pen _blackPen;
		private readonly Pen _bluePen;

		private readonly Bitmap dot = new Bitmap(7, 7);
		private readonly Bitmap graydot = new Bitmap(7, 7);

		public AnalogStickPanel()
		{
			Size = new Size(129, 129);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			_blackPen = new Pen(_blackBrush);
			_bluePen = new Pen(_blueBrush);
			BorderStyle = BorderStyle.Fixed3D;
			
			// Draw the dot into a bitmap
			Graphics g = Graphics.FromImage(dot);
			g.Clear(Color.Transparent);
			g.FillRectangle(_redBrush, 2, 0, 3, 7);
			g.FillRectangle(_redBrush, 1, 1, 5, 5);
			g.FillRectangle(_redBrush, 0, 2, 7, 3);

			Graphics gg = Graphics.FromImage(graydot);
			gg.Clear(Color.Transparent);
			gg.FillRectangle(Brushes.Gray, 2, 0, 3, 7);
			gg.FillRectangle(Brushes.Gray, 1, 1, 5, 5);
			gg.FillRectangle(Brushes.Gray, 0, 2, 7, 3);
		}

		private int RealToGFX(int val)
		{
			return (val + 128) / 2;
		}

		private int GFXToReal(int val, bool isX) // isX is a hack
		{
			var max = isX ? MaxX : MaxY;
			var min = isX ? MinX : MinY;

			int ret = (val * 2);
			if (ret > max)
			{
				ret = max;
			}

			if (ret < min)
			{
				ret = min;
			}

			return ret;
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			SetAnalog();
		}

		private void SetAnalog()
		{
			int? xn = HasValue ? X : (int?)null;
			int? yn = HasValue ? Y : (int?)null;
			Global.StickyXORAdapter.SetFloat(XName, xn);
			Global.StickyXORAdapter.SetFloat(YName, yn);

			Refresh();
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				//Background
				e.Graphics.FillRectangle(_grayBrush, 0, 0, 128, 128);
				e.Graphics.FillEllipse(_whiteBrush, 0, 0, 127, 127);
				e.Graphics.DrawEllipse(_blackPen, 0, 0, 127, 127);
				e.Graphics.DrawLine(_blackPen, 64, 0, 64, 127);
				e.Graphics.DrawLine(_blackPen, 0, 63, 127, 63);

				if (Global.MovieSession != null && Global.MovieSession.Movie != null && // For the desinger
					Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished &&
					Global.Emulator.Frame > 1)
				{
					var input = Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 2);

					var x = input.GetFloat(XName);
					var y = input.GetFloat(YName);

					var xx = RealToGFX((int)x);
					var yy = RealToGFX((int)y);

					e.Graphics.DrawLine(new Pen(Brushes.Gray), 64, 63, xx, 127 - yy);
					e.Graphics.DrawImage(graydot, xx - 3, 127 - yy - 3);
				}

				//Line
				if (HasValue)
				{
					e.Graphics.DrawLine(_bluePen, 64, 63, RealToGFX(X), 127 - RealToGFX(Y));
					e.Graphics.DrawImage(dot, RealToGFX(X) - 3, 127 - RealToGFX(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				X = GFXToReal(e.X - 64, true);
				Y = GFXToReal(-(e.Y - 63), false);
				HasValue = true;
				SetAnalog();
			}
			else if (e.Button == MouseButtons.Right)
			{
				Clear();
			}

			Refresh();
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
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
				X = GFXToReal(e.X - 64, true);
				Y = GFXToReal(-(e.Y - 63), false);
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

		public void Set(IController controller)
		{
			var newX = (int)controller.GetFloat(XName);
			var newY = (int)controller.GetFloat(YName);
			var changed = newX != X || newY != Y;
			if (changed)
			{
				SetPosition(newX, newY);
			}
		}

		public void SetPosition(int xval, int yval)
		{
			X = xval;
			Y = yval;
			HasValue = true;
			
			Refresh();
		}

		private void CheckMax()
		{
			if (X > MaxX)
			{
				X = MaxX;
			}
			else if (X < MinX)
			{
				X = MinX;
			}

			if (Y > MaxY)
			{
				Y = MaxY;
			}
			else if (Y < MinY)
			{
				Y = MinY;
			}

			Refresh();
		}
	}
}
