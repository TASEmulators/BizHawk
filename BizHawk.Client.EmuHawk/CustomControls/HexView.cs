using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	public class HexView : Control
	{
		private readonly GDI.GDIRenderer Gdi;
		private readonly Font NormalFont;
		private Size _charSize;

		private long _arrayLength;

		public HexView()
		{
			NormalFont = new Font("Courier New", 8);  // Only support fixed width

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			Gdi = new Win32GDIRenderer();

			using (var g = CreateGraphics())
			using (var LCK = Gdi.LockGraphics(g))
			{
				_charSize = Gdi.MeasureString("A", NormalFont); // TODO make this a property so changing it updates other values.
			}
		}

		protected override void Dispose(bool disposing)
		{
			Gdi.Dispose();

			NormalFont.Dispose();

			base.Dispose(disposing);
		}

		#region Paint

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var LCK = Gdi.LockGraphics(e.Graphics))
			{
				Gdi.StartOffScreenBitmap(Width, Height);

				// White Background
				Gdi.SetBrush(Color.White);
				Gdi.SetSolidPen(Color.White);
				Gdi.FillRectangle(0, 0, Width, Height);


				Gdi.DrawString("Hello World", new Point(10, 10));

				Gdi.CopyToScreen();
				Gdi.EndOffScreenBitmap();
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the sets the virtual number of the length of the array to display
		/// </summary>
		[Category("Behavior")]
		public long ArrayLength
		{
			get
			{
				return _arrayLength;
			}

			set
			{
				_arrayLength = value;
				RecalculateScrollBars();
			}
		}

		#endregion

		#region Event Handlers

		[Category("Virtual")]
		public event QueryIndexValueHandler QueryIndexValue;

		[Category("Virtual")]
		public event QueryIndexBkColorHandler QueryIndexBgColor;

		[Category("Virtual")]
		public event QueryIndexForeColorHandler QueryIndexForeColor;

		public delegate void QueryIndexValueHandler(int index, out long value);

		public delegate void QueryIndexBkColorHandler(int index, ref Color color);

		public delegate void QueryIndexForeColorHandler(int index, ref Color color);

		#endregion

		private void RecalculateScrollBars()
		{
		}
	}
}
