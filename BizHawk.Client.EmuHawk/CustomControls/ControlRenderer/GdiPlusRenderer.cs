using System;
using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class GdiPlusRenderer : IControlRenderer
	{
		private readonly Dictionary<Color, Pen> _penCache = new Dictionary<Color, Pen>();
		private readonly Dictionary<Color, Brush> _brushCache = new Dictionary<Color, Brush>();
		
		private Graphics _graphics;

		private Pen _currentPen = new Pen(Color.Black);
		private Brush _currentBrush = new SolidBrush(Color.Black);
		private Brush _currentStringBrush = new SolidBrush(Color.Black);
		private Font _currentFont = new Font("Arial", 8, FontStyle.Bold);

		public GdiPlusRenderer()
		{
			_currentPen = new Pen(Color.Black);
			_penCache.Add(Color.Black, _currentPen);

			_currentBrush = new SolidBrush(Color.Black);
			_brushCache.Add(Color.Black, _currentBrush);
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
			// TODO
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
			_graphics.DrawString(str, _currentFont, _currentStringBrush, point);
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
			// Implement TODO
			if (rotate)
			{
				throw new NotImplementedException("rotate not implemented yet!");
			}
			
			_currentFont = font;

			var result = _brushCache.TryGetValue(color, out Brush brush);
			if (!result)
			{
				brush = new SolidBrush(color);
				_brushCache.Add(color, brush);
			}

			_currentStringBrush = brush;
		}

		public void SetBrush(Color color)
		{
			var result = _brushCache.TryGetValue(color, out Brush brush);
			if (!result)
			{
				brush = new SolidBrush(color);
				_brushCache.Add(color, brush);
			}

			_currentBrush = brush;
		}

		public void SetSolidPen(Color color)
		{
			var result = _penCache.TryGetValue(color, out Pen pen);
			if (!result)
			{
				pen = new Pen(color);
				_penCache.Add(color, pen);
			}

			_currentPen = pen;
		}
	}
}
