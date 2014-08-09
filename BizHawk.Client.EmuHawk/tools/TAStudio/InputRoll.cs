using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	public class InputRoll : Control
	{
		private readonly GDIRenderer Gdi;
		private readonly RollColumns Columns = new RollColumns();

		private bool NeedToReDrawColumn = false;
		private int _horizontalOrientedColumnWidth = 0;
		private Size _charSize;

		public InputRoll()
		{
			CellPadding = 3;
			//SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			this.Font = new Font("Courier New", 8);  // Only support fixed width
			//BackColor = Color.Transparent;

			Gdi = new GDIRenderer(this);

			_charSize = Gdi.MeasureString("A", this.Font);
		}

		protected override void Dispose(bool disposing)
		{
			Gdi.Dispose();
			base.Dispose(disposing);
		}

		#region Properties

		/// <summary>
		/// Gets or sets the amount of padding on the text inside a cell
		/// </summary>
		[DefaultValue(3)]
		public int CellPadding { get; set; }

		// TODO: remove this, it is put here for more convenient replacing of a virtuallistview in tools with the need to refactor code
		public bool VirtualMode { get; set; }

		/// <summary>
		/// Gets or sets whether the control is horizontal or vertical
		/// </summary>
		[Category("Behavior")]
		public bool HorizontalOrientation { get; set; }

		/// <summary>
		/// Gets or sets the sets the virtual number of items to be displayed.
		/// </summary>
		[Category("Behavior")]
		public int ItemCount { get; set; }

		/// <summary>
		/// Gets or sets the sets the columns can be resized
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnResize { get; set; }

		/// <summary>
		/// Gets or sets the sets the columns can be reordered
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnReorder { get; set; }

		#endregion

		#region Event Handlers

		/// <summary>
		/// Retrieve the background color for a Listview cell (item and subitem).
		/// </summary>
		/// <param name="item">Listview item (row).</param>
		/// <param name="subItem">Listview subitem (column).</param>
		/// <param name="color">Background color to use</param>
		public delegate void QueryItemBkColorHandler(int item, int subItem, ref Color color);

		/// <summary>
		/// Retrieve the text for a Listview cell (item and subitem).
		/// </summary>
		/// <param name="item">Listview item (row).</param>
		/// <param name="subItem">Listview subitem (column).</param>
		/// <param name="text">Text to display.</param>
		public delegate void QueryItemTextHandler(int item, int subItem, out string text);

		/// <summary>
		/// Fire the QueryItemBkColor event which requests the background color for the passed Listview cell
		/// </summary>
		[Category("Virtual")] // TODO: can I make these up?
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fire the QueryItemText event which requests the text for the passed Listview cell.
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		#endregion

		#region Public Methods

		public string UserSettingsSerialized()
		{
			return string.Empty; // TODO
		}

		public void AddColumns(IEnumerable<RollColumn> columns)
		{
			Columns.AddRange(columns);
			ColumnChanged();
		}

		public void AddColumn(RollColumn column)
		{
			Columns.Add(column);
			ColumnChanged();
		}

		#endregion

		#region Paint

		private void DrawColumnBg(GDIRenderer gdi, PaintEventArgs e)
		{
			gdi.SetBrush(SystemColors.ControlLight);
			gdi.SetSolidPen(Color.Black);

			if (HorizontalOrientation)
			{
				var colWidth = _horizontalOrientedColumnWidth;
				gdi.DrawRectangle(0, 0, colWidth, Height);
				gdi.FillRectangle(1, 1, colWidth - 3, Height - 3);

				int start = 0;
				foreach (var column in Columns)
				{
					start += CellHeight;
					gdi.Line(1, start, colWidth - 1, start);
				}
			}
			else
			{
				gdi.DrawRectangle(0, 0, Width, CellHeight);
				gdi.FillRectangle(1, 1, Width - 3, CellHeight - 3);

				int start = 0;
				foreach (var column in Columns)
				{
					start += CalcWidth(column);
					gdi.Line(start, 0, start, CellHeight);
				}
			}
		}

		private void DrawBg(GDIRenderer gdi, PaintEventArgs e)
		{
			var startPoint = StartBg();

			gdi.SetBrush(Color.White);
			gdi.SetSolidPen(Color.Black);
			gdi.FillRectangle(startPoint.X, startPoint.Y, Width, Height);
			gdi.DrawRectangle(startPoint.X, startPoint.Y, Width, Height);

			gdi.SetSolidPen(SystemColors.ControlLight);
			if (HorizontalOrientation)
			{
				// Columns
				for (int i = 1; i < Width / CellWidth; i++)
				{
					var x = _horizontalOrientedColumnWidth + (i * CellWidth);
					gdi.Line(x, 1, x, Columns.Count * CellHeight);
				}

				// Rows
				for (int i = 1; i < Columns.Count + 1; i++)
				{
					gdi.Line(_horizontalOrientedColumnWidth, i * CellHeight, Width - 2, i * CellHeight);
				}
			}
			else
			{
				// Columns
				int x = 0;
				int y = CellHeight;
				foreach (var column in Columns)
				{
					x += CalcWidth(column);
					gdi.Line(x, y, x, Height - 1);
				}

				// Rows
				for (int i = 2; i < Height / CellHeight; i++)
				{
					gdi.Line(1, (i * CellHeight) + 1, Width - 2, (i * CellHeight) + 1);
				}
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing, and this should never be called
		}

		private void DrawColumnText(GDIRenderer gdi, PaintEventArgs e)
		{
			if (HorizontalOrientation)
			{
				int start = 0;
				foreach (var column in Columns)
				{
					var point = new Point(CellPadding, start + CellPadding);
					gdi.PrepDrawString(column.Text, this.Font, this.ForeColor, point);
					gdi.DrawString(column.Text, this.Font, point);
					start += CellHeight;
				}
			}
			else
			{
				int start = CellPadding;
				foreach(var column in Columns)
				{
					var point = new Point(start + CellPadding, CellPadding);
					gdi.PrepDrawString(column.Text, this.Font, this.ForeColor, point);
					gdi.DrawString(column.Text, this.Font, point);
					start += CalcWidth(column);
				}
			}
		}

		private void DrawData(GDIRenderer gdi, PaintEventArgs e)
		{

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Gdi.NewHdc(e.Graphics.GetHdc());

			// Header
			if (Columns.Any())
			{
				DrawColumnBg(Gdi, e);
				DrawColumnText(Gdi, e);
			}

			// Background
			DrawBg(Gdi, e);

			// ForeGround
			DrawData(Gdi, e);
		}

		#endregion

		#region Mouse and Key Events

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.R) // Ctrl + R
			{
				HorizontalOrientation ^= true;
			}

			base.OnKeyDown(e);
		}

		#endregion

		#region Helpers

		private bool NeedToUpdateScrollbar()
		{
			return true;
		}

		private Point StartBg()
		{
			if (Columns.Any())
			{
				if (HorizontalOrientation)
				{
					var x = _horizontalOrientedColumnWidth - 1;
					var y = 0;
					return new Point(x, y);
				}
				else
				{
					var x = 0;
					var y = CellHeight - 1;
					return new Point(x, y);
				}
			}

			return new Point(0, 0);
		}

		private int CellHeight
		{
			get
			{
				return _charSize.Height + (CellPadding * 2);
			}
		}

		private int CellWidth
		{
			get
			{
				return _charSize.Width + (CellPadding * 4); // Double the padding for horizontal because it looks better
			}
		}

		private bool NeedsScrollbar
		{
			get
			{
				if (HorizontalOrientation)
				{
					return Width / CellWidth > ItemCount;
				}

				return Height / CellHeight > ItemCount;
			}
		}

		private void ColumnChanged()
		{
			NeedToReDrawColumn = true;
			var text = Columns.Max(c => c.Text.Length);
			_horizontalOrientedColumnWidth = (text * _charSize.Width) + (CellPadding * 2);
		}

		private int CalcWidth(RollColumn col)
		{
			return col.Width ?? ((col.Text.Length * _charSize.Width) + (CellPadding * 4));
		}

		#endregion
	}

	public class RollColumns : List<RollColumn>
	{
		public void Add(string name, string text, int width, RollColumn.InputType type = RollColumn.InputType.Text)
		{
			Add(new RollColumn
			{
				Name = name,
				Text = text,
				Width = width,
				Type = type
			});
		}

		public IEnumerable<string> Groups
		{
			get
			{
				return this
					.Select(x => x.Group)
					.Distinct();
			}
		}
	}

	public class RollColumn
	{
		public enum InputType { Boolean, Float, Text, Image }

		public string Group { get; set; }
		public int? Width { get; set; }
		public string Name { get; set; }
		public string Text { get; set; }
		public InputType Type { get; set; }
	}
}
