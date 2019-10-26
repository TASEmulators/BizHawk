using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public interface IControlRenderer : IDisposable
	{
		/// <summary>
		/// Required to use before calling drawing methods
		/// </summary>
		IDisposable LockGraphics(Graphics g, int width, int height);

		/// <summary>
		/// Measure the width and height of string <paramref name="str"/> when drawn
		/// using the given font <paramref  name="font"/>
		/// </summary>
		Size MeasureString(string str, Font font);

		void SetBrush(Color color);
		void SetSolidPen(Color color);

		void PrepDrawString(Font font, Color color, bool rotate = false);

		/// <summary>
		/// Draw the given string using the given  font and foreground color at given location
		/// </summary>
		void DrawString(string str, Point point);


		void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
		void FillRectangle(int x, int y, int w, int h);

		/// <summary>
		/// Draw a bitmap object at the given position
		/// </summary>
		void DrawBitmap(Bitmap bitmap, Point point);
		void Line(int x1, int y1, int x2, int y2);
	}
}
