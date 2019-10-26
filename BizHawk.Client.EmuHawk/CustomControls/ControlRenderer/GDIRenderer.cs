using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <summary>
	/// Wrapper for GDI rendering functions
	/// This class is not thread-safe as GDI functions should be called from the UI thread
	/// </summary>
	public sealed class GdiRenderer : IControlRenderer
	{
		// Cache of all the Fonts used, rather than create them again and again
		private readonly Dictionary<Font, FontCacheEntry> _fontsCache = new Dictionary<Font, FontCacheEntry>();

		private class FontCacheEntry
		{
			public IntPtr HFont;
			public IntPtr RotatedHFont;
		}

		// Cache of all the brushes used, rather than create them again and again
		private readonly Dictionary<Color, IntPtr> _brushCache = new Dictionary<Color, IntPtr>();

		private Graphics _g;
		private IntPtr _hdc;
		private IntPtr _currentBrush = IntPtr.Zero;

		#region Construct and Destroy

		public void Dispose()
		{
			foreach (var brush in _brushCache)
			{
				if (brush.Value != IntPtr.Zero)
				{
					DeleteObject(brush.Value);
				}
			}

			foreach (var fc in _fontsCache)
			{
				DeleteObject(fc.Value.HFont);
				DeleteObject(fc.Value.RotatedHFont);
			}

			System.Diagnostics.Debug.Assert(_hdc == IntPtr.Zero, "Disposed a GDIRenderer while it held an HDC");
			System.Diagnostics.Debug.Assert(_g == null, "Disposed a GDIRenderer while it held a Graphics");
		}

		#endregion

		#region Api

		public void DrawBitmap(Bitmap bitmap, Point point)
		{
			IntPtr hBmp = bitmap.GetHbitmap();
			var bitHdc = CreateCompatibleDC(CurrentHdc);
			IntPtr old = SelectObject(bitHdc, hBmp);
			AlphaBlend(CurrentHdc, point.X, point.Y, bitmap.Width, bitmap.Height, bitHdc, 0, 0, bitmap.Width, bitmap.Height, new BLENDFUNCTION(AC_SRC_OVER, 0, 0xff, AC_SRC_ALPHA));
			SelectObject(bitHdc, old);
			DeleteDC(bitHdc);
			DeleteObject(hBmp);
		}

		public IDisposable LockGraphics(Graphics g, int width, int height)
		{
			_g = g;
			_hdc = g.GetHdc();
			SetBkMode(_hdc, BkModes.TRANSPARENT);
			var l = new GdiGraphicsLock(this);
			StartOffScreenBitmap(width, height);
			return l;
		}
		
		public Size MeasureString(string str, Font font)
		{
			SetFont(font);

			var size = new Size();
			GetTextExtentPoint32(CurrentHdc, str, str.Length, ref size);
			return size;
		}

		public void DrawString(string str, Point point)
		{
			TextOut(CurrentHdc, point.X, point.Y, str, str.Length);
		}

		public static IntPtr CreateNormalHFont(Font font, int width)
		{
			var logFont = new LOGFONT();
			font.ToLogFont(logFont);
			logFont.lfWidth = width;
			logFont.lfOutPrecision = (byte)FontPrecision.OUT_TT_ONLY_PRECIS;
			var ret = CreateFontIndirect(logFont);
			return ret;
		}

		//this returns an IntPtr font because .net's Font class will erase the relevant properties when using its Font.FromLogFont()
		//note that whether this is rotated clockwise or CCW might affect how you have to position the text (right-aligned sometimes?, up or down by the height of the font?)
		public static IntPtr CreateRotatedHFont(Font font, bool cw)
		{
			LOGFONT logF = new LOGFONT();
			font.ToLogFont(logF);
			logF.lfEscapement = cw ? 2700 : 900;
			logF.lfOrientation = logF.lfEscapement;
			logF.lfOutPrecision = (byte)FontPrecision.OUT_TT_ONLY_PRECIS;

			var ret = CreateFontIndirect(logF);
			return ret;
		}

		// TODO: this should go away and be abstracted internally
		public static void DestroyHFont(IntPtr hFont)
		{
			DeleteObject(hFont);
		}

		public void PrepDrawString(Font font, Color color, bool rotate = false)
		{
			var fontEntry = GetCachedHFont(font);
			SetGraphicsMode(CurrentHdc, 2); // shouldn't be necessary.. cant hurt
			SelectObject(CurrentHdc, rotate ? fontEntry.RotatedHFont : fontEntry.HFont);
			SetTextColor(color);
		}

		// Set the text color of the device  context
		private void SetTextColor(Color color)
		{
			int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
			SetTextColor(CurrentHdc, rgb);
		}

		public void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect)
		{
			Rectangle(CurrentHdc, nLeftRect, nTopRect, nRightRect, nBottomRect);
		}

		public void SetBrush(Color color)
		{
			if (_brushCache.ContainsKey(color))
			{
				_currentBrush = _brushCache[color];
			}
			else
			{
				int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
				var newBrush = CreateSolidBrush(rgb);
				_brushCache.Add(color, newBrush);
				_currentBrush = newBrush;
			}
		}

		public void FillRectangle(int x, int y, int w, int h)
		{
			var r = new GDIRect(new Rectangle(x, y, w, h));
			FillRect(CurrentHdc, ref r, _currentBrush);
		}

		public void SetSolidPen(Color color)
		{
			int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
			SelectObject(CurrentHdc, GetStockObject((int)PaintObjects.DC_PEN));
			SetDCPenColor(CurrentHdc, rgb);
		}

		public void Line(int x1, int y1, int x2, int y2)
		{
			MoveToEx(CurrentHdc, x1, y1, IntPtr.Zero);
			LineTo(CurrentHdc, x2, y2);
		}

		private IntPtr CurrentHdc => _bitHdc != IntPtr.Zero ? _bitHdc : _hdc;

		private IntPtr _bitMap = IntPtr.Zero;
		private IntPtr _bitHdc = IntPtr.Zero;
		private int _bitW;
		private int _bitH;

		private void StartOffScreenBitmap(int width, int height)
		{
			_bitW = width;
			_bitH = height;

			_bitHdc = CreateCompatibleDC(_hdc);
			_bitMap = CreateCompatibleBitmap(_hdc, width, height);
			SelectObject(_bitHdc, _bitMap);
			SetBkMode(_bitHdc, BkModes.TRANSPARENT);
		}

		private void EndOffScreenBitmap()
		{
			_bitW = 0;
			_bitH = 0;
			
			DeleteObject(_bitMap);
			DeleteObject(_bitHdc);

			_bitHdc = IntPtr.Zero;
			_bitMap = IntPtr.Zero;
		}

		private  void CopyToScreen()
		{
			BitBlt(_hdc, 0, 0, _bitW, _bitH, _bitHdc, 0, 0, 0x00CC0020);
		}

		#endregion

		#region Helpers

		// Set a resource (e.g. a font) for the current device context.
		private void SetFont(Font font)
		{
			var blah = GetCachedHFont(font);
			SelectObject(CurrentHdc, blah.HFont);
		}

		private FontCacheEntry GetCachedHFont(Font font)
		{
			FontCacheEntry fontEntry;
			var result = _fontsCache.TryGetValue(font, out fontEntry);
			if (!result)
			{
				// Hack! The 6 is hardcoded to make tastudio look like taseditor, because taseditor is so perfect and wonderful
				fontEntry = new FontCacheEntry
				{
					HFont = CreateNormalHFont(font, 6),
					RotatedHFont = CreateRotatedHFont(font, true)
				};
				_fontsCache.Add(font, fontEntry);
			}

			return fontEntry;
		}

		#endregion

		#region Imports

		// ReSharper disable IdentifierTypo
		[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr CreateFontIndirect(
			[In, MarshalAs(UnmanagedType.LPStruct)]LOGFONT lplf
			);

		[DllImport("gdi32.dll")]
		private static extern int Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("user32.dll")]
		private static extern int FillRect(IntPtr hdc, [In] ref GDIRect lprc, IntPtr hbr);

		[DllImport("gdi32.dll")]
		private static extern int SetBkMode(IntPtr hdc, BkModes mode);

		[DllImport("gdi32.dll")]

		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);

		[DllImport("gdi32.dll")]
		private static extern int SetTextColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll", EntryPoint = "GetTextExtentPoint32W")]
		private static extern int GetTextExtentPoint32(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Size size);

		[DllImport("gdi32.dll", EntryPoint = "TextOutW")]
		private static extern bool TextOut(IntPtr hdc, int x, int y, [MarshalAs(UnmanagedType.LPWStr)] string str, int len);

		[DllImport("gdi32.dll")]
		public static extern int SetGraphicsMode(IntPtr hdc, int iMode);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateSolidBrush(int color);

		[DllImport("gdi32.dll")]
		private static extern IntPtr MoveToEx(IntPtr hdc, int x, int y, IntPtr point);

		[DllImport("gdi32.dll")]
		private static extern IntPtr LineTo(IntPtr hdc, int nXEnd, int nYEnd);

		[DllImport("gdi32.dll")]
		private static extern IntPtr GetStockObject(int fnObject);

		[DllImport("gdi32.dll")]
		private static extern IntPtr SetDCPenColor(IntPtr hdc, int crColor);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
		public static extern bool DeleteDC([In] IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

		[DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

		[DllImport("gdi32.dll", EntryPoint = "GdiAlphaBlend")]
		static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, BLENDFUNCTION blendFunction);

		// ReSharper disable InconsistentNaming
		// ReSharper disable UnusedMember.Global
		// ReSharper disable UnusedMember.Local
		// ReSharper disable NotAccessedField.Local
		// ReSharper disable ArrangeTypeMemberModifiers
		public enum FontPrecision : byte
		{
			
			OUT_DEFAULT_PRECIS = 0,
			OUT_STRING_PRECIS = 1,
			OUT_CHARACTER_PRECIS = 2,
			OUT_STROKE_PRECIS = 3,
			OUT_TT_PRECIS = 4,
			OUT_DEVICE_PRECIS = 5,
			OUT_RASTER_PRECIS = 6,
			OUT_TT_ONLY_PRECIS = 7,
			OUT_OUTLINE_PRECIS = 8,
			OUT_SCREEN_OUTLINE_PRECIS = 9,
			OUT_PS_ONLY_PRECIS = 10,
		}

		// It is important for this to be the right declaration
		// See more here http://www.tech-archive.net/Archive/DotNet/microsoft.public.dotnet.framework.drawing/2004-04/0319.html
		// If it's wrong (I had a wrong one from pinvoke.net) then ToLogFont will fail mysteriously
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		class LOGFONT
		{
			public int lfHeight = 0;
			public int lfWidth = 0;
			public int lfEscapement = 0;
			public int lfOrientation = 0;
			public int lfWeight = 0;
			public byte lfItalic = 0;
			public byte lfUnderline = 0;
			public byte lfStrikeOut = 0;
			public byte lfCharSet = 0;
			public byte lfOutPrecision = 0;
			public byte lfClipPrecision = 0;
			public byte lfQuality = 0;
			public byte lfPitchAndFamily = 0;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string lfFaceName = null;
		} 

		/// <summary>
		///   The graphics mode that can be set by SetGraphicsMode.
		/// </summary>
		public enum GraphicsMode : int
		{
			/// <summary>
			///   Sets the graphics mode that is compatible with 16-bit Windows. This is the default mode. If
			///   this value is specified, the application can only modify the world-to-device transform by
			///   calling functions that set window and viewport extents and origins, but not by using
			///   SetWorldTransform or ModifyWorldTransform; calls to those functions will fail.
			///   Examples of functions that set window and viewport extents and origins are SetViewportExtEx
			///   and SetWindowExtEx.
			/// </summary>
			GM_COMPATIBLE = 1,
			/// <summary>
			///   Sets the advanced graphics mode that allows world transformations. This value must be
			///   specified if the application will set or modify the world transformation for the specified
			///   device context. In this mode all graphics, including text output, fully conform to the
			///   world-to-device transformation specified in the device context.
			/// </summary>
			GM_ADVANCED = 2,
		}

		/// <summary>
		///   The XFORM structure specifies a world-space to page-space transformation.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct XFORM
		{
			public float eM11;
			public float eM12;
			public float eM21;
			public float eM22;
			public float eDx;
			public float eDy;

			public XFORM(float eM11, float eM12, float eM21, float eM22, float eDx, float eDy)
			{
				this.eM11 = eM11;
				this.eM12 = eM12;
				this.eM21 = eM21;
				this.eM22 = eM22;
				this.eDx = eDx;
				this.eDy = eDy;
			}

			/// <summary>
			///   Allows implicit conversion to a managed transformation matrix.
			/// </summary>
			public static implicit operator System.Drawing.Drawing2D.Matrix(XFORM xf)
			{
				return new System.Drawing.Drawing2D.Matrix(xf.eM11, xf.eM12, xf.eM21, xf.eM22, xf.eDx, xf.eDy);
			}

			/// <summary>
			///   Allows implicit conversion from a managed transformation matrix.
			/// </summary>
			public static implicit operator XFORM(System.Drawing.Drawing2D.Matrix m)
			{
				float[] elems = m.Elements;
				return new XFORM(elems[0], elems[1], elems[2], elems[3], elems[4], elems[5]);
			}
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct BLENDFUNCTION
		{
			byte BlendOp;
			byte BlendFlags;
			byte SourceConstantAlpha;
			byte AlphaFormat;

			public BLENDFUNCTION(byte op, byte flags, byte alpha, byte format)
			{
				BlendOp = op;
				BlendFlags = flags;
				SourceConstantAlpha = alpha;
				AlphaFormat = format;
			}
		}

		const byte AC_SRC_OVER = 0x00;
		const byte AC_SRC_ALPHA = 0x01;

		#endregion

		#region Classes, Structs, and Enums

		private class GdiGraphicsLock : IDisposable
		{
			private readonly GdiRenderer _gdi;

			public GdiGraphicsLock(GdiRenderer gdi)
			{
				_gdi = gdi;
			}

			public void Dispose()
			{
				_gdi.CopyToScreen();
				_gdi.EndOffScreenBitmap();
				_gdi._g.ReleaseHdc(_gdi._hdc);
				_gdi._hdc = IntPtr.Zero;
				_gdi._g = null;
			}
		}

		private struct GDIRect
		{
			private int left;
			private int top;
			private int right;
			private int bottom;

			public GDIRect(Rectangle r)
			{
				left = r.Left;
				top = r.Top;
				bottom = r.Bottom;
				right = r.Right;
			}
		}

		private enum PaintObjects
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

		private enum BkModes : int
		{
			TRANSPARENT = 1,
			OPAQUE = 2
		}

		#endregion
	}
}
