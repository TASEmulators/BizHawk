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
	/// ----------------------
	/// *** Helper Methods ***
	/// ----------------------
	/// </summary>
	public partial class PlatformAgnosticVirtualListView
	{
		// TODO: Make into an extension method
		private static Color Add(Color color, int val)
		{
			var col = color.ToArgb();
			col += val;
			return Color.FromArgb(col);
		}

		private void DoColumnReorder()
		{
			if (_columnDown != CurrentCell.Column)
			{
				var oldIndex = _columns.IndexOf(_columnDown);
				var newIndex = _columns.IndexOf(CurrentCell.Column);

				ColumnReordered?.Invoke(this, new ColumnReorderedEventArgs(oldIndex, newIndex, _columnDown));

				_columns.Remove(_columnDown);
				_columns.Insert(newIndex, _columnDown);
			}
		}

		/// <summary>
		/// Helper method for implementations that do not make use of ColumnReorderedEventArgs related callbacks
		/// Basically, each column stores its initial index when added in .OriginalIndex
		/// </summary>
		/// <param name="currIndex"></param>
		/// <returns></returns>
		public int GetOriginalColumnIndex(int currIndex)
		{
			return AllColumns[currIndex].OriginalIndex;
		}

		// ScrollBar.Maximum = DesiredValue + ScrollBar.LargeChange - 1
		// See MSDN Page for more information on the dumb ScrollBar.Maximum Property
		private void RecalculateScrollBars()
		{
			if (_vBar == null || _hBar == null)
				return;

			UpdateDrawSize();

			var columns = _columns.VisibleColumns.ToList();

			if (CellHeight == 0) CellHeight++;
			NeedsVScrollbar = ItemCount > 1;
			NeedsHScrollbar = TotalColWidth.HasValue && TotalColWidth.Value - DrawWidth + 1 > 0;

			UpdateDrawSize();
			if (VisibleRows > 0)
			{
				_vBar.Maximum = Math.Max((VisibleRows - 1) * CellHeight, _vBar.Maximum); // ScrollBar.Maximum is dumb
				_vBar.LargeChange = (VisibleRows - 1) * CellHeight;
				// DrawWidth can be negative if the TAStudio window is small enough
				// Clamp LargeChange to 0 here to prevent exceptions
				_hBar.LargeChange = Math.Max(0, DrawWidth / 2);
			}

			// Update VBar
			if (NeedsVScrollbar)
			{
				_vBar.Maximum = RowsToPixels(ItemCount + 1) - (CellHeight * 3) + _vBar.LargeChange - 1;

				_vBar.Location = new Point(Width - _vBar.Width, 0);
				_vBar.Height = Height;
				_vBar.Visible = true;
			}
			else
			{
				_vBar.Visible = false;
				_vBar.Value = 0;
			}

			// Update HBar
			if (NeedsHScrollbar)
			{
				_hBar.Maximum = TotalColWidth.Value - DrawWidth + _hBar.LargeChange;

				_hBar.Location = new Point(0, Height - _hBar.Height);
				_hBar.Width = Width - (NeedsVScrollbar ? (_vBar.Width + 1) : 0);
				_hBar.Visible = true;
			}
			else
			{
				_hBar.Visible = false;
				_hBar.Value = 0;
			}
		}

		private void UpdateDrawSize()
		{
			if (NeedsVScrollbar)
			{
				DrawWidth = Width - _vBar.Width;
			}
			else
			{
				DrawWidth = Width;
			}
			if (NeedsHScrollbar)
			{
				DrawHeight = Height - _hBar.Height;
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
		private void SelectCell(Cell cell, bool toggle = false)
		{
			if (cell.RowIndex.HasValue && cell.RowIndex < ItemCount)
			{
				if (!MultiSelect)
				{
					_selectedItems.Clear();
				}

				if (FullRowSelect)
				{
					if (toggle && _selectedItems.Any(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex))
					{
						var items = _selectedItems
							.Where(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex)
							.ToList();

						foreach (var item in items)
						{
							_selectedItems.Remove(item);
						}
					}
					else
					{
						foreach (var column in _columns)
						{
							_selectedItems.Add(new Cell
							{
								RowIndex = cell.RowIndex,
								Column = column
							});
						}
					}
				}
				else
				{
					if (toggle && _selectedItems.Any(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex))
					{
						var item = _selectedItems
							.FirstOrDefault(x => x.Equals(cell));

						if (item != null)
						{
							_selectedItems.Remove(item);
						}
					}
					else
					{
						_selectedItems.Add(CurrentCell);
					}
				}
			}
		}

		private bool IsHoveringOnDraggableColumnDivide => 
			IsHoveringOnColumnCell && 
			((_currentX <= CurrentCell.Column.Left + 2 && CurrentCell.Column.Index != 0) || 
			(_currentX >= CurrentCell.Column.Right - 2 && CurrentCell.Column.Index != _columns.Count - 1));	

		private bool IsHoveringOnColumnCell => CurrentCell?.Column != null && !CurrentCell.RowIndex.HasValue;		

		private bool IsHoveringOnDataCell => CurrentCell?.Column != null && CurrentCell.RowIndex.HasValue;

		private bool WasHoveringOnColumnCell => LastCell?.Column != null && !LastCell.RowIndex.HasValue;

		private bool WasHoveringOnDataCell => LastCell?.Column != null && LastCell.RowIndex.HasValue;

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
			var columns = _columns.VisibleColumns.ToList();

			// If pointing to a column header
			if (columns.Any())
			{
				newCell.RowIndex = PixelsToRows(y);
				newCell.Column = ColumnAtX(x);
			}

			if (!(IsPaintDown || RightButtonHeld) && newCell.RowIndex <= -1) // -2 if we're entering from the top
			{
				newCell.RowIndex = null;
			}

			return newCell;
		} 


		private void CalculateColumnToResize()
		{
			// if this is reached, we are already over a selectable column divide
			_columnSeparatorDown = ColumnAtX(_currentX.Value);
		}

		private void DoColumnResize()
		{
			var widthChange = _currentX - _columnSeparatorDown.Right;
			_columnSeparatorDown.Width += widthChange;
			if (_columnSeparatorDown.Width < MinimumColumnSize)
				_columnSeparatorDown.Width = MinimumColumnSize;
			AllColumns.ColumnsChanged();
		}

		// A boolean that indicates if the InputRoll is too large vertically and requires a vertical scrollbar.
		private bool NeedsVScrollbar { get; set; }

		// A boolean that indicates if the InputRoll is too large horizontally and requires a horizontal scrollbar.
		private bool NeedsHScrollbar { get; set; }

		/// <summary>
		/// Updates the width of the supplied column.
		/// <remarks>Call when changing the ColumnCell text, CellPadding, or text font.</remarks>
		/// </summary>
		/// <param name="col">The RollColumn object to update.</param>
		/// <returns>The new width of the RollColumn object.</returns>
		private int UpdateWidth(ListColumn col)
		{
			col.Width = (col.Text.Length * _charSize.Width) + (CellWidthPadding * 4);
			return col.Width.Value;
		}

		/// <summary>
		/// Gets the total width of all the columns by using the last column's Right property.
		/// </summary>
		/// <returns>A nullable Int representing total width.</returns>
		private int? TotalColWidth
		{
			get
			{
				if (_columns.VisibleColumns.Any())
				{
					return _columns.VisibleColumns.Last().Right;
				}

				return null;
			}
		}

		/// <summary>
		/// Returns the RollColumn object at the specified visible x coordinate. Coordinate should be between 0 and Width of the InputRoll Control.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <returns>RollColumn object that contains the x coordinate or null if none exists.</returns>
		private ListColumn ColumnAtX(int x)
		{
			foreach (ListColumn column in _columns.VisibleColumns)
			{
				if (column.Left.Value - _hBar.Value <= x && column.Right.Value - _hBar.Value >= x)
				{
					return column;
				}
			}

			return null;
		}

		/// <summary>
		/// Converts a row number to a horizontal or vertical coordinate.
		/// </summary>
		/// <returns>A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</returns>
		private int RowsToPixels(int index)
		{
			return (index * CellHeight) + ColumnHeight;
		}

		/// <summary>
		/// Converts a horizontal or vertical coordinate to a row number.
		/// </summary>
		/// <param name="pixels">A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</param>
		/// <returns>A row number between 0 and VisibleRows if it is a Datarow, otherwise a negative number if above all Datarows.</returns>
		private int PixelsToRows(int pixels)
		{
			// Using Math.Floor and float because integer division rounds towards 0 but we want to round down.
			if (CellHeight == 0)
				CellHeight++;

			return (int)Math.Floor((float)(pixels - ColumnHeight) / CellHeight);
		}

		// The width of the largest column cell in Horizontal Orientation
		private int ColumnWidth { get; set; }

		// The height of a column cell in Vertical Orientation.
		private int ColumnHeight { get; set; }

		// The width of a cell in Horizontal Orientation. Only can be changed by changing the Font or CellPadding.
		private int CellWidth { get; set; }

		[Browsable(false)]
		public int RowHeight => CellHeight;

		/// <summary>
		/// Gets or sets a value indicating the height of a cell in Vertical Orientation. Only can be changed by changing the Font or CellPadding.
		/// </summary>
		private int CellHeight { get; set; }

		/// <summary>
		/// Call when _charSize, MaxCharactersInHorizontal, or CellPadding is changed.
		/// </summary>
		private void UpdateCellSize()
		{
			CellHeight = _charSize.Height + (CellHeightPadding * 2);
			CellWidth = (_charSize.Width/* * MaxCharactersInHorizontal*/) + (CellWidthPadding * 4); // Double the padding for horizontal because it looks better
		}
		/*
		/// <summary>
		/// Call when _charSize, MaxCharactersInHorizontal, or CellPadding is changed.
		/// </summary>
		private void UpdateColumnSize()
		{

		}
		*/

		// Number of displayed + hidden frames, if fps is as expected
		private int ExpectedDisplayRange()
		{
			return (VisibleRows + 1); // * LagFramesToHide;
		}
	}
}
