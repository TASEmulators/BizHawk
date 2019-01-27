using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A performant VirtualListView implementation that doesn't rely on native Win32 API calls
	/// (and in fact does not inherit the ListView class at all)
	/// It is an enhanced version of the work done with GDI+ rendering in InputRoll.cs
	/// -----------------------------------
	/// *** Events ***
	/// -----------------------------------
	/// </summary>
	public partial class PlatformAgnosticVirtualListView
	{
		#region Event Handlers

		/// <summary>
		/// Fire the <see cref="QueryItemText"/> event which requests the text for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		/// <summary>
		/// Fire the <see cref="QueryItemTextAdvanced"/> event which requests the text for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandlerAdvanced QueryItemTextAdvanced;

		/// <summary>
		/// Fire the <see cref="QueryItemBkColor"/> event which requests the background color for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fire the <see cref="QueryItemBkColorAdvanced"/> event which requests the background color for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandlerAdvanced QueryItemBkColorAdvanced;

		[Category("Virtual")]
		public event QueryRowBkColorHandler QueryRowBkColor;

		/// <summary>
		/// Fire the <see cref="QueryItemIconHandler"/> event which requests an icon for a given cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemIconHandler QueryItemIcon;		

		/// <summary>
		/// Fires when the mouse moves from one cell to another (including column header cells)
		/// </summary>
		[Category("Mouse")]
		public event CellChangeEventHandler PointedCellChanged;

		/// <summary>
		/// Fires when a cell is hovered on
		/// </summary>
		[Category("Mouse")]
		public event HoverEventHandler CellHovered;

		/// <summary>
		/// Occurs when a column header is clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnClick;

		/// <summary>
		/// Occurs when a column header is right-clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnRightClick;

		/// <summary>
		/// Occurs whenever the 'SelectedItems' property for this control changes
		/// </summary>
		[Category("Behavior")]
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Occurs whenever the mouse wheel is scrolled while the right mouse button is held
		/// </summary>
		[Category("Behavior")]
		public event RightMouseScrollEventHandler RightMouseScrolled;

		[Category("Property Changed")]
		[Description("Occurs when the column header has been reordered")]
		public event ColumnReorderedEventHandler ColumnReordered;

		[Category("Action")]
		[Description("Occurs when the scroll value of the visible rows change (in vertical orientation this is the vertical scroll bar change, and in horizontal it is the horizontal scroll bar)")]
		public event RowScrollEvent RowScroll;

		[Category("Action")]
		[Description("Occurs when the scroll value of the columns (in vertical orientation this is the horizontal scroll bar change, and in horizontal it is the vertical scroll bar)")]
		public event ColumnScrollEvent ColumnScroll;

		[Category("Action")]
		[Description("Occurs when a cell is dragged and then dropped into a new cell, old cell is the cell that was being dragged, new cell is its new destination")]
		public event CellDroppedEvent CellDropped;

		#endregion

		#region Delegates

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandlerAdvanced(int index, ListColumn column, out string text, ref int offsetX, ref int offsetY);
		public delegate void QueryItemTextHandler(int index, int column, out string text);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandlerAdvanced(int index, ListColumn column, ref Color color);
		public delegate void QueryItemBkColorHandler(int index, int column, ref Color color);

		public delegate void QueryRowBkColorHandler(int index, ref Color color);

		/// <summary>
		/// Retrieve the image for a given cell
		/// </summary>
		public delegate void QueryItemIconHandler(int index, ListColumn column, ref Bitmap icon, ref int offsetX, ref int offsetY);		

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void HoverEventHandler(object sender, CellEventArgs e);

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);

		public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

		public delegate void ColumnReorderedEventHandler(object sender, ColumnReorderedEventArgs e);

		public delegate void RowScrollEvent(object sender, EventArgs e);

		public delegate void ColumnScrollEvent(object sender, EventArgs e);

		public delegate void CellDroppedEvent(object sender, CellEventArgs e);

		#endregion

		#region Mouse and Key Events

		private bool _columnDownMoved;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			_currentX = e.X;
			_currentY = e.Y;

			if (_columnDown != null)
			{
				_columnDownMoved = true;
			}

			Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);			
			
			newCell.RowIndex += FirstVisibleRow;
			if (newCell.RowIndex < 0)
			{
				newCell.RowIndex = 0;
			}

			if (!newCell.Equals(CurrentCell))
			{
				CellChanged(newCell);

				if (IsHoveringOnColumnCell ||
					(WasHoveringOnColumnCell && !IsHoveringOnColumnCell))
				{
					Refresh();
				}
				else if (_columnDown != null)
				{
					Refresh();
				}
			}
			else if (_columnDown != null)  // Kind of silly feeling to have this check twice, but the only alternative I can think of has it refreshing twice when pointed column changes with column down, and speed matters
			{
				Refresh();
			}

			if (_columnSeparatorDown != null)
			{
				// column is being resized
				DoColumnResize();
				Refresh();
			}

			// cursor changes
			if (IsHoveringOnDraggableColumnDivide && AllowColumnResize)
				Cursor.Current = Cursors.VSplit;
			else if (IsHoveringOnColumnCell && AllowColumnReorder)
				Cursor.Current = Cursors.Hand;
			else
				Cursor.Current = Cursors.Default;

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
			_currentX = null;
			_currentY = null;
			CurrentCell = null;
			IsPaintDown = false;
			_hoverTimer.Stop();
			Cursor.Current = Cursors.Default;
			Refresh();
			base.OnMouseLeave(e);
		}

		// TODO add query callback of whether to select the cell or not
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (!GlobalWin.MainForm.EmulatorPaused && _currentX.HasValue)
			{
				// copypaste from OnMouseMove()
				Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);

				newCell.RowIndex += FirstVisibleRow;
				if (newCell.RowIndex < 0)
				{
					newCell.RowIndex = 0;
				}

				if (!newCell.Equals(CurrentCell))
				{
					CellChanged(newCell);

					if (IsHoveringOnColumnCell ||
						(WasHoveringOnColumnCell && !IsHoveringOnColumnCell))
					{
						Refresh();
					}
					else if (_columnDown != null)
					{
						Refresh();
					}
				}
				else if (_columnDown != null)
				{
					Refresh();
				}
			}

			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnDraggableColumnDivide && AllowColumnResize)
				{
					_columnSeparatorDown = ColumnAtX(_currentX.Value);
				}
				else if (IsHoveringOnColumnCell && AllowColumnReorder)
				{
					_columnDown = CurrentCell.Column;
				}
				else if (InputPaintingMode)
				{
					IsPaintDown = true;
				}
			}

			if (e.Button == MouseButtons.Right)
			{
				if (!IsHoveringOnColumnCell)
				{
					RightButtonHeld = true;
				}
			}

			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnDataCell)
				{
					if (ModifierKeys == Keys.Alt)
					{
						// do marker drag here
					}
					else if (ModifierKeys == Keys.Shift && (CurrentCell.Column.Type == ListColumn.InputType.Text))
					{
						if (_selectedItems.Any())
						{
							if (FullRowSelect)
							{
								var selected = _selectedItems.Any(c => c.RowIndex.HasValue && CurrentCell.RowIndex.HasValue && c.RowIndex == CurrentCell.RowIndex);

								if (!selected)
								{
									var rowIndices = _selectedItems
										.Where(c => c.RowIndex.HasValue)
										.Select(c => c.RowIndex ?? -1)
										.Where(c => c >= 0) // Hack to avoid possible Nullable exceptions
										.Distinct()
										.ToList();

									var firstIndex = rowIndices.Min();
									var lastIndex = rowIndices.Max();

									if (CurrentCell.RowIndex.Value < firstIndex)
									{
										for (int i = CurrentCell.RowIndex.Value; i < firstIndex; i++)
										{
											SelectCell(new Cell
											{
												RowIndex = i,
												Column = CurrentCell.Column
											});
										}
									}
									else if (CurrentCell.RowIndex.Value > lastIndex)
									{
										for (int i = lastIndex + 1; i <= CurrentCell.RowIndex.Value; i++)
										{
											SelectCell(new Cell
											{
												RowIndex = i,
												Column = CurrentCell.Column
											});
										}
									}
									else // Somewhere in between, a scenario that can happen with ctrl-clicking, find the previous and highlight from there
									{
										var nearest = rowIndices
											.Where(x => x < CurrentCell.RowIndex.Value)
											.Max();

										for (int i = nearest + 1; i <= CurrentCell.RowIndex.Value; i++)
										{
											SelectCell(new Cell
											{
												RowIndex = i,
												Column = CurrentCell.Column
											});
										}
									}
								}
							}
							else
							{
								MessageBox.Show("Shift click logic for individual cells has not yet implemented");
							}
						}
						else
						{
							SelectCell(CurrentCell);
						}
					}
					else if (ModifierKeys == Keys.Control && (CurrentCell.Column.Type == ListColumn.InputType.Text))
					{
						SelectCell(CurrentCell, toggle: true);
					}
					else if (ModifierKeys != Keys.Shift)
					{
						var hadIndex = _selectedItems.Any();
						_selectedItems.Clear();
						SelectCell(CurrentCell);
					}

					Refresh();

					SelectedIndexChanged?.Invoke(this, new EventArgs());
				}
			}

			base.OnMouseDown(e);

			if (AllowRightClickSelecton && e.Button == MouseButtons.Right)
			{
				if (!IsHoveringOnColumnCell)
				{
					_currentX = e.X;
					_currentY = e.Y;
					Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
					newCell.RowIndex += FirstVisibleRow;
					CellChanged(newCell);
					SelectCell(CurrentCell);
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (_columnSeparatorDown != null && AllowColumnResize)
			{
				DoColumnResize();				
				Refresh();
			}
			else if (IsHoveringOnColumnCell && AllowColumnReorder)
			{				
				if (_columnDown != null && _columnDownMoved)
				{
					DoColumnReorder();
					_columnDown = null;
					Refresh();
				}
				else if (e.Button == MouseButtons.Left)
				{
					ColumnClickEvent(ColumnAtX(e.X));
				}
				else if (e.Button == MouseButtons.Right)
				{
					ColumnRightClickEvent(ColumnAtX(e.X));
				}
			}

			_columnDown = null;
			_columnDownMoved = false;
			_columnSeparatorDown = null;
			RightButtonHeld = false;
			IsPaintDown = false;
			base.OnMouseUp(e);
		}

		private void IncrementScrollBar(ScrollBar bar, bool increment)
		{
			int newVal;
			if (increment)
			{
				newVal = bar.Value + (bar.SmallChange * ScrollSpeed);
				if (newVal > bar.Maximum - bar.LargeChange)
				{
					newVal = bar.Maximum - bar.LargeChange;
				}
			}
			else
			{
				newVal = bar.Value - (bar.SmallChange * ScrollSpeed);
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
			IncrementScrollBar(_vBar, e.Delta < 0);
			if (_currentX != null)
			{
				OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, _currentX.Value, _currentY.Value, 0));
			}

			Refresh();			
		}

		private void DoRightMouseScroll(object sender, MouseEventArgs e)
		{
			RightMouseScrolled?.Invoke(sender, e);
		}

		private void ColumnClickEvent(ListColumn column)
		{
			ColumnClick?.Invoke(this, new ColumnClickEventArgs(column));
		}

		private void ColumnRightClickEvent(ListColumn column)
		{
			ColumnRightClick?.Invoke(this, new ColumnClickEventArgs(column));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!SuspendHotkeys)
			{
				if (e.Control && !e.Alt && e.Shift && e.KeyCode == Keys.F) // Ctrl+Shift+F
				{
					//HorizontalOrientation ^= true;
				}
				// Scroll
				else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.PageUp) // Page Up
				{
					if (FirstVisibleRow > 0)
					{
						LastVisibleRow = FirstVisibleRow;
						Refresh();
					}
				}
				else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.PageDown) // Page Down
				{
					var totalRows = LastVisibleRow - FirstVisibleRow;
					if (totalRows <= ItemCount)
					{
						var final = LastVisibleRow + totalRows;
						if (final > ItemCount)
						{
							final = ItemCount;
						}

						LastVisibleRow = final;
						Refresh();
					}
				}
				else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.Home) // Home
				{
					FirstVisibleRow = 0;
					Refresh();
				}
				else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.End) // End
				{
					LastVisibleRow = ItemCount;
					Refresh();
				}
				else if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Up
				{
					if (FirstVisibleRow > 0)
					{
						FirstVisibleRow--;
						Refresh();
					}
				}
				else if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Down
				{
					if (FirstVisibleRow < ItemCount - 1)
					{
						FirstVisibleRow++;
						Refresh();
					}
				}
				// Selection courser
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Ctrl + Up
				{
					if (SelectedRows.Any() && LetKeysModifySelection && SelectedRows.First() > 0)
					{
						foreach (var row in SelectedRows.ToList())
						{
							SelectItem(row - 1, true);
							SelectItem(row, false);
						}
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Ctrl + Down
				{
					if (SelectedRows.Any() && LetKeysModifySelection)
					{
						foreach (var row in SelectedRows.Reverse().ToList())
						{
							SelectItem(row + 1, true);
							SelectItem(row, false);
						}
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Left) // Ctrl + Left
				{
					if (SelectedRows.Any() && LetKeysModifySelection)
					{
						SelectItem(SelectedRows.Last(), false);
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Right) // Ctrl + Right
				{
					if (SelectedRows.Any() && LetKeysModifySelection && SelectedRows.Last() < _itemCount - 1)
					{
						SelectItem(SelectedRows.Last() + 1, true);
					}
				}
				else if (e.Control && e.Shift && !e.Alt && e.KeyCode == Keys.Left) // Ctrl + Shift + Left
				{
					if (SelectedRows.Any() && LetKeysModifySelection && SelectedRows.First() > 0)
					{
						SelectItem(SelectedRows.First() - 1, true);
					}
				}
				else if (e.Control && e.Shift && !e.Alt && e.KeyCode == Keys.Right) // Ctrl + Shift + Right
				{
					if (SelectedRows.Any() && LetKeysModifySelection)
					{
						SelectItem(SelectedRows.First(), false);
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.PageUp) // Ctrl + Page Up
				{
					//jump to above marker with selection courser
					if (LetKeysModifySelection)
					{

					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.PageDown) // Ctrl + Page Down
				{
					//jump to below marker with selection courser
					if (LetKeysModifySelection)
					{

					}

				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Home) // Ctrl + Home
				{
					//move selection courser to frame 0
					if (LetKeysModifySelection)
					{
						DeselectAll();
						SelectItem(0, true);
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.End) // Ctrl + End
				{
					//move selection courser to end of movie
					if (LetKeysModifySelection)
					{
						DeselectAll();
						SelectItem(ItemCount - 1, true);
					}
				}
			}

			base.OnKeyDown(e);
		}

		#endregion

		#region Change Events

		protected override void OnResize(EventArgs e)
		{
			RecalculateScrollBars();
			if (BorderSize > 0 && this.Parent != null)
			{
				// refresh the parent control to regen the border
				this.Parent.Refresh();
			}
			base.OnResize(e);
			Refresh();
			
			
			
		}

		/// <summary>
		/// Call this function to change the CurrentCell to newCell
		/// </summary>
		private void CellChanged(Cell newCell)
		{
			LastCell = CurrentCell;
			CurrentCell = newCell;

			if (PointedCellChanged != null &&
				(LastCell.Column != CurrentCell.Column || LastCell.RowIndex != CurrentCell.RowIndex))
			{
				PointedCellChanged(this, new CellEventArgs(LastCell, CurrentCell));
			}

			if (CurrentCell?.Column != null && CurrentCell.RowIndex.HasValue)
			{
				_hoverTimer.Start();
			}
			else
			{
				_hoverTimer.Stop();
			}
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

			RowScroll?.Invoke(this, e);
		}

		private void HorizontalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

			ColumnScroll?.Invoke(this, e);
		}

		private void ColumnChangedCallback()
		{
			RecalculateScrollBars();
			if (_columns.VisibleColumns.Any())
			{
				ColumnWidth = _columns.VisibleColumns.Max(c => c.Width.Value) + CellWidthPadding * 4;
			}
		}

		

		#endregion
	}
}
