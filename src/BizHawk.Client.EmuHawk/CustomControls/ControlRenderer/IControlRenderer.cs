using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public interface IControlRenderer : IDisposable
	{
		/// <summary>
		/// Required to use before calling drawing methods
		/// </summary>
		IDisposable LockGraphics(Graphics g);

		/// <summary>
		/// Measure the width and height of string <paramref name="str"/> when drawn
		/// using the given font <paramref  name="font"/>
		/// </summary>
		SizeF MeasureString(string str, Font font);

		void SetBrush(Color color);
		void SetSolidPen(Color color);

		void PrepDrawString(Font font, Color color, bool rotate = false);

		/// <summary>
		/// Draw the given string using the given font and foreground color at the X/Y of the given rect.
		/// Text not fitting inside of the rect will be truncated
		/// </summary>
		void DrawString(string str, Rectangle rect);

		void DrawRectangle(Rectangle rect);
		void FillRectangle(Rectangle rect);

		/// <summary>
		/// Draw a bitmap object at the given position
		/// </summary>
		void DrawBitmap(Bitmap bitmap, Point point);
		void Line(int x1, int y1, int x2, int y2);
	}
}
