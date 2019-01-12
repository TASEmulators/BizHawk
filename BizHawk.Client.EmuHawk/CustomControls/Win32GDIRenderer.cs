using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public sealed class Win32GDIRenderer : GDI.GDIRenderer
	{
		/// <summary>
		/// used for <see cref="MeasureString(string, System.Drawing.Font, float, out int, out int)"/> calculation.
		/// </summary>
		private static readonly int[] CharFit = new int[1];

		/// <summary>
		/// used for <see cref="MeasureString(string, System.Drawing.Font,float, out int, out int)"/> calculation
		/// </summary>
		private static readonly int[] CharFitWidth = new int[1000];

		/// <summary>
		/// Cache of all the HFONTs used, rather than create them again and again
		/// </summary>
		private readonly Dictionary<Font, FontCacheEntry> FontsCache = new Dictionary<Font, FontCacheEntry>();

		class FontCacheEntry
		{
			public IntPtr HFont;
		}

		/// <summary>
		/// Cache of all the brushes used, rather than create them again and again
		/// </summary>
		private readonly Dictionary<Color, IntPtr> BrushCache = new Dictionary<Color, IntPtr>();

		private Graphics _g;
		private IntPtr _hdc;
		private IntPtr _currentBrush = IntPtr.Zero;

		#region Construct and Destroy

		public Win32GDIRenderer()
		{
			//zero 04-16-2016 : this can't be legal, theres no HDC yet
			//SetBkMode(_hdc, GDI.BkModes.OPAQUE);
		}

		public void Dispose()
		{
			foreach (var brush in BrushCache)
			{
				if (brush.Value != IntPtr.Zero)
				{
					DeleteObject(brush.Value);
				}
			}

			foreach (var fc in FontsCache)
				DeleteObject(fc.Value.HFont);

			EndOffScreenBitmap();

			System.Diagnostics.Debug.Assert(_hdc == IntPtr.Zero, "Disposed a Win32GDIRenderer while it held an HDC");
			System.Diagnostics.Debug.Assert(_g == null, "Disposed a Win32GDIRenderer while it held a Graphics");
		}

		#endregion

		#region Api

		/// <summary>
		/// Draw a bitmap object at the given position
		/// </summary>
		public void DrawBitmap(Bitmap bitmap, Point point, bool blend = false)
		{
			IntPtr hbmp = bitmap.GetHbitmap();
			var bitHDC = CreateCompatibleDC(CurrentHDC);
			IntPtr old = SelectObject(bitHDC, hbmp);
			if (blend)
			{
				AlphaBlend(CurrentHDC, point.X, point.Y, bitmap.Width, bitmap.Height, bitHDC, 0, 0, bitmap.Width, bitmap.Height, new BLENDFUNCTION(AC_SRC_OVER, 0, 0xff, AC_SRC_ALPHA));
			}
			else
			{
				BitBlt(CurrentHDC, point.X, point.Y, bitmap.Width, bitmap.Height, bitHDC, 0, 0, 0xCC0020);
			}
			SelectObject(bitHDC, old);
			DeleteDC(bitHDC);
			DeleteObject(hbmp);
		}

		/// <summary>
		/// Required to use before calling drawing methods
		/// </summary>
		public GDI.GDIGraphicsLock<GDI.GDIRenderer> LockGraphics(Graphics g)
		{
			_g = g;
			_hdc = g.GetHdc();
			SetBkMode(_hdc, GDI.BkModes.TRANSPARENT);
//			return (GDI.GDIGraphicsLock<GDI.GDIRenderer>) new GDI.GDIGraphicsLock<Win32GDIRenderer>(this);
			// going to need this explained to me, the below works as expected but the above does not --Yoshi
			return new GDI.GDIGraphicsLock<GDI.GDIRenderer>((GDI.GDIRenderer) this);
		}

		/// <summary>
		/// Measure the width and height of string <paramref name="str"/> when drawn on device context HDC
		/// using the given font <paramref  name="font"/>
		/// </summary>
		public Size MeasureString(string str, Font font)
		{
			SetFont(font);

			var size = new Size();
			GetTextExtentPoint32(CurrentHDC, str, str.Length, ref size);
			return size;
		}

		/// <summary>
		/// Measure the width and height of string <paramref name="str"/> when drawn on device context HDC
		/// using the given font <paramref  name="font"/>
		/// Restrict the width of the string and get the number of characters able to fit in the restriction and
		/// the width those characters take
		/// </summary>
		/// <param name="maxWidth">the max width to render the string  in</param>
		/// <param name="charFit">the number of characters that will fit under  <see cref="maxWidth"/> restriction</param>
		/// <param name="charFitWidth"></param>
		public Size MeasureString(string str, Font font, float maxWidth, out int charFit, out int charFitWidth)
		{
			SetFont(font);

			var size = new Size();
			GetTextExtentExPoint(CurrentHDC, str, str.Length, (int)Math.Round(maxWidth), CharFit, CharFitWidth, ref size);
			charFit = CharFit[0];
			charFitWidth = charFit > 0 ? CharFitWidth[charFit - 1] : 0;
			return size;
		}

		public void DrawString(string str, Point point)
		{
			TextOut(CurrentHDC, point.X, point.Y, str, str.Length);
		}

		public static IntPtr CreateNormalHFont(Font font, int width)
		{
			LOGFONT logf = new LOGFONT();
			font.ToLogFont(logf);
			logf.lfWidth = width;
			logf.lfOutPrecision = (byte)FontPrecision.OUT_TT_ONLY_PRECIS;
			var ret = CreateFontIndirect(logf);
			return ret;
		}

		//this returns an IntPtr HFONT because .net's Font class will erase the relevant properties when using its Font.FromLogFont()
		//note that whether this is rotated clockwise or CCW might affect how you have to position the text (right-aligned sometimes?, up or down by the height of the font?)
		public static IntPtr CreateRotatedHFont(Font font, bool CW)
		{
			LOGFONT logf = new LOGFONT();
			font.ToLogFont(logf);
			logf.lfEscapement = CW ? 2700 : 900;
			logf.lfOrientation = logf.lfEscapement;
			logf.lfOutPrecision = (byte)FontPrecision.OUT_TT_ONLY_PRECIS;

			//this doesnt work! .net erases the relevant propreties.. it seems?
			//return Font.FromLogFont(logf);

			var ret = CreateFontIndirect(logf);
			return ret;
		}

		public static void DestroyHFont(IntPtr hfont)
		{
			DeleteObject(hfont);
		}

		public void PrepDrawString(IntPtr hfont, Color color)
		{
			SetGraphicsMode(CurrentHDC, 2); //shouldnt be necessary.. cant hurt
			SelectObject(CurrentHDC, hfont);
			SetTextColor(color);
		}

		public void PrepDrawString(Font font, Color color)
		{
			SetFont(font);
			SetTextColor(color);
		}


		/// <summary>
		/// Draw the given string using the given  font and foreground color at given location
		/// See [http://msdn.microsoft.com/en-us/library/windows/desktop/dd162498(v=vs.85).aspx][15]
		/// </summary>
		public void DrawString(string str, Font font, Color color, Rectangle rect, GDI.TextFormatFlags flags)
		{
			SetFont(font);
			SetTextColor(color);

			var rect2 = new Rect(rect);
			DrawText(CurrentHDC, str, str.Length, ref  rect2, (uint)flags);
		}


		/// <summary>
		/// Set the text color of the device  context
		/// </summary>
		public void SetTextColor(Color color)
		{
			int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
			SetTextColor(CurrentHDC, rgb);
		}

		public void SetBackgroundColor(Color color)
		{
			int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
			SetBkColor(CurrentHDC, rgb);
		}

		public void DrawRectangle(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect)
		{
			Rectangle(CurrentHDC, nLeftRect, nTopRect, nRightRect, nBottomRect);
		}

		public void SetBrush(Color color)
		{
			if (BrushCache.ContainsKey(color))
			{
				_currentBrush = BrushCache[color];
			}
			else
			{
				int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
				var newBrush = CreateSolidBrush(rgb);
				BrushCache.Add(color, newBrush);
				_currentBrush = newBrush;
			}
		}

		public void FillRectangle(int x, int y, int w, int h)
		{
			var r = new GDIRect(new Rectangle(x, y, w, h));
			FillRect(CurrentHDC, ref r, _currentBrush);
		}

		public void SetPenPosition(int x, int y)
		{
			MoveToEx(CurrentHDC, x, y, IntPtr.Zero);
		}

		public void SetSolidPen(Color color)
		{
			int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
			SelectObject(CurrentHDC, GetStockObject((int)GDI.PaintObjects.DC_PEN));
			SetDCPenColor(CurrentHDC, rgb);
		}

		public void Line(int x1, int y1, int x2, int y2)
		{
			MoveToEx(CurrentHDC, x1, y1, IntPtr.Zero);
			LineTo(CurrentHDC, x2, y2);
		}

		private IntPtr CurrentHDC
		{
			get { return _bitHDC != IntPtr.Zero ? _bitHDC : _hdc; }
		}

		private IntPtr _bitMap = IntPtr.Zero;
		private IntPtr _bitHDC = IntPtr.Zero;
		private int _bitW;
		private int _bitH;

		public void StartOffScreenBitmap(int width, int height)
		{
			_bitW = width;
			_bitH = height;

			_bitHDC = CreateCompatibleDC(_hdc);
			_bitMap = CreateCompatibleBitmap(_hdc, width, height);
			SelectObject(_bitHDC, _bitMap);
			SetBkMode(_bitHDC, GDI.BkModes.TRANSPARENT);
		}

		public void EndOffScreenBitmap()
		{
			_bitW = 0;
			_bitH = 0;

			DeleteObject(_bitMap);
			DeleteObject(_bitHDC);

			_bitHDC = IntPtr.Zero;
			_bitMap = IntPtr.Zero;
		}

		public void CopyToScreen()
		{
			BitBlt(_hdc, 0, 0, _bitW, _bitH, _bitHDC, 0, 0, 0x00CC0020);
		}

		public void HackDisposeGraphics()
		{
			_g.ReleaseHdc(_hdc);
			_hdc = IntPtr.Zero;
			_g = null;
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Set a resource (e.g. a font) for the  specified device context.
		/// </summary>
		private void SetFont(Font font)
		{
			SelectObject(CurrentHDC, GetCachedHFont(font));
		}

		private IntPtr GetCachedHFont(Font font)
		{
			//the original code struck me as bad. attempting to ID fonts by picking a subset of their fields is not gonna work.
			//don't call this.Font in InputRoll.cs, it is probably slow.
			//consider Fonts to be a jealously guarded resource (they need to be disposed, after all) and manage them carefully.
			//this cache maintains the HFONTs only.
			FontCacheEntry ce;
			if (!FontsCache.TryGetValue(font, out ce))
			{
				FontsCache[font] = ce = new FontCacheEntry();
				ce.HFont = font.ToHfont();
			}
			return ce.HFont;
		}

		#endregion

		#region Imports

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern IntPtr BeginPaint(IntPtr hWnd, ref IntPtr lpPaint);

		[DllImport("user32.dll")]
		private static extern IntPtr EndPaint(IntPtr hWnd, IntPtr lpPaint);

		[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr CreateFontIndirect(
			[In, MarshalAs(UnmanagedType.LPStruct)]LOGFONT lplf
			);

		[DllImport("gdi32.dll")]
		private static extern int Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("user32.dll")]
		private static extern int FillRect(IntPtr hdc, [In] ref GDIRect lprc, IntPtr hbr);

		[DllImport("gdi32.dll")]
		private static extern int SetBkMode(IntPtr hdc, GDI.BkModes mode);

		[DllImport("gdi32.dll")]
		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);

		[DllImport("gdi32.dll")]
		private static extern int SetTextColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll")]
		private static extern int SetBkColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll", EntryPoint = "GetTextExtentPoint32W")]
		private static extern int GetTextExtentPoint32(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Size size);

		[DllImport("gdi32.dll", EntryPoint = "GetTextExtentExPointW")]
		private static extern bool GetTextExtentExPoint(IntPtr hDc, [MarshalAs(UnmanagedType.LPWStr)]string str, int nLength, int nMaxExtent, int[] lpnFit, int[] alpDx, ref Size size);

		[DllImport("gdi32.dll", EntryPoint = "TextOutW")]
		private static extern bool TextOut(IntPtr hdc, int x, int y, [MarshalAs(UnmanagedType.LPWStr)] string str, int len);

		[DllImport("gdi32.dll")]
		public static extern int SetGraphicsMode(IntPtr hdc, int iMode);

		[DllImport("user32.dll", EntryPoint = "DrawTextW")]
		private static extern int DrawText(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Rect rect, uint uFormat);

		[DllImport("gdi32.dll", EntryPoint = "ExtTextOutW")]
		private static extern bool ExtTextOut(IntPtr hdc, int X, int Y, uint fuOptions, uint cbCount, [In] IntPtr lpDx);

		[DllImport("gdi32.dll")]
		static extern bool SetWorldTransform(IntPtr hdc, [In] ref XFORM lpXform);

		[DllImport("gdi32.dll")]
		private static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateSolidBrush(int color);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int color);

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

		public enum FontWeight : int
		{
			FW_DONTCARE = 0,
			FW_THIN = 100,
			FW_EXTRALIGHT = 200,
			FW_LIGHT = 300,
			FW_NORMAL = 400,
			FW_MEDIUM = 500,
			FW_SEMIBOLD = 600,
			FW_BOLD = 700,
			FW_EXTRABOLD = 800,
			FW_HEAVY = 900,
		}
		public enum FontCharSet : byte
		{
			ANSI_CHARSET = 0,
			DEFAULT_CHARSET = 1,
			SYMBOL_CHARSET = 2,
			SHIFTJIS_CHARSET = 128,
			HANGEUL_CHARSET = 129,
			HANGUL_CHARSET = 129,
			GB2312_CHARSET = 134,
			CHINESEBIG5_CHARSET = 136,
			OEM_CHARSET = 255,
			JOHAB_CHARSET = 130,
			HEBREW_CHARSET = 177,
			ARABIC_CHARSET = 178,
			GREEK_CHARSET = 161,
			TURKISH_CHARSET = 162,
			VIETNAMESE_CHARSET = 163,
			THAI_CHARSET = 222,
			EASTEUROPE_CHARSET = 238,
			RUSSIAN_CHARSET = 204,
			MAC_CHARSET = 77,
			BALTIC_CHARSET = 186,
		}
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
		public enum FontClipPrecision : byte
		{
			CLIP_DEFAULT_PRECIS = 0,
			CLIP_CHARACTER_PRECIS = 1,
			CLIP_STROKE_PRECIS = 2,
			CLIP_MASK = 0xf,
			CLIP_LH_ANGLES = (1 << 4),
			CLIP_TT_ALWAYS = (2 << 4),
			CLIP_DFA_DISABLE = (4 << 4),
			CLIP_EMBEDDED = (8 << 4),
		}
		public enum FontQuality : byte
		{
			DEFAULT_QUALITY = 0,
			DRAFT_QUALITY = 1,
			PROOF_QUALITY = 2,
			NONANTIALIASED_QUALITY = 3,
			ANTIALIASED_QUALITY = 4,
			CLEARTYPE_QUALITY = 5,
			CLEARTYPE_NATURAL_QUALITY = 6,
		}
		[Flags]
		public enum FontPitchAndFamily : byte
		{
			DEFAULT_PITCH = 0,
			FIXED_PITCH = 1,
			VARIABLE_PITCH = 2,
			FF_DONTCARE = (0 << 4),
			FF_ROMAN = (1 << 4),
			FF_SWISS = (2 << 4),
			FF_MODERN = (3 << 4),
			FF_SCRIPT = (4 << 4),
			FF_DECORATIVE = (5 << 4),
		}

		//it is important for this to be the right declaration
		//see more here http://www.tech-archive.net/Archive/DotNet/microsoft.public.dotnet.framework.drawing/2004-04/0319.html
		//if it's wrong (I had a wrong one from pinvoke.net) then ToLogFont will fail mysteriously
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
			///   Allows implicit converstion to a managed transformation matrix.
			/// </summary>
			public static implicit operator System.Drawing.Drawing2D.Matrix(XFORM xf)
			{
				return new System.Drawing.Drawing2D.Matrix(xf.eM11, xf.eM12, xf.eM21, xf.eM22, xf.eDx, xf.eDy);
			}

			/// <summary>
			///   Allows implicit converstion from a managed transformation matrix.
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

		[DllImport("gdi32.dll")]
		static extern int SetBitmapBits(IntPtr hbmp, uint cBytes, byte[] lpBits);

		#endregion

		#region Classes, Structs, and Enums

		private struct Rect
		{
			private int _left;
			private int _top;
			private int _right;
			private int _bottom;

			public Rect(Rectangle r)
			{
				_left = r.Left;
				_top = r.Top;
				_bottom = r.Bottom;
				_right = r.Right;
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

		#endregion
	}
}
