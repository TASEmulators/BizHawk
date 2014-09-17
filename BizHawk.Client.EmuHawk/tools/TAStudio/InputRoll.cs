using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;
using System.Collections;

namespace BizHawk.Client.EmuHawk
{
	//Row width depends on font size and padding
	//Column width is specified in column headers
	//Row width is specified for horizontal orientation
	public class InputRoll : Control
	{
		private readonly GDIRenderer Gdi;
		private readonly RollColumns _columns = new RollColumns();
		private readonly List<Cell> SelectedItems = new List<Cell>();

		private readonly VScrollBar VBar;

		private readonly HScrollBar HBar;

		private bool _horizontalOrientation = false;
		private bool _programmaticallyUpdatingScrollBarValues = false;
		private int _maxCharactersInHorizontal = 1;

		private int _rowCount = 0;
		private Size _charSize;

		public InputRoll()
		{

			UseCustomBackground = true;
			GridLines = true;
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
				_charSize = Gdi.MeasureString("A", this.Font);//TODO make this a property so changing it updates other values.
			}

			UpdateCellSize();
			ColumnWidth = CellWidth;
			ColumnHeight = CellHeight + 5;

			//TODO Figure out how to use the width and height properties of the scrollbars instead of 17
			VBar = new VScrollBar
			{
				Location = new Point(Width - 17, 0),
				Visible = false,
				Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
				SmallChange = CellHeight,
				LargeChange = CellHeight * 20
			};

			HBar = new HScrollBar
			{
				Location = new Point(0, Height - 17),
				Visible = false,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				SmallChange = 1,
				LargeChange = 20
			};

			this.Controls.Add(VBar);
			this.Controls.Add(HBar);

			VBar.ValueChanged += VerticalBar_ValueChanged;
			HBar.ValueChanged += HorizontalBar_ValueChanged;

			HorizontalOrientation = false;
			RecalculateScrollBars();
			_columns.ChangedCallback = ColumnChangedCallback;
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

		/// <summary>
		/// Displays grid lines around cells
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool GridLines { get; set; }

		/// <summary>
		/// Gets or sets whether the control is horizontal or vertical
		/// </summary>
		[Category("Behavior")]
		public bool HorizontalOrientation {
			get{
				return _horizontalOrientation;
			}
			set
			{
				if (_horizontalOrientation != value) 
				{ 
					_horizontalOrientation = value; 
					OrientationChanged(); 
				}
			}
		}

		/// <summary>
		/// Gets or sets the sets the virtual number of rows to be displayed. Does not include the column header row.
		/// </summary>
		[Category("Behavior")]
		public int RowCount
		{
			get
			{
				return _rowCount;
			}

			set
			{
				_rowCount = value;
				RecalculateScrollBars();
			}
		}

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
		[DefaultValue(false)]
		public bool FullRowSelect { get; set; }

		/// <summary>
		/// Allows multiple items to be selected
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool MultiSelect { get; set; }

		/// <summary>
		/// Gets or sets whether or not the control is in input painting mode
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		public bool InputPaintingMode { get; set; }

		/// <summary>
		/// The columns shown
		/// </summary>
		[Category("Behavior")]
		public RollColumns Columns { get { return _columns; } }

		public void SelectAll()
		{
			var oldFullRowVal = FullRowSelect;
			FullRowSelect = true;
			for (int i = 0; i < RowCount; i++)
			{
				SelectRow(i, true);
			}

			FullRowSelect = oldFullRowVal;
		}

		public void DeselectAll()
		{
			SelectedItems.Clear();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Fire the QueryItemText event which requests the text for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		/// <summary>
		/// Fire the QueryItemBkColor event which requests the background color for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fire the QueryItemIconHandler event which requests an icon for a given cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemIconHandler QueryItemIcon;

		/// <summary>
		/// Fires when the mouse moves from one cell to another (including column header cells)
		/// </summary>
		[Category("Mouse")]
		public event CellChangeEventHandler PointedCellChanged;

		/// <summary>
		/// Occurs when a column header is clicked
		/// </summary>
		[Category("Action")]
		public event System.Windows.Forms.ColumnClickEventHandler ColumnClick;

		/// <summary>
		/// Occurs whenever the 'SelectedItems' property for this control changes
		/// </summary>
		[Category("Behavior")]
		public event System.EventHandler SelectedIndexChanged;

		/// <summary>
		/// Occurs whenever the mouse wheel is scrolled while the right mouse button is held
		/// </summary>
		[Category("Behavior")]
		public event RightMouseScrollEventHandler RightMouseScrolled;

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandler(int index, int column, out string text);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandler(int index, int column, ref Color color);

		/// <summary>
		/// Retrive the image for a given cell
		/// </summary>
		public delegate void QueryItemIconHandler(int index, int column, ref Bitmap icon);

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);

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

		public void SelectRow(int index, bool val)
		{
			if (_columns.Any())
			{
				if (val)
				{
					SelectCell(new Cell
					{
						RowIndex = index,
						Column = _columns[0]
					});
				}
				else
				{
					var items = SelectedItems.Where(cell => cell.RowIndex == index);
					SelectedItems.RemoveAll(x => items.Contains(x));
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int? FirstSelectedIndex
		{
			get
			{
				if (SelectedRows.Any())
				{
					return SelectedRows
						.OrderBy(x => x)
						.First();
				}

				return null;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int? LastSelectedIndex
		{
			get
			{
				if (SelectedRows.Any())
				{
					return SelectedRows
						.OrderBy(x => x)
						.Last();
				}

				return null;
			}
		}

		/// <summary>
		/// The current Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		/// <summary>
		/// The previous Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Cell LastCell { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool IsPaintDown { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool UseCustomBackground { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int DrawHeight{ get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int DrawWidth { get; private set; }

		/// <summary>
		/// Sets the width of data cells when in Horizontal orientation.
		/// </summary>
		/// <param name="maxLength">The maximum number of characters the column will support in Horizontal orientation.</param>
		public int MaxCharactersInHorizontal{
			get
			{
				return _maxCharactersInHorizontal;
			} 
			set
			{
				_maxCharactersInHorizontal = value;
				UpdateCellSize();
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool RightButtonHeld { get; set; }

		public string UserSettingsSerialized()
		{
			return string.Empty; // TODO
		}

		/// <summary>
		/// Gets or sets the first visible row index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleRow
		{
			get
			{
				if (HorizontalOrientation)
				{
					if (NeedsHScrollbar)
					{
						return HBar.Value / CellWidth;
					}
				}

				if (NeedsVScrollbar)
				{
					return VBar.Value / CellHeight;
				}

				return 0;
			}

			set
			{
				if (HorizontalOrientation)
				{
					if (NeedsHScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						HBar.Value = value * CellWidth;
						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
				else
				{
					if (NeedsVScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						VBar.Value = value * CellHeight;
						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
			}
		}

		public int LastVisibleRow
		{
			get
			{
				return FirstVisibleRow + VisibleRows;
			}

			set
			{
				int i = Math.Max(value - VisibleRows, 0);
				FirstVisibleRow = i;
			}
		}

		/// <summary>
		/// Gets the number of rows currently visible including partially visible rows.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int VisibleRows
		{
			get
			{
				if (HorizontalOrientation)
				{
					var width = DrawWidth - (NeedsVScrollbar ? VBar.Width : 0);

					return (int)Math.Floor((decimal)(width - ColumnWidth) / CellWidth);
				}

				var height = DrawHeight - (NeedsHScrollbar ? HBar.Height : 0);

				return (int)((decimal)height / CellHeight);
			}
		}

		// TODO: make IEnumerable, IList is for legacy support
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public IList<int> SelectedRows
		{
			get
			{
				return SelectedItems
					.Where(cell => cell.RowIndex.HasValue)
					.Select(cell => cell.RowIndex.Value)
					.Distinct()
					.ToList();
			}
		}

		#endregion

		#region Paint

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var LCK = Gdi.LockGraphics(e.Graphics))
			{
				Gdi.StartOffScreenBitmap(Width, Height);

				//White Background
				Gdi.SetBrush(Color.White);
				Gdi.SetSolidPen(Color.White);
				Gdi.FillRectangle(0, 0, Width, Height);

				if (_columns.Any())
				{
					DrawColumnBg(e);
					DrawColumnText(e);
				}

				//Background
				DrawBg(e);

				//Foreground
				DrawData(e);

				Gdi.CopyToScreen();
				Gdi.EndOffScreenBitmap();
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
				foreach (var column in _columns)
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
				Gdi.PrepDrawString(this.Font, this.ForeColor);
				foreach (var column in _columns)
				{
					var point = new Point(column.Left.Value + 2* CellPadding - HBar.Value, CellPadding);//TODO: fix this CellPadding issue (2 * CellPadding vs just CellPadding)

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
				}
			}
		}

		//TODO Centering text is buggy and only implemented for Horizontal Orientation
		private void DrawData(PaintEventArgs e)
		{
			if (QueryItemText != null)
			{
				if (HorizontalOrientation)
				{
					int startIndex = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startIndex + 1;

					Gdi.PrepDrawString(this.Font, this.ForeColor);
					for (int i = 0; i < range; i++)
					{
						for (int j = 0; j < _columns.Count; j++)
						{
							string text;
							QueryItemText(i + startIndex, j, out text);

							//Center Text
							int x = RowsToPixels(i) + (CellWidth - text.Length * _charSize.Width) / 2;
							int y = j * CellHeight + CellPadding;
							var point = new Point(x, y);
							if (!string.IsNullOrWhiteSpace(text))
							{
								Gdi.DrawString(text, point);
							}
						}
					}
				}
				else
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					Gdi.PrepDrawString(this.Font, this.ForeColor);
					int xPadding = CellPadding + 1 - HBar.Value;
					for (int i = 0; i < range; i++)//Vertical
					{
						for (int j = 0; j < _columns.Count; j++)//Horizontal
						{
							var col = _columns[j];
							if (col.Left.Value < 0 || col.Right.Value > DrawWidth)
							{
								continue;
							}
							string text;
							var point = new Point(col.Left.Value + xPadding, RowsToPixels(i));

							Bitmap image = null;
							if (QueryItemIcon != null)
							{
								QueryItemIcon(i + startRow, j, ref image);
							}

							if (image != null)
							{
								Gdi.DrawBitmap(image, point);
							}
							else
							{
								QueryItemText(i + startRow, j, out text);
								if (!string.IsNullOrWhiteSpace(text))
								{
									Gdi.DrawString(text, point);
								}
							}
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
				Gdi.FillRectangle(0, 0, ColumnWidth + 1, DrawHeight + 1);
				Gdi.Line(0, 0, 0, _columns.Count * CellHeight + 1);
				Gdi.Line(ColumnWidth, 0, ColumnWidth, _columns.Count * CellHeight + 1);

				int start = 0;
				foreach (var column in _columns)
				{
					Gdi.Line(1, start, ColumnWidth, start);
					start += CellHeight;
				}
				if (_columns.Any())
				{
					Gdi.Line(1, start, ColumnWidth, start);
				}
			}
			else
			{
				int bottomEdge = RowsToPixels(0);
				//Gray column box and black line underneath
				Gdi.FillRectangle(0, 0, Width + 1, bottomEdge + 1);
				Gdi.Line(0, 0, TotalColWidth.Value + 1, 0);
				Gdi.Line(0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				//Vertical black seperators
				for (int i = 0; i < _columns.Count; i++)
				{
					int pos = _columns[i].Left.Value - HBar.Value;
					Gdi.Line(pos, 0, pos, bottomEdge);
				}

				////Draw right most line
				if (_columns.Any())
				{
					int right = TotalColWidth.Value - HBar.Value;
					Gdi.Line(right, 0, right, bottomEdge);
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (int i = 0; i < _columns.Count; i++)
					{
						if (_columns[i] != CurrentCell.Column)
						{
							continue;
						}

						Gdi.SetBrush(SystemColors.Highlight);
						Gdi.FillRectangle(1, i * CellHeight + 1, ColumnWidth - 1, CellHeight - 1);
					}
				}
				else
				{
					//TODO multiple selected columns
					for (int i = 0; i < _columns.Count; i++)
					{
						if (_columns[i] == CurrentCell.Column){
							//Left of column is to the right of the viewable area or right of column is to the left of the viewable area
							if(_columns[i].Left.Value - HBar.Value > Width || _columns[i].Right.Value - HBar.Value < 0){
								continue;
							}
							int left = _columns[i].Left.Value - HBar.Value;
							int width = _columns[i].Right.Value - HBar.Value - left;
							Gdi.SetBrush(SystemColors.Highlight);
							Gdi.FillRectangle(left + 1, 1, width - 1, CellHeight - 1);
						}
					}
				}
			}
		}
		//TODO refactor this and DoBackGroundCallback functions.
		/// <summary>
		/// Draw Gridlines and background colors using QueryItemBkColor.
		/// </summary>
		/// <param name="e"></param>
		private void DrawBg(PaintEventArgs e)
		{
			if (QueryItemBkColor != null && UseCustomBackground)
			{
				DoBackGroundCallback(e);
			}
			if (GridLines)
			{
				Gdi.SetSolidPen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						var x = RowsToPixels(i);
						var y2 = (_columns.Count * CellHeight) - 1;
						if (y2 > Height)
						{
							y2 = Height - 2;
						}

						Gdi.Line(x, 1, x, DrawHeight);
					}

					// Rows
					for (int i = 0; i < _columns.Count + 1; i++)
					{
						Gdi.Line(RowsToPixels(0) + 1, i * CellHeight, DrawWidth, i * CellHeight);
					}
				}
				else
				{
					// Columns
					int y = ColumnHeight + 1;
					foreach (var column in _columns)
					{
						int x = column.Left.Value - HBar.Value;
						Gdi.Line(x, y, x, Height - 1);
					}
					if (_columns.Any())
					{
						Gdi.Line(TotalColWidth.Value - HBar.Value, y, TotalColWidth.Value - HBar.Value, Height - 1);
					}

					// Rows
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						Gdi.Line(0, RowsToPixels(i), Width + 1, RowsToPixels(i));
					}
				}
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
				var relativeCell = new Cell()
				{
					RowIndex = cell.RowIndex - FirstVisibleRow,
					Column = cell.Column,
					CurrentText = cell.CurrentText
				};
				DrawCellBG(SystemColors.Highlight, relativeCell);
			}
		}

		/// <summary>
		/// Given a cell with rowindex inbetween 0 and VisibleRows, it draws the background color specified. Do not call with absolute rowindices.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="cell"></param>
		private void DrawCellBG(Color color, Cell cell)
		{
			int x = 0,
				y = 0,
				w = 0,
				h = 0;

			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				w = CellWidth - 1;
				y = (CellHeight * _columns.IndexOf(cell.Column)) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (x < ColumnWidth) { return; }
			}
			else
			{
				w = cell.Column.Width.Value - 1;
				x = cell.Column.Left.Value - HBar.Value + 1;
				y = RowsToPixels(cell.RowIndex.Value) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (y < ColumnHeight) { return; }
			}

			if (x > DrawWidth || y > DrawHeight) { return; }//Don't draw if off screen.

			Gdi.SetBrush(color);
			Gdi.FillRectangle(x, y, w, h);
		}

		/// <summary>
		/// Calls QueryItemBkColor callback for all visible cells and fills in the background of those cells.
		/// </summary>
		/// <param name="e"></param>
		private void DoBackGroundCallback(PaintEventArgs e)
		{
			if (HorizontalOrientation)
			{
				int startIndex = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startIndex + 1;

				for (int i = 0; i < range; i++)
				{
					for (int j = 0; j < _columns.Count; j++)//TODO: Don't query all columns
					{
						Color color = Color.White;
						QueryItemBkColor(i + startIndex, j, ref color);

						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell()
							{
								Column = _columns[j],
								RowIndex = i
							};
							DrawCellBG(color, cell);
						}
					}
				}
			}
			else
			{
				int startRow = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

				for (int i = 0; i < range; i++)//Vertical
				{
					for (int j = 0; j < _columns.Count; j++)//Horizontal
					{
						Color color = Color.White;
						QueryItemBkColor(i + startRow, j, ref color);
						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell()
							{
								Column = _columns[j],
								RowIndex = i
							};
							DrawCellBG(color, cell);
						}
					}
				}
			}
		}

		#endregion

		#region Mouse and Key Events

		//protected override void OnKeyDown(KeyEventArgs e)
		//{
		//	base.OnKeyDown(e);
		//}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var newCell = CalculatePointedCell(e.X, e.Y);
			newCell.RowIndex += FirstVisibleRow;
			if (!newCell.Equals(CurrentCell))
			{
				CellChanged(newCell);

				if (IsHoveringOnColumnCell ||
					(WasHoveringOnColumnCell && !IsHoveringOnColumnCell))
				{
					Refresh();
				}
			}
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
			IsPaintDown = false;
			Refresh();
			base.OnMouseLeave(e);
		}

		//TODO add query callback of whether to select the cell or not
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && InputPaintingMode)
			{
				IsPaintDown = true;
			}

			if (e.Button == MouseButtons.Right)
			{
				RightButtonHeld = true;
			}

			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnColumnCell)
				{
					ColumnClickEvent(ColumnAtX(e.X));
				}
				else if (IsHoveringOnDataCell)
				{
					if (ModifierKeys == Keys.Alt)
					{
						MessageBox.Show("Alt click logic is not yet implemented");
					}
					else if (ModifierKeys == Keys.Shift)
					{
						if (SelectedItems.Any())
						{
							MessageBox.Show("Shift click logic is not yet implemented");
						}
						else
						{
							SelectCell(CurrentCell);
						}
					}
					else if (ModifierKeys == Keys.Control)
					{
						SelectCell(CurrentCell);
					}
					else
					{
						SelectedItems.Clear();
						SelectCell(CurrentCell);
					}

					Refresh();
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			IsPaintDown = false;
			RightButtonHeld = false;

			base.OnMouseUp(e);
			}

		private void IncrementScrollBar(ScrollBar bar, bool increment)
		{
			int newVal;
			if (increment)
			{
				newVal = bar.Value + bar.SmallChange;
				if (newVal > bar.Maximum)
				{
					newVal = bar.Maximum;
				}
			}
			else
			{
				newVal = bar.Value - bar.SmallChange;
				if (newVal < 0)
				{
					newVal = 0;
				}
			}

			_programmaticallyUpdatingScrollBarValues = true;
			bar.Value = newVal;
			_programmaticallyUpdatingScrollBarValues = false;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (RightButtonHeld)
			{
				DoRightMouseScroll(this, e);
			}
			else
			{
				if (HorizontalOrientation)
				{
					IncrementScrollBar(HBar, e.Delta < 0);
				}
				else
				{
					IncrementScrollBar(VBar, e.Delta < 0);
				}

				Refresh();
			}
		}

		private void DoRightMouseScroll(object sender, MouseEventArgs e)
		{
			if (RightMouseScrolled != null)
			{
				RightMouseScrolled(sender, e);
			}
		}

		private void ColumnClickEvent(RollColumn column)
		{
			if (ColumnClick != null)
			{
				ColumnClick(this, new ColumnClickEventArgs(_columns.IndexOf(column)));
			}
		}

		#endregion

		#region Change Events

		protected override void OnResize(EventArgs e)
		{
			RecalculateScrollBars();
			base.OnResize(e);
			Refresh();
		}

		private void OrientationChanged()
		{
			RecalculateScrollBars();

			//TODO scroll to correct positions

			if (HorizontalOrientation)
			{
				VBar.SmallChange = CellHeight;
				HBar.SmallChange = CellWidth;
				VBar.LargeChange = 10;
				HBar.LargeChange = CellWidth * 20;
			}
			else
			{
				VBar.SmallChange = CellHeight;
				HBar.SmallChange = 1;
				VBar.LargeChange = CellHeight * 20;
				HBar.LargeChange = 20;
			}

			RecalculateScrollBars();

			Refresh();
		}

		/// <summary>
		/// Call this function to change the CurrentCell to newCell
		/// </summary>
		/// <param name="oldCell"></param>
		/// <param name="newCell"></param>
		private void CellChanged(Cell newCell)
		{
			if (PointedCellChanged != null && newCell != CurrentCell)
			{
				LastCell = CurrentCell;
				CurrentCell = newCell;
				PointedCellChanged(this, new CellEventArgs(LastCell, CurrentCell));
			}
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}
		}

		private void HorizontalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}
		}

		private void ColumnChangedCallback()
		{
			RecalculateScrollBars();
			if (_columns.Any())
			{
				ColumnWidth = _columns.Max(c => c.Width.Value) + CellPadding * 4;
			}
		}

		#endregion

		#region Helpers

		//ScrollBar.Maximum = DesiredValue + ScrollBar.LargeChange - 1
		//See MSDN Page for more information on the dumb ScrollBar.Maximum Property
		private void RecalculateScrollBars()
		{
			UpdateDrawSize();

			if (HorizontalOrientation)
			{
				NeedsVScrollbar =  _columns.Count > DrawHeight / CellHeight;
				NeedsHScrollbar = RowCount > (DrawWidth - ColumnWidth) / CellWidth;
			}
			else
			{
				NeedsVScrollbar = RowCount > DrawHeight / CellHeight;
				NeedsHScrollbar = TotalColWidth.HasValue && TotalColWidth.Value - DrawWidth + 1 > 0;
			}

			UpdateDrawSize();

			//Update VBar
			if (NeedsVScrollbar)
			{
				if (HorizontalOrientation)
				{
					VBar.Maximum = (((_columns.Count * CellHeight) - DrawHeight) / CellHeight) + VBar.LargeChange;
				}
				else
				{
					VBar.Maximum = RowsToPixels(RowCount + 1) - DrawHeight + VBar.LargeChange - 1;
				}

				VBar.Location = new Point(Width - 17, 0);
				VBar.Size = new Size(VBar.Width, Height);
				VBar.Visible = true;
			}
			else
			{
				VBar.Visible = false;
				VBar.Value = 0;
			}

			//Update HBar
			if (NeedsHScrollbar)
			{
				HBar.Visible = true;
				if (HorizontalOrientation)
				{
					HBar.Maximum = RowsToPixels(RowCount + 1) - DrawWidth + HBar.LargeChange - 1;
				}
				else
				{
					HBar.Maximum = TotalColWidth.Value - DrawWidth + HBar.LargeChange;
				}

				if (NeedsVScrollbar)
				{
					HBar.Size = new Size(Width - VBar.Width + 1, HBar.Height);
				}
				else
				{
					HBar.Size = new Size(Width, HBar.Height);
				}
			}
			else
			{
				HBar.Visible = false;
				HBar.Value = 0;
			}
		}

		private void UpdateDrawSize()
		{
			if (NeedsVScrollbar) 
			{
				DrawWidth = Width - VBar.Width; 
			}
			else
			{
				DrawWidth = Width;
			}
			if (NeedsHScrollbar) { 
				DrawHeight = Height - HBar.Height;
			}
			else
			{
				DrawHeight = Height; 
			}
		}

		/// <summary>
		/// If FullRowSelect is enabled, selects all cells in the row that contains the given cell. Otherwise only given cell is added.
		/// </summary>
		/// <param name="cell">The cell to select.</param>
		private void SelectCell(Cell cell)
		{
			if (!MultiSelect)
			{
				SelectedItems.Clear();
			}

			if (FullRowSelect)
			{
				foreach (var column in _columns)
				{
					SelectedItems.Add(new Cell
					{
						RowIndex = cell.RowIndex,
						Column = column
					});
				}
			}
			else
			{
				SelectedItems.Add(CurrentCell);
			}

			SelectedIndexChanged(this, new EventArgs());
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Column Cell.
		/// </summary>
		private bool IsHoveringOnColumnCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					!CurrentCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Data Cell.
		/// </summary>
		private bool IsHoveringOnDataCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					CurrentCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Column Cell.
		/// </summary>
		private bool WasHoveringOnColumnCell
		{
			get
			{
				return LastCell != null &&
					LastCell.Column != null &&
					!LastCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Data Cell.
		/// </summary>
		private bool WasHoveringOnDataCell
		{
			get
			{
				return LastCell != null &&
					LastCell.Column != null &&
					LastCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Finds the specific cell that contains the (x, y) coordinate.
		/// </summary>
		/// <remarks>The row number that it returns will be between 0 and VisibleRows, NOT the absolute row number.</remarks>
		/// <param name="x">X coordinate point.</param>
		/// <param name="y">Y coordinate point.</param>
		/// <returns>The cell with row number and RollColumn reference, both of which can be null. </returns>
		private Cell CalculatePointedCell(int x, int y)
		{
			var newCell = new Cell();

			// If pointing to a column header
			if (_columns.Any())
			{
				if (HorizontalOrientation)
				{
					if (x >= ColumnWidth)
					{
						newCell.RowIndex = PixelsToRows(x);
					}

					int colIndex = (y / CellHeight);
					if (colIndex >= 0 && colIndex < _columns.Count)
					{
						newCell.Column = _columns[colIndex];
					}
				}
				else
				{
					if (y >= CellHeight)
					{
						newCell.RowIndex = PixelsToRows(y);
					}

					newCell.Column = ColumnAtX(x);
				}
			}
			return newCell;
		}

		// TODO: Calculate this on Orientation change instead of call it every time
		//TODO: find a different solution
		private Point StartBg()
		{
			if (_columns.Any())
			{
				if (HorizontalOrientation)
				{
					var x = ColumnWidth;
					var y = 0;
					return new Point(x, y);
				}
				else
				{
					var y = ColumnHeight;
					return new Point(0, y);
				}
			}

			return new Point(0, 0);
		}

		private void DrawRectangleNoFill(GDIRenderer gdi, int x, int y, int width, int height)
		{
			gdi.Line(x, y, x + width, y);
			gdi.Line(x + width, y, x + width, y + height);
			gdi.Line(x + width, y + height, x, y + height);
			gdi.Line(x, y + height, x, y);
		}

		/// <summary>
		/// A boolean that indicates if the InputRoll is too large vertically and requires a vertical scrollbar.
		/// </summary>
		private bool NeedsVScrollbar{ get; set; }

		/// <summary>
		/// A boolean that indicates if the InputRoll is too large horizontally and requires a horizontal scrollbar.
		/// </summary>
		private bool NeedsHScrollbar{ get; set; }

		//TODO rename and find uses
		//private void ColumnChanged()
		//{
		//	var text = _columns.Max(c => c.Text.Length);
		//	ColumnWidth = (text * _charSize.Width) + (CellPadding * 2);
		//}

		/// <summary>
		/// Updates the width of the supplied column.
		/// <remarks>Call when changing the ColumnCell text, CellPadding, or text font.</remarks>
		/// </summary>
		/// <param name="col">The RollColumn object to update.</param>
		/// <returns>The new width of the RollColumn object.</returns>
		private int UpdateWidth(RollColumn col)
		{
			col.Width = ((col.Text.Length * _charSize.Width) + (CellPadding * 4));
			return col.Width.Value;
		}

		/// <summary>
		/// Gets the total width of all the columns by using the last column's Right property.
		/// </summary>
		/// <returns>A nullable Int representing total width.</returns>
		private int? TotalColWidth
		{ 
			get{
				if (_columns.Any())
				{
					return _columns.Last().Right;
				}
				return null;
			}
		}

		/// <summary>
		/// Returns the RollColumn object at the specified visible x coordinate. Coordinate should be between 0 and Width of the InputRoll Control.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <returns>RollColumn object that contains the x coordinate or null if none exists.</returns>
		private RollColumn ColumnAtX(int x)
		{
			foreach (var column in _columns)
			{
				if (column.Left.Value - HBar.Value <= x && column.Right.Value - HBar.Value >= x)
				{
					return column;
				}
			}

			return null;
		}

		/// <summary>
		/// Converts a row number to a horizontal or vertical coordinate.
		/// </summary>
		/// <param name="pixels">A row number.</param>
		/// <returns>A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</returns>
		private int RowsToPixels(int index)
		{
			if (_horizontalOrientation)
			{
				return index * CellWidth + ColumnWidth;
			}
			return index * CellHeight + ColumnHeight;
		}

		/// <summary>
		/// Converts a horizontal or vertical coordinate to a row number.
		/// </summary>
		/// <param name="pixels">A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</param>
		/// <returns>A row number between 0 and VisibleRows if it is a Datarow, otherwise a negative number if above all Datarows.</returns>
		private int PixelsToRows(int pixels)
		{
			if (_horizontalOrientation)
			{
				return (pixels - ColumnWidth) / CellWidth;
			}
			return (pixels - ColumnHeight) / CellHeight;
		}

		/// <summary>
		/// The width of the largest column cell in Horizontal Orientation
		/// </summary>
		private int ColumnWidth { get; set; }

		/// <summary>
		/// The height of a column cell in Vertical Orientation.
		/// </summary>
		private int ColumnHeight { get; set; }

		//Cell defaults
		/// <summary>
		/// The width of a cell in Horizontal Orientation. Only can be changed by changing the Font or CellPadding.
		/// </summary>
		private int CellWidth { get; set; }

		/// <summary>
		/// The height of a cell in Vertical Orientation. Only can be changed by changing the Font or CellPadding.
		/// </summary>
		private int CellHeight { get; set; }

		/// <summary>
		/// Call when _charSize, MaxCharactersInHorizontal, or CellPadding is changed.
		/// </summary>
		private void UpdateCellSize()
		{
			CellHeight = _charSize.Height + CellPadding * 2;
			CellWidth  = _charSize.Width * MaxCharactersInHorizontal + CellPadding * 4; // Double the padding for horizontal because it looks better
		}

		#endregion

		#region Classes

		public class RollColumns : List<RollColumn>
		{
			public RollColumn this[string name]
			{
				get
				{
					return this.SingleOrDefault(column => column.Name == name);
				}
			}
			
			public Action ChangedCallback { get; set; }

			private void DoChangeCallback()
			{
				if (ChangedCallback != null)
				{
					ChangedCallback();
				}
			}

			private void ColumnsChanged()
			{
				int pos = 0;
				for (int i = 0; i < Count; i++)
				{
					this[i].Left = pos;
					pos += this[i].Width.Value;
					this[i].Right = pos;
				}
				DoChangeCallback();
			}

			public new void Add(RollColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					// The designer sucks, doing nothing for now
					return;
					//throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Add(column);
				ColumnsChanged();
			}

			public new void AddRange(IEnumerable<RollColumn> collection)
			{
				foreach(var column in collection)
				{
					if (this.Any(c => c.Name == column.Name))
					{
						// The designer sucks, doing nothing for now
						return;

						throw new InvalidOperationException("A column with this name already exists.");
					}
				}

				base.AddRange(collection);
				ColumnsChanged();
			}

			public new void Insert(int index, RollColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Insert(index, column);
				ColumnsChanged();
			}

			public new void InsertRange(int index, IEnumerable<RollColumn> collection)
			{
				foreach (var column in collection)
				{
					if (this.Any(c => c.Name == column.Name))
					{
						throw new InvalidOperationException("A column with this name already exists.");
					}
				}

				base.InsertRange(index, collection);
				ColumnsChanged();
			}

			public new bool Remove(RollColumn column)
			{
				var result = base.Remove(column);
				ColumnsChanged();
				return result;
			}

			public new int RemoveAll(Predicate<RollColumn> match)
			{
				var result = base.RemoveAll(match);
				ColumnsChanged();
				return result;
			}

			public new void RemoveAt(int index)
			{
				base.RemoveAt(index);
				ColumnsChanged();
			}

			public new void RemoveRange(int index, int count)
			{
				base.RemoveRange(index, count);
				ColumnsChanged();
			}

			public new void Clear()
			{
				base.Clear();
				ColumnsChanged();
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
			public int? Left { get; set; }
			public int? Right { get; set; }
			public string Name { get; set; }
			public string Text { get; set; }
			public InputType Type { get; set; }
		}

		/// <summary>
		/// 
		/// </summary>
		public class Cell
		{
			public RollColumn Column { get; internal set; }
			public int? RowIndex { get; internal set; }
			public string CurrentText { get; internal set; }

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
