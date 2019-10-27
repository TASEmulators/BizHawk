using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public class HexView : Control
	{
		private readonly IControlRenderer _renderer;
		private readonly Font _normalFont;
		private Size _charSize;

		private long _arrayLength;

		public HexView()
		{
			_normalFont = new Font("Courier New", 8);  // Only support fixed width

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
			{
				_renderer = new GdiRenderer();
			}
			else
			{
				_renderer = new GdiPlusRenderer();
			}

			using (var g = CreateGraphics())
			using (var lck = _renderer.LockGraphics(g, Width, Height))
			{
				_charSize = _renderer.MeasureString("A", _normalFont); // TODO make this a property so changing it updates other values.
			}
		}

		protected override void Dispose(bool disposing)
		{
			_renderer.Dispose();
			_normalFont.Dispose();

			base.Dispose(disposing);
		}

		#region Paint

		protected override void OnPaint(PaintEventArgs e)
		{
			using (_renderer.LockGraphics(e.Graphics, Width, Height))
			{
				// White Background
				_renderer.SetBrush(Color.White);
				_renderer.SetSolidPen(Color.White);
				_renderer.FillRectangle(0, 0, Width, Height);


				_renderer.DrawString("Hello World", new Point(10, 10));
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
			get => _arrayLength;

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
