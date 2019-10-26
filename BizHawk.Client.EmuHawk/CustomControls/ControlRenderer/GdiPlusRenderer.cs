using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class GdiPlusRenderer : IControlRenderer
	{
		private Graphics _graphics;

		private readonly Pen _currentPen = new Pen(Color.Black);
		private readonly SolidBrush _currentBrush = new SolidBrush(Color.Black);
		private readonly SolidBrush _currentStringBrush = new SolidBrush(Color.Black);
		private readonly Font _defaultFont = new Font("Arial", 8, FontStyle.Bold);
		private Font _currentFont;
		private bool _rotateString;

		public GdiPlusRenderer()
		{
			_currentFont = _defaultFont;
		}

		private class GdiPlusGraphicsLock : IDisposable
		{
			public void Dispose()
			{
				// Nothing to do
				// Other drawing methods need a way to dispose on demand, hence the need for 
				// this dummy class
			}
		}

		public void Dispose()
		{
			_currentPen.Dispose();
			_currentBrush.Dispose();
			_currentStringBrush.Dispose();
			_defaultFont.Dispose();
		}

		public void DrawBitmap(Bitmap bitmap, Point point, bool blend = false)
		{
			// TODO: implement blend
			_graphics.DrawImage(bitmap, point);
		}

		public void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect)
		{
			_graphics.DrawRectangle(
				_currentPen,
				new Rectangle(nLeftRect, nTopRect, nRightRect - nLeftRect, nBottomRect - nTopRect));
		}

		public void DrawString(string str, Point point)
		{
			if (_rotateString)
			{
				_graphics.TranslateTransform(point.X, point.Y);
				_graphics.RotateTransform(90);
				_graphics.DrawString(str, _currentFont, _currentStringBrush, Point.Empty);
				_graphics.ResetTransform();
			}
			else
			{
				_graphics.DrawString(str, _currentFont, _currentStringBrush, point);
			}
		}

		public void FillRectangle(int x, int y, int w, int h)
		{
			_graphics.FillRectangle(
				_currentBrush,
				new Rectangle(x, y, w, h));
		}

		public void Line(int x1, int y1, int x2, int y2)
		{
			_graphics.DrawLine(_currentPen, x1, y1, x2, y2);
		}

		public IDisposable LockGraphics(Graphics g, int width, int height)
		{
			_graphics = g;
			return new GdiPlusGraphicsLock();
		}

		public Size MeasureString(string str, Font font)
		{
			var size = _graphics.MeasureString(str, font);
			return new Size((int)(size.Width + 0.5), (int)(size.Height + 0.5));
		}

		public void PrepDrawString(Font font, Color color, bool rotate = false)
		{
			_currentFont = font;
			_currentStringBrush.Color = color;
			_rotateString = rotate;
		}

		public void SetBrush(Color color)
		{
			_currentBrush.Color = color;
		}

		public void SetSolidPen(Color color)
		{
			_currentPen.Color = color;
		}
	}
}
