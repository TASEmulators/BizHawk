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
	public sealed class GDIRenderer : IDisposable
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
		/// Cache of all the fonts used, rather than create them again and again
		/// </summary>
		private static readonly Dictionary<string, Dictionary<float, Dictionary<FontStyle, IntPtr>>> FontsCache = new Dictionary<string, Dictionary<float, Dictionary<FontStyle, IntPtr>>>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Cache of all the brushes used, rather than create them again and again
		/// </summary>
		private readonly Dictionary<Color, IntPtr> BrushCache = new Dictionary<Color, IntPtr>();

		private Graphics _g;
		private IntPtr _hdc;
		private IntPtr _currentBrush = IntPtr.Zero;

		#region Construct and Destroy

		public GDIRenderer()
		{
			SetBkMode(_hdc, (int)BkModes.OPAQUE);
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

			EndOffScreenBitmap();

			System.Diagnostics.Debug.Assert(_hdc == IntPtr.Zero, "Disposed a GDIRenderer while it held an HDC");
			System.Diagnostics.Debug.Assert(_g == null, "Disposed a GDIRenderer while it held a Graphics");
		}

		#endregion

		#region Api

		/// <summary>
		/// Required to use before calling drawing methods
		/// </summary>
		public GdiGraphicsLock LockGraphics(Graphics g)
		{
			_g = g;
			_hdc = g.GetHdc();
			SetBkMode(_hdc, (int)BkModes.TRANSPARENT);
			return new GdiGraphicsLock(this);
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

		public void PrepDrawString(Font font, Color color)
		{
			SetFont(font);
			SetTextColor(color);
		}

		/// <summary>
		/// Draw the given string using the given  font and foreground color at given location
		/// See [http://msdn.microsoft.com/en-us/library/windows/desktop/dd162498(v=vs.85).aspx][15]
		/// </summary>
		public void DrawString(string str, Font font, Color color, Rectangle rect, TextFormatFlags flags)
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
			SelectObject(CurrentHDC, GetStockObject((int)PaintObjects.DC_PEN));
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

		private IntPtr _bitMap = IntPtr.Zero; // TODO: dispose of this guy
		private IntPtr _bitHDC = IntPtr.Zero; // TODO: dispose of this guy
		private int _bitW;
		private int _bitH;

		public void StartOffScreenBitmap(int width, int height)
		{
			_bitW = width;
			_bitH = height;

			_bitHDC = CreateCompatibleDC(_hdc);
			_bitMap = CreateCompatibleBitmap(_hdc, width, height);
			SelectObject(_bitHDC, _bitMap);
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

		#endregion

		#region Helpers

		/// <summary>
		/// Set a resource (e.g. a font) for the  specified device context.      
		/// </summary>
		private void SetFont(Font font)
		{
			SelectObject(_hdc, GetCachedHFont(font));
		}

		private static IntPtr GetCachedHFont(Font font)
		{
			var hfont = IntPtr.Zero;
			Dictionary<float, Dictionary<FontStyle, IntPtr>> dic1;
			if (FontsCache.TryGetValue(font.Name, out dic1))
			{
				Dictionary<FontStyle, IntPtr> dic2;
				if (dic1.TryGetValue(font.Size, out  dic2))
				{
					dic2.TryGetValue(font.Style, out hfont);
				}
				else
				{
					dic1[font.Size] = new Dictionary<FontStyle, IntPtr>();
				}
			}
			else
			{
				FontsCache[font.Name] = new Dictionary<float, Dictionary<FontStyle, IntPtr>>();
				FontsCache[font.Name][font.Size] = new Dictionary<FontStyle, IntPtr>();
			}

			if (hfont == IntPtr.Zero)
			{
				FontsCache[font.Name][font.Size][font.Style] = hfont = font.ToHfont();
			}

			return hfont;
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

		[DllImport("gdi32.dll")]
		private static extern int Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("user32.dll")]
		private static extern int FillRect(IntPtr hdc, [In] ref GDIRect lprc, IntPtr hbr);

		[DllImport("gdi32.dll")]
		private static extern int SetBkMode(IntPtr hdc, int mode);

		[DllImport("gdi32.dll")]
		private static extern int SelectObject(IntPtr hdc, IntPtr hgdiObj);

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

		[DllImport("user32.dll", EntryPoint = "DrawTextW")]
		private static extern int DrawText(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Rect rect, uint uFormat);

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

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

		[DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

		#endregion

		#region Classes, Structs, and Enums

		public class GdiGraphicsLock : IDisposable
		{
			private readonly GDIRenderer Gdi;

			public GdiGraphicsLock(GDIRenderer gdi)
			{
				this.Gdi = gdi;
			}

			public void Dispose()
			{
				Gdi._g.ReleaseHdc(Gdi._hdc);
				Gdi._hdc = IntPtr.Zero;
				Gdi._g = null;
			}
		}

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

		private struct GDIPoint
		{
			private int x;
			private int y;

			private GDIPoint(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
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

		public enum BkModes
		{
			TRANSPARENT = 1,
			OPAQUE = 2
		}

		#endregion
	}
}
