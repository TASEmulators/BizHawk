using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

//Just because this code was mostly rewritten, dont think it isnt still awful

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
				if (value < MinX) { _x = MinX; }
				else if (value > MaxX) { _x = MaxX; }
				else { _x = value; }
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
				if (value < MinY) { _y = MinY; }
				else if (value > MaxY) { _y = MaxY; }
				else { _y = value; }
				SetAnalog();
			}
		}

		public bool HasValue = false;
		public bool ReadOnly { get; set; }

		public string XName = string.Empty;
		public string YName = string.Empty;

		private IController _previous = null;

		float UserRangePercentageX = 100, UserRangePercentageY = 100;

		public void SetUserRange(float rx, float ry)
		{
			UserRangePercentageX = rx;
			UserRangePercentageY = ry;
			Rerange();
			Refresh();
		}

		public void SetRangeX(float[] range)
		{
			for (int i = 0; i < 3; i++) ActualRangeX[i] = range[i];
			Rerange();
		}

		public void SetRangeY(float[] range)
		{
			for (int i = 0; i < 3; i++) ActualRangeY[i] = range[i];
			Rerange();
		}

		public float[] RangeX = new float[] { -128f, 0.0f, 127f };
		public float[] RangeY = new float[] { -128f, 0.0f, 127f };
		public float[] ActualRangeX = new float[] { -128f, 0.0f, 127f };
		public float[] ActualRangeY = new float[] { -128f, 0.0f, 127f };

		float flipx = 1, flipy = 1;

		void Rerange()
		{
			//baseline:
			//Array.Copy(ActualRangeX, RangeX, 3);
			//Array.Copy(ActualRangeY, RangeY, 3);

			float rx = ActualRangeX[2] - ActualRangeX[0];
			float ry = ActualRangeY[2] - ActualRangeY[0];
			float midx = rx / 2 + ActualRangeX[0];
			float midy = ry / 2 + ActualRangeY[0];
			rx *= UserRangePercentageX / 100;
			ry *= UserRangePercentageY / 100;
			float minx = midx - rx / 2;
			float maxx = minx + rx;
			float miny = midy - ry / 2;
			float maxy = miny + ry;

			if (minx > maxx)
			{
				float temp = minx;
				minx = maxx;
				maxx = temp;
				flipx = -1;
			}

			if (miny > maxy)
			{
				float temp = miny;
				miny = maxy;
				maxy = temp;
				flipy = -1;
			}

			//Range?[1] isn't really used
			RangeX[0] = minx;
			RangeX[2] = maxx;
			RangeY[0] = miny;
			RangeY[2] = maxy;

			Clamp();
		}

		//dont count on this working. it's never been tested.
		//but it kind of must be, or else nothing here would work...
		public float ScaleX = 0.60f;
		public float ScaleY = 0.60f;

		int MinX { get { return (int)(RangeX[0]); } }
		int MinY { get { return (int)(RangeY[0]); } }
		int MaxX { get { return (int)(RangeX[2]); } }
		int MaxY { get { return (int)(RangeY[2]); } }
		int RangeSizeX { get { return (int)(MaxX - MinX + 1); } }
		int RangeSizeY { get { return (int)(MaxY - MinY + 1); } }

		int PixelSizeX { get { return (int)(RangeSizeX * ScaleX); } }
		int PixelSizeY { get { return (int)(RangeSizeY * ScaleY); } }
		int PixelMinX { get { return (Size.Width - PixelSizeX) / 2; } }
		int PixelMinY { get { return (Size.Height - PixelSizeY) / 2; } }
		int PixelMidX { get { return PixelMinX + PixelSizeX / 2; } }
		int PixelMidY { get { return PixelMinY + PixelSizeY / 2; } }
		int PixelMaxX { get { return PixelMinX + PixelSizeX - 1; } }
		int PixelMaxY { get { return PixelMinY + PixelSizeY - 1; } }

		private int RealToGfxX(int val)
		{
			int v = val;
			if (flipx == -1)
				v = (MaxX - val) + MinX;
			v = (int)(((float)v - MinX) * ScaleX);
			v += PixelMinX;
			return v;
		}

		private int RealToGfxY(int val)
		{
			int v = val;
			if (flipy == -1)
				v = (MaxY - val) + MinY;
			v = (int)(((float)v - MinY) * ScaleY);
			v += PixelMinY;
			return v;
		}

		private int GfxToRealX(int val)
		{
			val -= PixelMinX;
			float v = ((float)val / ScaleX + MinX);
			if (v < MinX) v = MinX;
			if (v > MaxX) v = MaxX;
			if (flipx == -1)
				v = (MaxX - v) + MinX;
			return (int)v;
		}

		private int GfxToRealY(int val)
		{
			val -= PixelMinY;
			float v;
			v = ((float)val / ScaleY + MinY);
			if (v < MinX) v = MinX;
			if (v > MaxX) v = MaxX;
			if(flipy == -1)
				v = (MaxY - v) + MinY;
			return (int)v;
		}

		private readonly Brush WhiteBrush = Brushes.White;
		private readonly Brush GrayBrush = Brushes.LightGray;
		private readonly Brush RedBrush = Brushes.Red;
		private readonly Brush OffWhiteBrush = Brushes.Beige;

		private readonly Pen BlackPen = new Pen(Brushes.Black);
		private readonly Pen BluePen = new Pen(Brushes.Blue, 2);
		private readonly Pen GrayPen = new Pen(Brushes.Gray, 2);

		private readonly Bitmap Dot = new Bitmap(7, 7);
		private readonly Bitmap GrayDot = new Bitmap(7, 7);

		public Action ClearCallback { get; set; }

		private void DoClearCallback()
		{
			if (ClearCallback != null)
			{
				ClearCallback();
			}
		}

		public AnalogStickPanel()
		{
			Size = new Size(PixelSizeX + 1, PixelSizeY + 1);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			BorderStyle = BorderStyle.Fixed3D;

			// Draw the dot into a bitmap
			using (var g = Graphics.FromImage(Dot))
			{
				g.Clear(Color.Transparent);
				g.FillRectangle(RedBrush, 2, 0, 3, 7);
				g.FillRectangle(RedBrush, 1, 1, 5, 5);
				g.FillRectangle(RedBrush, 0, 2, 7, 3);
			}

			using (var gg = Graphics.FromImage(GrayDot))
			{
				gg.Clear(Color.Transparent);
				gg.FillRectangle(Brushes.Gray, 2, 0, 3, 7);
				gg.FillRectangle(Brushes.Gray, 1, 1, 5, 5);
				gg.FillRectangle(Brushes.Gray, 0, 2, 7, 3);
			}
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
				// Background
				e.Graphics.Clear(Color.LightGray);

				e.Graphics.FillRectangle(GrayBrush, PixelMinX, PixelMinY, PixelMaxX - PixelMinX, PixelMaxY- PixelMinY);
				e.Graphics.FillEllipse(ReadOnly ? OffWhiteBrush : WhiteBrush, PixelMinX, PixelMinY, PixelMaxX - PixelMinX - 2, PixelMaxY - PixelMinY - 3);
				e.Graphics.DrawEllipse(BlackPen, PixelMinX, PixelMinY, PixelMaxX - PixelMinX - 2, PixelMaxY - PixelMinY - 3);
				e.Graphics.DrawLine(BlackPen, PixelMidX, 0, PixelMidX, PixelMaxY);
				e.Graphics.DrawLine(BlackPen, 0, PixelMidY, PixelMaxX, PixelMidY);

				// Previous frame
				if (_previous != null)
				{
					var pX = (int)_previous.GetFloat(XName);
					var pY = (int)_previous.GetFloat(YName);
					e.Graphics.DrawLine(GrayPen, PixelMidX, PixelMidY, RealToGfxX(pX), RealToGfxY(pY));
					e.Graphics.DrawImage(GrayDot, RealToGfxX(pX) - 3, RealToGfxY(MaxY) - RealToGfxY(pY) - 3);
				}

				// Line
				if (HasValue)
				{
					e.Graphics.DrawLine(BluePen, PixelMidX, PixelMidY, RealToGfxX(X), RealToGfxY(Y));
					e.Graphics.DrawImage(ReadOnly ? GrayDot : Dot, RealToGfxX(X) - 3, RealToGfxY(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				if (e.Button == MouseButtons.Left)
				{
					X = GfxToRealX(e.X);
					Y = GfxToRealY(e.Y);
					Clamp();
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
					X = GfxToRealX(e.X);
					Y = GfxToRealY(e.Y);
					Clamp();
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
			if (X != 0 || Y != 0 || HasValue)
			{
				X = Y = 0;
				HasValue = false;
				DoClearCallback();
				Refresh();
			}
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
			Clamp();
			HasValue = true;
			
			Refresh();
		}

		private void Clamp()
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
		}
	}
}
