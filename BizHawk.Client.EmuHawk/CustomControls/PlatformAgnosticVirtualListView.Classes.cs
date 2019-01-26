using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A performant VirtualListView implementation that doesn't rely on native Win32 API calls
	/// (and in fact does not inherit the ListView class at all)
	/// It is an enhanced version of the work done with GDI+ rendering in InputRoll.cs
	/// ---------------
	/// *** Classes ***
	/// ---------------
	/// </summary>
	public partial class PlatformAgnosticVirtualListView
	{
		#region Event Args

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

		public class ColumnClickEventArgs
		{
			public ColumnClickEventArgs(ListColumn column)
			{
				Column = column;
			}

			public ListColumn Column { get; private set; }
		}

		public class ColumnReorderedEventArgs
		{
			public ColumnReorderedEventArgs(int oldDisplayIndex, int newDisplayIndex, ListColumn column)
			{
				Column = column;
				OldDisplayIndex = oldDisplayIndex;
				NewDisplayIndex = newDisplayIndex;
			}

			public ListColumn Column { get; private set; }
			public int OldDisplayIndex { get; private set; }
			public int NewDisplayIndex { get; private set; }
		}

		#endregion

		#region Columns

		public class ListColumn
		{
			public enum InputType { Boolean, Float, Text, Image }

			public int Index { get; set; }
			public int OriginalIndex { get; set; }  // for implementations that dont use ColumnReorderedEventArgs
			public string Group { get; set; }
			public int? Width { get; set; }
			public int? Left { get; set; }
			public int? Right { get; set; }
			public string Name { get; set; }
			public string Text { get; set; }
			public InputType Type { get; set; }
			public bool Visible { get; set; }

			/// <summary>
			/// Column will be drawn with an emphasized look, if true
			/// </summary>
			private bool _emphasis;
			public bool Emphasis
			{
				get { return _emphasis; }
				set { _emphasis = value; }
			}

			public ListColumn()
			{
				Visible = true;
			}
		}

		public class ListColumns : List<ListColumn>
		{
			public ListColumn this[string name]
			{
				get
				{
					return this.SingleOrDefault(column => column.Name == name);
				}
			}

			public IEnumerable<ListColumn> VisibleColumns
			{
				get
				{
					return this.Where(c => c.Visible);
				}
			}

			public Action ChangedCallback { get; set; }

			private void DoChangeCallback()
			{
				// no check will make it crash for user too, not sure which way of alarm we prefer. no alarm at all will cause all sorts of subtle bugs
				if (ChangedCallback == null)
				{
					System.Diagnostics.Debug.Fail("ColumnChangedCallback has died!");
				}
				else
				{
					ChangedCallback();
				}
			}

			// TODO: this shouldn't be exposed.  But in order to not expose it, each RollColumn must have a change callback, and all property changes must call it, it is quicker and easier to just call this when needed
			public void ColumnsChanged()
			{
				int pos = 0;

				var columns = VisibleColumns.ToList();

				for (int i = 0; i < columns.Count; i++)
				{
					columns[i].Left = pos;
					pos += columns[i].Width.Value;
					columns[i].Right = pos;
				}

				DoChangeCallback();
			}

			public new void Add(ListColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					// The designer sucks, doing nothing for now
					return;
					//throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Add(column);
				// save the original index for implementations that do not use ColumnReorderedEventArgs
				column.OriginalIndex = this.IndexOf(column);
				ColumnsChanged();
			}

			public new void AddRange(IEnumerable<ListColumn> collection)
			{
				foreach (var column in collection)
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

			public new void Insert(int index, ListColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Insert(index, column);
				ColumnsChanged();
			}

			public new void InsertRange(int index, IEnumerable<ListColumn> collection)
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

			public new bool Remove(ListColumn column)
			{
				var result = base.Remove(column);
				ColumnsChanged();
				return result;
			}

			public new int RemoveAll(Predicate<ListColumn> match)
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

		#endregion

		#region Cells

		/// <summary>
		/// Represents a single cell of the Roll
		/// </summary>
		public class Cell
		{
			public ListColumn Column { get; internal set; }
			public int? RowIndex { get; internal set; }
			public string CurrentText { get; internal set; }

			public Cell() { }

			public Cell(Cell cell)
			{
				Column = cell.Column;
				RowIndex = cell.RowIndex;
			}

			public bool IsDataCell => Column != null && RowIndex.HasValue;

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

		private class SortCell : IComparer<Cell>
		{
			int IComparer<Cell>.Compare(Cell a, Cell b)
			{
				Cell c1 = a as Cell;
				Cell c2 = b as Cell;
				if (c1.RowIndex.HasValue)
				{
					if (c2.RowIndex.HasValue)
					{
						int row = c1.RowIndex.Value.CompareTo(c2.RowIndex.Value);
						if (row == 0)
						{
							return c1.Column.Name.CompareTo(c2.Column.Name);
						}

						return row;
					}

					return 1;
				}

				if (c2.RowIndex.HasValue)
				{
					return -1;
				}

				return c1.Column.Name.CompareTo(c2.Column.Name);
			}
		}

		#endregion
	}
}
