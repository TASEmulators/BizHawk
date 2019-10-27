using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
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
		private readonly VScrollBar _vBar = new VScrollBar { Visible = false };
		private Size _charSize;
		private readonly int _cellPadding;
		private long _rowSize = 16;
		private long _arraySize;

		private int CellMargin => _charSize.Width + _cellPadding;

		private int NumDigits => DataSize * 2;
		private int NumAddressDigits => ArrayLength.NumHexDigits();
		private int AddressBarWidth => (Padding.Left * 2) + (ArrayLength.NumHexDigits() * _charSize.Width) + CellMargin;
		private int CellWidth => (NumDigits * _charSize.Width) + _cellPadding;
		private int CellHeight => _charSize.Height + Padding.Top + Padding.Bottom;
		private int VisibleRows => (Height / CellHeight) - 1;
		private long TotalRows => ArrayLength / 16;
		private int FirstVisibleRow => _vBar.Value;
		private int LastVisibleRow => FirstVisibleRow + VisibleRows;

		public HexView()
		{
			_font = new Font(FontFamily.GenericMonospace, 9);  // Only support fixed width

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
				// Measure the font. There seems to be some extra horizontal padding on the first
				// character so we'll see how much the width increases on the second character.
				var s1 = _renderer.MeasureString("0", _font);
				var s2 = _renderer.MeasureString("00", _font);
				_charSize = new Size(s2.Width - s1.Width, s1.Height);
				_cellPadding = s1.Width * 2 - s2.Width; // The padding applied to the first digit;
			}

			_vBar.Anchor =  AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
			_vBar.Location = new Point(0 - _vBar.Width, 0);
			_vBar.Height = Height;
			_vBar.SmallChange = 1;
			_vBar.LargeChange = 16;
			Controls.Add(_vBar);
			_vBar.ValueChanged += VerticalBar_ValueChanged;
			RecalculateScrollBars();
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
				_renderer.SetBrush(SystemColors.Control);
				_renderer.SetSolidPen(SystemColors.Control);
				_renderer.FillRectangle(0, 0, Width, Height);

				_renderer.SetBrush(_foreColor);
				_renderer.SetSolidPen(_foreColor);
				_renderer.PrepDrawString(_font, _foreColor);

				DrawLines();
				DrawHeader();
				DrawAddressBar();
				DrawValues();

				// Debug
				_renderer.DrawString(_vBar.Value.ToString(), new Point(0, 0));
			}
		}

		private void DrawLines()
		{
			_renderer.Line(AddressBarWidth, CellHeight + Padding.Top, AddressBarWidth, Height);
			_renderer.Line(AddressBarWidth, CellHeight + Padding.Top, Width, CellHeight + Padding.Top);
		}

		private void DrawHeader()
		{
			var x = AddressBarWidth + CellMargin;
			var y = Padding.Top;
			var sb = new StringBuilder();
			for (int i = 0; i < _rowSize; i += DataSize)
			{
				
				sb
					.Append(i.ToHexString(NumDigits))
					.Append(" ");
			}

			_renderer.DrawString(sb.ToString(), new Point(x, y));
		}

		private void DrawAddressBar()
		{ 
			for (int i = 0; i <= VisibleRows; i++)
			{
				var addr = ((FirstVisibleRow + i) * 16);
				if (addr <= ArrayLength - 16)
				{
					int x = Padding.Left;
					int y = (i * CellHeight) + CellHeight + Padding.Top;
					var str = addr.ToHexString(NumAddressDigits);
					_renderer.DrawString(str, new Point(x, y));
				}
			}
		}

		private void DrawValues()
		{
			if (QueryIndexValue != null)
			{
				for (int i = 0; i <= VisibleRows; i++)
				{
					var x = AddressBarWidth + CellMargin;
					var y = (i * CellHeight) + CellHeight + Padding.Top;

					var sb = new StringBuilder();
					
					for (int j = 0; j < 16; j += DataSize)
					{
						var addr = ((FirstVisibleRow + i) * 16) + j;
					
						if (addr < ArrayLength)
						{
							QueryIndexValue(addr, DataSize, out long value);
							sb
								.Append(value.ToHexString(NumDigits))
								.Append(" ");
						}
					}

					_renderer.DrawString(sb.ToString(), new Point(x, y));
				}
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the total length of the data set to edit
		/// </summary>
		[Category("Behavior")]
		public long ArrayLength
		{
			get => _arraySize;
			set
			{
				_arraySize = value;
				RecalculateScrollBars();
			}
		}

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

		public delegate void QueryIndexValueHandler(int index, int dataSize, out long value);

		public delegate void QueryIndexBkColorHandler(int index, ref Color color);

		public delegate void QueryIndexForeColorHandler(int index, ref Color color);

		#endregion

		private void RecalculateScrollBars()
		{
			_vBar.Visible = TotalRows > VisibleRows;
			_vBar.Minimum = 0;
			_vBar.Maximum = (int)TotalRows - 1;
			_vBar.Refresh();
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			Refresh();
		}
	}
}
