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
		private readonly GDIRenderer Gdi;
		private readonly Font NormalFont;
		private Size _charSize;

		public HexView()
		{
			NormalFont = new Font("Courier New", 8);  // Only support fixed width

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			Gdi = new GDIRenderer();

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
	}
}
