using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class GdiPlusRenderer : IControlRenderer
	{
		private Graphics _graphics;

		private readonly Pen _currentPen = Pens.Black.GetMutableCopy();

		private readonly SolidBrush _currentBrush = ((SolidBrush) Brushes.Black).GetMutableCopy();

		private readonly SolidBrush _currentStringBrush = ((SolidBrush) Brushes.Black).GetMutableCopy();

		private Font _currentFont;
		private bool _rotateString;

		public GdiPlusRenderer(Font font)
		{
			_currentFont = font;
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
		}

		public void DrawBitmap(Bitmap bitmap, Point point)
		{
			_graphics.DrawImage(bitmap, point);
		}

		public void DrawRectangle(Rectangle rect)
		{
			_graphics.DrawRectangle(_currentPen, rect);
		}

		public void DrawString(string str, Rectangle rect)
		{
			if (_rotateString)
			{
				_graphics.TranslateTransform(rect.X, rect.Y);
				_graphics.RotateTransform(90);
				_graphics.DrawString(str, _currentFont, _currentStringBrush, Point.Empty);
				_graphics.ResetTransform();
			}
			else
			{
				_graphics.DrawString(str, _currentFont, _currentStringBrush, rect);
			}
		}

		public void FillRectangle(Rectangle rect)
		{
			_graphics.FillRectangle(_currentBrush, rect);
		}

		public void Line(int x1, int y1, int x2, int y2)
		{
			_graphics.DrawLine(_currentPen, x1, y1, x2, y2);
		}

		public IDisposable LockGraphics(Graphics g)
		{
			_graphics = g;
			return new GdiPlusGraphicsLock();
		}

		public SizeF MeasureString(string str, Font font)
		{
			return _graphics.MeasureString(str, font);
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
