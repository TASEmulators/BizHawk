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
		public bool ReadOnly { get; set; }

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

		private readonly Brush WhiteBrush = Brushes.White;
		private readonly Brush GrayBrush = Brushes.LightGray;
		private readonly Brush RedBrush = Brushes.Red;
		private readonly Brush OffWhiteBrush = Brushes.Beige;

		private readonly Pen BlackPen = new Pen(Brushes.Black);
		private readonly Pen BluePen = new Pen(Brushes.Blue, 2);

		private readonly Bitmap Dot = new Bitmap(7, 7);
		private readonly Bitmap GrayDot = new Bitmap(7, 7);

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

		private static int RealToGfx(int val)
		{
			return (val + 128) / 2;
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

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				//Background
				e.Graphics.FillRectangle(GrayBrush, 0, 0, 128, 128);
				e.Graphics.FillEllipse(ReadOnly ? OffWhiteBrush : WhiteBrush, 0, 0, 127, 127);
				e.Graphics.DrawEllipse(BlackPen, 0, 0, 127, 127);
				e.Graphics.DrawLine(BlackPen, 64, 0, 64, 127);
				e.Graphics.DrawLine(BlackPen, 0, 63, 127, 63);

				//Line
				if (HasValue)
				{
					e.Graphics.DrawLine(BluePen, 64, 63, RealToGfx(X), 127 - RealToGfx(Y));
					e.Graphics.DrawImage(ReadOnly ? GrayDot : Dot, RealToGfx(X) - 3, 127 - RealToGfx(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				if (e.Button == MouseButtons.Left)
				{
					X = GfxToReal(e.X - 64, true);
					Y = GfxToReal(-(e.Y - 63), false);
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
					X = GfxToReal(e.X - 64, true);
					Y = GfxToReal(-(e.Y - 63), false);
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
