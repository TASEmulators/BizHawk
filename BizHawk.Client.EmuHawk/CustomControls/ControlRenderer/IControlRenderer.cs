using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public interface IControlRenderer : IDisposable
	{
		IDisposable LockGraphics(Graphics g);

		void StartOffScreenBitmap(int width, int height);
		void EndOffScreenBitmap();
		void CopyToScreen();

		Size MeasureString(string str, Font font);

		void SetBrush(Color color);
		void SetSolidPen(Color color);

		// TODO: use the Font version
		void PrepDrawString(IntPtr hFont, Color color);
		void DrawString(string str, Point point);

		void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
		void FillRectangle(int x, int y, int w, int h);
		void DrawBitmap(Bitmap bitmap, Point point, bool blend = false);
		void Line(int x1, int y1, int x2, int y2);
	}
}
