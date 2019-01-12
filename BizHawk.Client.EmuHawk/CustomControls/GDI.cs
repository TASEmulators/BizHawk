using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <summary>
	/// Singleton holding GDIRenderer and associated types
	/// </summary>
	public sealed class GDI
	{
		private GDI() {}

		/// <summary>
		/// Wrapper for GDI rendering functions
		/// Inheritors are not thread-safe as GDI functions should be called from the UI thread
		/// </summary>
		public interface GDIRenderer : IDisposable
		{
			void CopyToScreen();
			void DrawBitmap(Bitmap bitmap, Point point, bool blend = false);
			void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
			void DrawString(string str, Point point);
			void EndOffScreenBitmap();
			void FillRectangle(int x, int y, int w, int h);
			void Line(int x1, int y1, int x2, int y2);
			GDIGraphicsLock<GDIRenderer> LockGraphics(Graphics g);
			Size MeasureString(string str, Font font);
			void PrepDrawString(IntPtr hfont, Color color);
			void SetBrush(Color color);
			void SetSolidPen(Color color);
			void StartOffScreenBitmap(int width, int height);

			/// <summary>
			/// do not use outside GDI.GDIGraphicsLock<*>
			/// </summary>
			void HackDisposeGraphics();
		}

		public class GDIGraphicsLock<R> : IDisposable where R : GDIRenderer
		{
			private readonly R Renderer;
			public GDIGraphicsLock(R gdi)
			{
				Renderer = gdi;
			}
			public void Dispose()
			{
				Renderer.HackDisposeGraphics();
			}
		}

		[Flags]
		public enum ETOOptions : uint
		{
			CLIPPED = 0x4,
			GLYPH_INDEX = 0x10,
			IGNORELANGUAGE = 0x1000,
			NUMERICSLATIN = 0x800,
			NUMERICSLOCAL = 0x400,
			OPAQUE = 0x2,
			PDY = 0x2000,
			RTLREADING = 0x800,
		}

		/// <summary>
		/// See [http://msdn.microsoft.com/en-us/library/windows/desktop/dd162498(v=vs.85).aspx][15]
		///  </summary>
		[Flags]
		public enum TextFormatFlags : uint
		{
			Default = 0x00000000,
			Center = 0x00000001,
			Right = 0x00000002,
			VCenter = 0x00000004,
			Bottom = 0x00000008,
			WordBreak = 0x00000010,
			SingleLine = 0x00000020,
			ExpandTabs = 0x00000040,
			TabStop = 0x00000080,
			NoClip = 0x00000100,
			ExternalLeading = 0x00000200,
			CalcRect = 0x00000400,
			NoPrefix = 0x00000800,
			Internal = 0x00001000,
			EditControl = 0x00002000,
			PathEllipsis = 0x00004000,
			EndEllipsis = 0x00008000,
			ModifyString = 0x00010000,
			RtlReading = 0x00020000,
			WordEllipsis = 0x00040000,
			NoFullWidthCharBreak = 0x00080000,
			HidePrefix = 0x00100000,
			ProfixOnly = 0x00200000,
		}

		[Flags]
		public enum PenStyles
		{
			PS_SOLID = 0x00000000
			// TODO
		}

		public enum PaintObjects
		{
			WHITE_BRUSH = 0,
			LTGRAY_BRUSH = 1,
			GRAY_BRUSH = 2,
			DKGRAY_BRUSH = 3,
			BLACK_BRUSH = 4,
			NULL_BRUSH = 5,
			WHITE_PEN = 6,
			BLACK_PEN = 7,
			NULL_PEN = 8,
			OEM_FIXED_FONT = 10,
			ANSI_FIXED_FONT = 11,
			ANSI_VAR_FONT = 12,
			SYSTEM_FONT = 13,
			DEVICE_DEFAULT_FONT = 14,
			DEFAULT_PALETTE = 15,
			SYSTEM_FIXED_FONT = 16,
			DC_BRUSH = 18,
			DC_PEN = 19,
		}

		public enum BkModes : int
		{
			TRANSPARENT = 1,
			OPAQUE = 2
		}
	}
}
