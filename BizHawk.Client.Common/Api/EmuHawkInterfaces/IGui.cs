using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace BizHawk.Client.Common
{
	public interface IGui : IDisposable, IExternalApi
	{
		ImageAttributes Attributes { get; set; }

		Color? DefaultBackgroundColor { get; set; }

		Color DefaultForegroundColor { get; set; }

		string DefaultPixelFont { get; set; }

		Color? DefaultTextBackground { get; set; }

		bool HasGUISurface { get; }

		(int Left, int Top, int Right, int Bottom) Padding { get; set; }

		void AddMessage(string message);

		void ClearGraphics();

		void ClearImageCache();

		void ClearText();

		void DrawAxis(int x, int y, int size, Color? color = null);

		void DrawBezier(Point p1, Point p2, Point p3, Point p4, Color? color = null);

		void DrawBeziers(Point[] points, Color? color = null);

		void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null);

		void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null);

		void DrawFinish();

		void DrawIcon(string path, int x, int y, int? width = null, int? height = null);

		void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true);

		void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null);

		void DrawLine(int x1, int y1, int x2, int y2, Color? color = null);

		void DrawNew(string name, bool clear = true);

		void DrawPie(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null, Color? background = null);

		void DrawPixel(int x, int y, Color? color = null);

		void DrawPolygon(Point[] points, Color? line = null, Color? background = null);

		void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null);

		void DrawString(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, int? fontsize = null, string fontfamily = null, string fontstyle = null, string horizalign = null, string vertalign = null);

		void DrawText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null);

		void Text(int x, int y, string message, Color? forecolor = null, string anchor = null);

		void ToggleCompositingMode();
	}
}