using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A performant VirtualListView implementation that doesn't rely on native Win32 API calls
	/// (and in fact does not inherit the ListView class at all)
	/// It is an enhanced version of the work done with GDI+ rendering in InputRoll.cs
	/// -------------------
	/// *** API Related ***
	/// -------------------
	/// </summary>
	public partial class PlatformAgnosticVirtualListView
	{
		private Cell _draggingCell;

		#region Methods

		/// <summary>
		/// Parent form calls this to add columns
		/// </summary>
		/// <param name="columnName"></param>
		/// <param name="columnText"></param>
		/// <param name="columnWidth"></param>
		/// <param name="columnType"></param>
		public void AddColumn(string columnName, string columnText, int columnWidth, ListColumn.InputType columnType = ListColumn.InputType.Boolean)
		{
			if (AllColumns[columnName] == null)
			{
				var column = new ListColumn
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
					Type = columnType
				};

				AllColumns.Add(column);
			}
		}

		/// <summary>
		/// Sets the state of the passed row index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="val"></param>
		public void SelectItem(int index, bool val)
		{
			if (_columns.VisibleColumns.Any())
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
					IEnumerable<Cell> items = _selectedItems.Where(cell => cell.RowIndex == index);
					_selectedItems.RemoveWhere(items.Contains);
				}
			}
		}

		public void SelectAll()
		{
			var oldFullRowVal = FullRowSelect;
			FullRowSelect = true;
			for (int i = 0; i < ItemCount; i++)
			{
				SelectItem(i, true);
			}

			FullRowSelect = oldFullRowVal;
		}

		public void DeselectAll()
		{
			_selectedItems.Clear();
		}

		public void TruncateSelection(int index)
		{
			_selectedItems.RemoveWhere(cell => cell.RowIndex > index);
		}

		public bool IsVisible(int index)
		{
			return (index >= FirstVisibleRow) && (index <= LastFullyVisibleRow);
		}

		public bool IsPartiallyVisible(int index)
		{
			return index >= FirstVisibleRow && index <= LastVisibleRow;
		}

		public void DragCurrentCell()
		{
			_draggingCell = CurrentCell;
		}

		public void ReleaseCurrentCell()
		{
			if (_draggingCell != null)
			{
				var draggedCell = _draggingCell;
				_draggingCell = null;

				if (CurrentCell != draggedCell)
				{
					CellDropped?.Invoke(this, new CellEventArgs(draggedCell, CurrentCell));
				}
			}
		}

		/// <summary>
		/// Scrolls to the given index, according to the scroll settings.
		/// </summary>
		public void ScrollToIndex(int index)
		{
			if (ScrollMethod == "near")
			{
				MakeIndexVisible(index);
			}

			if (!IsVisible(index) || AlwaysScroll)
			{
				if (ScrollMethod == "top")
				{
					FirstVisibleRow = index;
				}
				else if (ScrollMethod == "bottom")
				{
					LastVisibleRow = index;
				}
				else if (ScrollMethod == "center")
				{
					FirstVisibleRow = Math.Max(index - (VisibleRows / 2), 0);
				}
			}
		}

		/// <summary>
		/// Scrolls so that the given index is visible, if it isn't already; doesn't use scroll settings.
		/// </summary>
		public void MakeIndexVisible(int index)
		{
			if (!IsVisible(index))
			{
				if (FirstVisibleRow > index)
				{
					FirstVisibleRow = index;
				}
				else
				{
					LastVisibleRow = index;
				}
			}
		}

		/// <summary>
		/// Compatibility method from VirtualListView
		/// </summary>
		/// <param name="index"></param>
		public void ensureVisible()
		{
			if (_selectedItems.Count != 0)
				MakeIndexVisible(_selectedItems.Last().RowIndex.Value);
		}

		public void ClearSelectedRows()
		{
			_selectedItems.Clear();
		}

		#endregion		
	}
}
