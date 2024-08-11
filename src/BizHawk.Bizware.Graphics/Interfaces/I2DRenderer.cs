using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Interface for 2D rendering, similar to System.Drawing's Graphics
	/// Semantically, this must be able to be called at any point
	/// This means batching MUST occur
	/// As in this case IGL resources can only be used in the ctor, Dispose(), and Render()
	/// </summary>
	public interface I2DRenderer : IDisposable
	{
		/// <summary>
		/// Renders any pending draw calls.
		/// Returns the result as an ITexture2D.
		/// Internally, this may change the bound render target, pipeline, viewport/scissor, and blending state.
		/// If this is important, rebind them after calling this.
		/// Any rendering occurs after clearing the target texture.
		/// If nothing needs to be rendered and the size does not change, the contents are preserved
		/// </summary>
		ITexture2D Render(int width, int height);

		/// <summary>
		/// Clears any pending draw calls.
		/// This will also insert a command to clear the target texture.
		/// </summary>
		void Clear();

		/// <summary>
		/// Discards any pending draw calls.
		/// Similar to Clear(), except this won't insert a command to clear the target texture
		/// </summary>
		void Discard();

		CompositingMode CompositingMode { set; }

		void DrawBezier(Color color, Point pt1, Point pt2, Point pt3, Point pt4);

		void DrawBeziers(Color color, Point[] points);

		void DrawRectangle(Color color, int x, int y, int width, int height);

		void FillRectangle(Color color, int x, int y, int width, int height);

		void DrawEllipse(Color color, int x, int y, int width, int height);

		void FillEllipse(Color color, int x, int y, int width, int height);

		void DrawImage(Bitmap bitmap, int x, int y);

		void DrawImage(Bitmap bitmap, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, bool cache);

		void DrawLine(Color color, int x1, int y1, int x2, int y2);

		void DrawPie(Color color, int x, int y, int width, int height, int startAngle, int sweepAngle);

		void FillPie(Color color, int x, int y, int width, int height, int startAngle, int sweepAngle);

		void DrawPolygon(Color color, Point[] points);

		void FillPolygon(Color color, Point[] points);

		void DrawString(string s, Font font, Color color, float x, float y, StringFormat format = null, TextRenderingHint textRenderingHint = TextRenderingHint.SystemDefault);
	}
}
