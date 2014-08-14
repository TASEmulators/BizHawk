using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	public class InputRoll : Control
	{
		private readonly GDIRenderer Gdi;
		private readonly RollColumns Columns = new RollColumns();
		private readonly List<Cell> SelectedItems = new List<Cell>();

		private int _horizontalOrientedColumnWidth = 0;
		private Size _charSize;

		public InputRoll()
		{
			CellPadding = 3;
			CurrentCell = null;
			Font = new Font("Courier New", 8);  // Only support fixed width

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			Gdi = new GDIRenderer();

			using (var g = CreateGraphics())
			using (var LCK = Gdi.LockGraphics(g))
			{
				_charSize = Gdi.MeasureString("A", this.Font);
			}
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
		[Category("Behavior")]
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

		/// <summary>
		/// Indicates whether the entire row will always be selected 
		/// </summary>
		[Category("Appearance")]
		public bool FullRowSelect { get; set; }

		#endregion

		#region Event Handlers

		/// <summary>
		/// Fire the QueryItemText event which requests the text for the passed Listview cell.
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		/// <summary>
		/// Fire the QueryItemBkColor event which requests the background color for the passed Listview cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fires when the mouse moves from one cell to another (including column header cells)
		/// </summary>
		[Category("Mouse")]
		public event CellChangeEventHandler PointedCellChanged;

		[Category("Action")]
		public event ColumnClickEventHandler ColumnClick;

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandler(int index, int column, out string text);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandler(int index, int column, ref Color color);

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

		public class CellEventArgs
		{
			public CellEventArgs(Cell oldCell, Cell newCell)
			{
				OldCell = oldCell;
				NewCell = newCell;
			}

			public Cell OldCell { get; private set; }
			public Cell NewCell { get; private set; }
		}

		#endregion

		#region Api

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool UseCustomBackground { get; set; }

		public string UserSettingsSerialized()
		{
			return string.Empty; // TODO
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int VisibleRows
		{
			get
			{
				if (HorizontalOrientation)
				{
					return (Width - _horizontalOrientedColumnWidth) / CellWidth;
				}

				return (Height / CellHeight) - 1;
			}
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

		public RollColumn GetColumn(int index)
		{
			return Columns[index];
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<int> SelectedIndices
		{
			get
			{
				return SelectedItems
					.Where(cell => cell.RowIndex.HasValue)
					.Select(cell => cell.RowIndex.Value);
			}
		}

		#endregion

		#region Paint

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var LCK = Gdi.LockGraphics(e.Graphics))
			{
				// Header
				if (Columns.Any())
				{
					DrawColumnBg(e);
					DrawColumnText(e);
				}

				// Background
				DrawBg(e);

				// ForeGround
				DrawData(e);
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing, and this should never be called
		}

		private void DrawColumnText(PaintEventArgs e)
		{
			if (HorizontalOrientation)
			{
				int start = 0;
				Gdi.PrepDrawString(this.Font, this.ForeColor);
				foreach (var column in Columns)
				{
					var point = new Point(CellPadding, start + CellPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						Gdi.PrepDrawString(this.Font, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						Gdi.PrepDrawString(this.Font, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}

					start += CellHeight;
				}
			}
			else
			{
				int start = CellPadding;
				Gdi.PrepDrawString(this.Font, this.ForeColor);
				foreach (var column in Columns)
				{
					var point = new Point(start + CellPadding, CellPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						Gdi.PrepDrawString(this.Font, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						Gdi.PrepDrawString(this.Font, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}

					start += CalcWidth(column);
				}
			}
		}

		private void DrawData(PaintEventArgs e)
		{
			if (QueryItemText != null)
			{
				if (HorizontalOrientation)
				{
					var visibleRows = (Width - _horizontalOrientedColumnWidth) / CellWidth;
					Gdi.PrepDrawString(this.Font, this.ForeColor);
					for (int i = 0; i < visibleRows; i++)
					{
						for (int j = 0; j < Columns.Count; j++)
						{
							string text;
							int x = _horizontalOrientedColumnWidth + CellPadding + (CellWidth * i);
							int y = j * CellHeight;
							var point = new Point(x, y);
							QueryItemText(i, j, out text);
							Gdi.DrawString(text, point);
						}
					}
				}
				else
				{
					var visibleRows = (Height / CellHeight) - 1;
					Gdi.PrepDrawString(this.Font, this.ForeColor);
					for (int i = 1; i < visibleRows; i++)
					{
						int x = 1;
						for (int j = 0; j < Columns.Count; j++)
						{
							string text;
							var point = new Point(x + CellPadding, i * CellHeight);
							QueryItemText(i, j, out text);
							Gdi.DrawString(text, point);
							x += CalcWidth(Columns[j]);
						}
					}
				}
			}
		}

		private void DrawColumnBg(PaintEventArgs e)
		{
			Gdi.SetBrush(SystemColors.ControlLight);
			Gdi.SetSolidPen(Color.Black);

			if (HorizontalOrientation)
			{
				Gdi.DrawRectangle(0, 0, _horizontalOrientedColumnWidth + 1, Height);
				Gdi.FillRectangle(1, 1, _horizontalOrientedColumnWidth, Height - 3);

				int start = 0;
				foreach (var column in Columns)
				{
					start += CellHeight;
					Gdi.Line(1, start, _horizontalOrientedColumnWidth, start);
				}
			}
			else
			{
				Gdi.DrawRectangle(0, 0, Width, CellHeight);
				Gdi.FillRectangle(1, 1, Width - 2, CellHeight);

				int start = 0;
				foreach (var column in Columns)
				{
					start += CalcWidth(column);
					Gdi.Line(start, 0, start, CellHeight);
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (int i = 0; i < Columns.Count; i++)
					{
						if (Columns[i] == CurrentCell.Column)
						{
							Gdi.SetBrush(SystemColors.Highlight);
							Gdi.FillRectangle(
								1,
								(i * CellHeight) + 1,
								_horizontalOrientedColumnWidth - 1,
								CellHeight - 1);
						}
					}
				}
				else
				{
					int start = 0;
					for (int i = 0; i < Columns.Count; i++)
					{
						var width = CalcWidth(Columns[i]);
						if (Columns[i] == CurrentCell.Column)
						{
							Gdi.SetBrush(SystemColors.Highlight);
							Gdi.FillRectangle(start + 1, 1, width - 1, CellHeight - 1);
						}

						start += width;
					}
				}
			}
		}

		private void DrawBg(PaintEventArgs e)
		{
			var startPoint = StartBg();

			Gdi.SetBrush(Color.White);
			Gdi.SetSolidPen(Color.Black);

			Gdi.DrawRectangle(startPoint.X, startPoint.Y, Width, Height);

			Gdi.SetSolidPen(SystemColors.ControlLight);
			if (HorizontalOrientation)
			{
				// Columns
				for (int i = 1; i < Width / CellWidth; i++)
				{
					var x = _horizontalOrientedColumnWidth + 1 + (i * CellWidth);
					Gdi.Line(x, 1, x, Columns.Count * CellHeight);
				}

				// Rows
				for (int i = 1; i < Columns.Count + 1; i++)
				{
					Gdi.Line(_horizontalOrientedColumnWidth + 1, i * CellHeight, Width - 2, i * CellHeight);
				}
			}
			else
			{
				// Columns
				int x = 0;
				int y = CellHeight + 1;
				foreach (var column in Columns)
				{
					x += CalcWidth(column);
					Gdi.Line(x, y, x, Height - 1);
				}

				// Rows
				for (int i = 2; i < Height / CellHeight; i++)
				{
					Gdi.Line(1, (i * CellHeight) + 1, Width - 2, (i * CellHeight) + 1);
				}
			}

			if (QueryItemBkColor != null && UseCustomBackground)
			{
				DoBackGroundCallback(e);
			}

			if (SelectedItems.Any())
			{
				DoSelectionBG(e);
			}
			
		}

		private void DoSelectionBG(PaintEventArgs e)
		{
			foreach(var cell in SelectedItems)
			{
				DrawCellBG(SystemColors.Highlight, cell);
			}
		}

		private void DrawCellBG(Color color, Cell cell)
		{
			int x = 0,
				y = 0,
				w = 0,
				h = 0;

			if (HorizontalOrientation)
			{
				x = (cell.RowIndex.Value * CellWidth) + 2 + _horizontalOrientedColumnWidth;
				w = CellWidth - 1;
				y = (CellHeight * Columns.IndexOf(cell.Column)) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
			}
			else
			{
				foreach (var column in Columns)
				{
					if (cell.Column == column)
					{
						w = CalcWidth(column) - 1;
						break;
					}
					else
					{
						x += CalcWidth(column);
					}
				}

				x += 1;
				y = (CellHeight * (cell.RowIndex.Value + 1)) + 2; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
			}

			Gdi.SetBrush(color);
			Gdi.FillRectangle(x, y, w, h);
		}

		private void DoBackGroundCallback(PaintEventArgs e)
		{
			if (HorizontalOrientation)
			{
				var visibleRows = (Width - _horizontalOrientedColumnWidth) / CellWidth;
				for (int i = 0; i < visibleRows; i++)
				{
					for (int j = 0; j < Columns.Count; j++)
					{
						Color color = Color.White;
						QueryItemBkColor(i, j, ref color);

						// TODO: refactor to use DrawCellBG
						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							Gdi.SetBrush(color);
							Gdi.FillRectangle(
								_horizontalOrientedColumnWidth + (i * CellWidth) + 2,
								(j * CellHeight) + 1,
								CellWidth - 1,
								CellHeight - 1);
						}
					}
				}
			}
			else
			{
				var visibleRows = (Height / CellHeight) - 1;
				for (int i = 1; i < visibleRows; i++)
				{
					int x = 1;
					for (int j = 0; j < Columns.Count; j++)
					{
						Color color = Color.White;
						QueryItemBkColor(i, j, ref color);

						var width = CalcWidth(Columns[j]);
						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							Gdi.SetBrush(color);
							Gdi.FillRectangle(x, (i * CellHeight) + 2, width - 1, CellHeight - 1);
						}

						x += width;
					}
				}
			}
		}

		#endregion

		#region Mouse and Key Events

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.R) // Ctrl + R
			{
				HorizontalOrientation ^= true;
				Refresh();
			}

			base.OnKeyDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			CalculatePointedCell(e.X, e.Y);
			base.OnMouseMove(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			CurrentCell = new Cell
			{
				Column = null,
				RowIndex = null
			};

			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			CurrentCell = null;
			Refresh();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (IsHoveringOnColumnCell)
			{
				ColumnClickEvent(ColumnAtX(e.X));
			}
			else if (IsHoveringOnDataCell)
			{
				// Alt+Click
				if (ModifierKeys.HasFlag(Keys.Shift) && !ModifierKeys.HasFlag(Keys.Control) && !ModifierKeys.HasFlag(Keys.Alt))
				{
					MessageBox.Show("Alt click logic is not yet implemented");
				}
				// Shift Click
				else if (ModifierKeys.HasFlag(Keys.Shift) && !ModifierKeys.HasFlag(Keys.Control) && !ModifierKeys.HasFlag(Keys.Alt))
				{
					if (SelectedItems.Any())
					{
						MessageBox.Show("Shift click logic is not yet implemented");
					}
					else
					{
						SelectedItems.Add(CurrentCell);
					}
				}
				// Ctrl Click
				else if (ModifierKeys.HasFlag(Keys.Control) && !ModifierKeys.HasFlag(Keys.Shift) && !ModifierKeys.HasFlag(Keys.Alt))
				{
					SelectedItems.Add(CurrentCell);
				}
				else
				{
					SelectedItems.Clear();
					SelectedItems.Add(CurrentCell);
				}

				// TODO: selected index changed event
				Refresh();
			}

			base.OnMouseClick(e);
		}

		#endregion

		#region Helpers

		private bool IsHoveringOnColumnCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					!CurrentCell.RowIndex.HasValue;
			}
		}

		private bool IsHoveringOnDataCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					CurrentCell.RowIndex.HasValue;
			}
		}

		private void CalculatePointedCell(int x, int y)
		{
			bool wasHoveringColumnCell = IsHoveringOnColumnCell;
			var newCell = new Cell();

			// If pointing to a column header
			if (Columns.Any())
			{
				if (HorizontalOrientation)
				{
					if (x < _horizontalOrientedColumnWidth)
					{
						newCell.RowIndex = null;
					}
					else
					{
						newCell.RowIndex = (x - _horizontalOrientedColumnWidth) / CellWidth;
					}

					int colIndex = (y / CellHeight);
					if (colIndex >= 0 && colIndex < Columns.Count)
					{
						newCell.Column = Columns[colIndex];
					}
				}
				else
				{
					if (y < CellHeight)
					{
						newCell.RowIndex = null;
					}
					else
					{
						newCell.RowIndex = (y / CellHeight) - 1;
					}

					int start = 0;
					//for (int i = 0; i < Columns.Count; i++)
					foreach (var column in Columns)
					{
						if (x > start)
						{
							start += CalcWidth(column);
							if (x <= start)
							{
								newCell.Column = column;
								break;
							}
						}
					}
				}
			}

			if (!newCell.Equals(CurrentCell))
			{
				CellChanged(CurrentCell, newCell);
				CurrentCell = newCell;

				if (IsHoveringOnColumnCell ||
					(wasHoveringColumnCell && !IsHoveringOnColumnCell))
				{
					Refresh();
				}
			}
		}

		private void CellChanged(Cell oldCell, Cell newCell)
		{
			if (PointedCellChanged != null)
			{
				PointedCellChanged(this, new CellEventArgs(oldCell, newCell));
			}
		}

		private void ColumnClickEvent(RollColumn column)
		{
			if (ColumnClick != null)
			{
				ColumnClick(this, new ColumnClickEventArgs(Columns.IndexOf(column)));
			}
		}

		private bool NeedToUpdateScrollbar()
		{
			return true;
		}

		// TODO: Calculate this on Orientation change instead of call it every time
		private Point StartBg()
		{
			if (Columns.Any())
			{
				if (HorizontalOrientation)
				{
					var x = _horizontalOrientedColumnWidth;
					var y = 0;
					return new Point(x, y);
				}
				else
				{
					var y = CellHeight;
					return new Point(0, y);
				}
			}

			return new Point(0, 0);
		}

		// TODO: calculate this on Cell Padding change instead of calculate it every time
		private int CellHeight
		{
			get
			{
				return _charSize.Height + (CellPadding * 2);
			}
		}

		// TODO: calculate this on Cell Padding change instead of calculate it every time
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
			var text = Columns.Max(c => c.Text.Length);
			_horizontalOrientedColumnWidth = (text * _charSize.Width) + (CellPadding * 2);
		}

		// On Column Change calculate this for every column
		private int CalcWidth(RollColumn col)
		{
			return col.Width ?? ((col.Text.Length * _charSize.Width) + (CellPadding * 4));
		}

		private RollColumn ColumnAtX(int x)
		{
			int start = 0;
			foreach (var column in Columns)
			{
				start += CalcWidth(column);
				if (start > x)
				{
					return column;
				}
			}

			return null;
		}

		#endregion

		#region Classes

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

		public class Cell
		{
			public RollColumn Column { get; set; }
			public int? RowIndex { get; set; }

			public Cell() { }

			public Cell(Cell cell)
			{
				Column = cell.Column;
				RowIndex = cell.RowIndex;
			}

			public override bool Equals(object obj)
			{
				if (obj is Cell)
				{
					var cell = obj as Cell;
					return this.Column == cell.Column && this.RowIndex == cell.RowIndex;
				}

				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return Column.GetHashCode() + RowIndex.GetHashCode();
			}
		}

		#endregion
	}
}
