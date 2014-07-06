using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class AnalogStickPanel : Panel
	{
		private int _x = 0;
		private int _y = 0;

		public int X
		{
			get
			{
				return _x;
			}

			set
			{
				_x = value;
				SetAnalog();
			}
		}

		public int Y
		{
			get
			{
				return _y;
			}

			set
			{
				_y = value;
				SetAnalog();
			}
		}

		public bool HasValue = false;
		public bool ReadOnly { get; set; }

		public string XName = string.Empty;
		public string YName = string.Empty;

		private IController _previous = null;

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

		public int MinX { get { return 0 - MaxX - 1; } }
		public int MinY { get { return 0 - MaxY - 1; } }

		private readonly Brush WhiteBrush = Brushes.White;
		private readonly Brush GrayBrush = Brushes.LightGray;
		private readonly Brush RedBrush = Brushes.Red;
		private readonly Brush OffWhiteBrush = Brushes.Beige;

		private readonly Pen BlackPen = new Pen(Brushes.Black);
		private readonly Pen BluePen = new Pen(Brushes.Blue, 2);
		private readonly Pen GrayPen = new Pen(Brushes.Gray, 2);

		private readonly Bitmap Dot = new Bitmap(7, 7);
		private readonly Bitmap GrayDot = new Bitmap(7, 7);

		public AnalogStickPanel()
		{
			Size = new Size(MaxX + 1, MaxY + 1);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			BorderStyle = BorderStyle.Fixed3D;
			
			// Draw the dot into a bitmap
			var g = Graphics.FromImage(Dot);
			g.Clear(Color.Transparent);
			g.FillRectangle(RedBrush, 2, 0, 3, 7);
			g.FillRectangle(RedBrush, 1, 1, 5, 5);
			g.FillRectangle(RedBrush, 0, 2, 7, 3);

			var gg = Graphics.FromImage(GrayDot);
			gg.Clear(Color.Transparent);
			gg.FillRectangle(Brushes.Gray, 2, 0, 3, 7);
			gg.FillRectangle(Brushes.Gray, 1, 1, 5, 5);
			gg.FillRectangle(Brushes.Gray, 0, 2, 7, 3);
		}

		private int RealToGfx(int val)
		{
			return (val + MaxX) / 2;
		}

		private int GfxToReal(int val, bool isX) // isX is a hack
		{
			var max = isX ? MaxX : MaxY;
			var min = isX ? MinX : MinY;

			var ret = (val * 2);
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
			var xn = HasValue ? X : (int?)null;
			var yn = HasValue ? Y : (int?)null;
			Global.StickyXORAdapter.SetFloat(XName, xn);
			Global.StickyXORAdapter.SetFloat(YName, yn);

			Refresh();
		}

		private int MidX { get { return (int)((MaxX + 0.5) / 2); } }
		private int MidY { get { return (int)((MaxY + 0.5) / 2); } }
		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				// Background
				e.Graphics.FillRectangle(GrayBrush, 0, 0, MaxX, MaxY);
				e.Graphics.FillEllipse(ReadOnly ? OffWhiteBrush : WhiteBrush, 0, 0, MaxX - 3, MaxY - 3);
				e.Graphics.DrawEllipse(BlackPen, 0, 0, MaxX - 3, MaxY - 3);
				e.Graphics.DrawLine(BlackPen, MidX, 0, MidX, MaxY);
				e.Graphics.DrawLine(BlackPen, 0, MidY, MaxX, MidY);

				// Previous frame
				if (_previous != null)
				{
					var pX = (int)_previous.GetFloat(XName);
					var pY = (int)_previous.GetFloat(YName);
					e.Graphics.DrawLine(GrayPen, MidX, MidY, RealToGfx(pX), MaxY - RealToGfx(pY));
					e.Graphics.DrawImage(GrayDot, RealToGfx(pX) - 3, MaxY - RealToGfx(pY) - 3);
				}

				// Line
				if (HasValue)
				{
					e.Graphics.DrawLine(BluePen, MidX, MidY, RealToGfx(X), MaxY - RealToGfx(Y));
					e.Graphics.DrawImage(ReadOnly ? GrayDot : Dot, RealToGfx(X) - 3, MaxY - RealToGfx(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				if (e.Button == MouseButtons.Left)
				{
					X = GfxToReal(e.X - MidX, true);
					Y = GfxToReal(-(e.Y - MidY), false);
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
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x007B) // WM_CONTEXTMENU
			{
				// Don't let parent controls get this. We handle the right mouse button ourselves
				return;
			}

			base.WndProc(ref m);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				if (e.Button == MouseButtons.Left)
				{
					X = GfxToReal(e.X - MidX, true);
					Y = GfxToReal(-(e.Y - MidY), false);
					HasValue = true;
				}
				if (e.Button == MouseButtons.Right)
				{
					Clear();
				}

				Refresh();
			}
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

		public void SetPrevious(IController previous)
		{
			_previous = previous;
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
