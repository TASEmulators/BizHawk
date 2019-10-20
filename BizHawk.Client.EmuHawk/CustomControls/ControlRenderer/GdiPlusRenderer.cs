using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class GdiPlusRenderer : IControlRenderer
	{
		private Graphics _graphics;
		private int _width;
		private int _height;

		// TODO: see if caching has any benefit
		private readonly Dictionary<Color, Pen> _penCache = new Dictionary<Color, Pen>();
		private Pen _currentPen;

		private readonly Dictionary<Color, Brush> _brushCache = new Dictionary<Color, Brush>();
		private Brush _currentBrush;

		private Font _currentFont = new Font("Arial", 8, FontStyle.Bold);
		// TODO: cache this?
		private Brush _currentStringBrush = new SolidBrush(Color.Black);

		public GdiPlusRenderer()
		{
			_currentPen = new Pen(Color.Black);
			_penCache.Add(Color.Black, _currentPen);

			_currentBrush = new SolidBrush(Color.Black);
			_brushCache.Add(Color.Black, _currentBrush);
		}

		private class GdiPlusGraphicsLock : IDisposable
		{
			private readonly GdiPlusRenderer _renderer;

			public GdiPlusGraphicsLock(GdiPlusRenderer renderer)
			{
				_renderer = renderer;
			}

			public void Dispose()
			{
				// TODO
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
			_width = width;
			_height = height;
			return new GdiPlusGraphicsLock(this);
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
			_currentStringBrush = new SolidBrush(color);
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
