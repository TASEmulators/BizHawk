using System.Drawing;
using System.Drawing.Imaging;

namespace BizHawk.Client.Common
{
	public interface IGuiApi : IDisposable, IExternalApi
	{
		void ToggleCompositingMode();

		[Obsolete("No longer supported, returns null always.")]
		ImageAttributes GetAttributes();
		[Obsolete("No longer supported, no-op.")]
		void SetAttributes(ImageAttributes a);

		void WithSurface(DisplaySurfaceID surfaceID, Action<IGuiApi> drawingCallsFunc);

		[Obsolete("use the other overload e.g. `APIs.Gui.WithSurface(..., gui => { gui.DrawLine(...); });`")]
		void WithSurface(DisplaySurfaceID surfaceID, Action drawingCallsFunc);

		[Obsolete("No longer supported, no-op.")]
		void DrawNew(string name, bool clear = true);

		[Obsolete("No longer supported, no-op.")]
		void DrawFinish();

		[Obsolete("Always true")]
		bool HasGUISurface { get; }

		void SetPadding(int all);
		void SetPadding(int x, int y);
		void SetPadding(int l, int t, int r, int b);
		(int Left, int Top, int Right, int Bottom) GetPadding();

		void AddMessage(string message, int? duration = null);

		void ClearGraphics(DisplaySurfaceID? surfaceID = null);
		void ClearText();
		void SetDefaultForegroundColor(Color color);
		void SetDefaultBackgroundColor(Color color);
		Color GetDefaultTextBackground();
		void SetDefaultTextBackground(Color color);
		void SetDefaultPixelFont(string fontfamily);
		void DrawBezier(Point p1, Point p2, Point p3, Point p4, Color? color = null, DisplaySurfaceID? surfaceID = null);
		void DrawBeziers(Point[] points, Color? color = null, DisplaySurfaceID? surfaceID = null);
		void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null);
		void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null);
		void DrawIcon(string path, int x, int y, int? width = null, int? height = null, DisplaySurfaceID? surfaceID = null);
		void DrawImage(Image img, int x, int y, int? width = null, int? height = null, bool cache = true, DisplaySurfaceID? surfaceID = null);
		void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true, DisplaySurfaceID? surfaceID = null);
		void ClearImageCache();
		void DrawImageRegion(Image img, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null, DisplaySurfaceID? surfaceID = null);
		void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null, DisplaySurfaceID? surfaceID = null);
		void DrawLine(int x1, int y1, int x2, int y2, Color? color = null, DisplaySurfaceID? surfaceID = null);
		void DrawAxis(int x, int y, int size, Color? color = null, DisplaySurfaceID? surfaceID = null);
		void DrawPie(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null);
		void DrawPixel(int x, int y, Color? color = null, DisplaySurfaceID? surfaceID = null);
		void DrawPolygon(Point[] points, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null);
		void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null);

		/// <remarks>exposed to Lua as <c>gui.drawString</c> and alias <c>gui.drawText</c></remarks>
		void DrawString(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, int? fontsize = null, string fontfamily = null, string fontstyle = null, string horizalign = null, string vertalign = null, DisplaySurfaceID? surfaceID = null);

		/// <remarks>exposed to Lua as <c>gui.pixelText</c></remarks>
		[Obsolete("method renamed to PixelText to match Lua")]
		void DrawText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null, DisplaySurfaceID? surfaceID = null);

		/// <remarks>exposed to Lua as <c>gui.pixelText</c></remarks>
		void PixelText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null, DisplaySurfaceID? surfaceID = null);

		/// <remarks>exposed to Lua as <c>gui.text</c></remarks>
		void Text(int x, int y, string message, Color? forecolor = null, string anchor = null);
	}
}