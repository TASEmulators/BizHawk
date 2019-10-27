using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk
{
	public class HexView : Control
	{
		private readonly IControlRenderer _renderer;
		private readonly Font _font;
		private readonly Color _foreColor = Color.Black;

		private Size _charSize;

		private long _rowSize = 16;

		private int NumDigits => DataSize * 2;
		private int AddressBarWidth => (Padding.Left * 2) + (ArrayLength.NumHexDigits() * _charSize.Width) + _charSize.Width;
		private int CellWidth => NumDigits * _charSize.Width;
		public HexView()
		{
			_font = new Font("Courier New", 8);  // Only support fixed width

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
			{
				//_renderer = new GdiRenderer(); // TODO
				_renderer = new GdiPlusRenderer();
			}
			else
			{
				_renderer = new GdiPlusRenderer();
			}

			using (var g = CreateGraphics())
			using (_renderer.LockGraphics(g, Width, Height))
			{
				_charSize = _renderer.MeasureString("A", _font); // TODO make this a property so changing it updates other values.
			}
		}

		protected override void Dispose(bool disposing)
		{
			_renderer.Dispose();
			_font.Dispose();

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

				DrawHeader();
			}
		}

		private void DrawHeader()
		{
			_renderer.PrepDrawString(_font, _foreColor);
			for (int i = 0; i < _rowSize; i += DataSize)
			{
				var x = AddressBarWidth + ((i / DataSize) * CellWidth) + _charSize.Width;
				var y = Padding.Top;
				var str = i.ToHexString(NumDigits);
				_renderer.DrawString(str, new Point(x, y));
			}
		}

		#endregion

		#region Properties
		
		/// <summary>
		/// Gets or sets the total length of the data set to edit
		/// </summary>
		[Category("Behavior")]
		public long ArrayLength { get; set; }

		/// <summary>
		/// Gets or sets the number of bytes each cell represents
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(1)]
		public int DataSize { get; set; } = 1;

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
