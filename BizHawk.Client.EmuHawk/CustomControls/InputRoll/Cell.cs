using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Represents a single cell of the <seealso cref="InputRoll"/>
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

		public bool IsDataCell => Column != null && RowIndex.HasValue;

		public override bool Equals(object obj)
		{
			var cell = obj as Cell;
			if (cell != null)
			{
				return Column == cell.Column && RowIndex == cell.RowIndex;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Column.GetHashCode() + RowIndex.GetHashCode();
		}

		public static bool operator ==(Cell a, Cell b)
		{
			if (ReferenceEquals(a, null))
			{
				return ReferenceEquals(b, null);
			}

			return a.Equals(b);
		}

		public static bool operator !=(Cell a, Cell b)
		{
			return !(a == b);
		}
	}

	internal class SortCell : IComparer<Cell>
	{
		int IComparer<Cell>.Compare(Cell c1, Cell c2)
		{
			if (c1 == null && c2 == null)
			{
				return 0;
			}

			if (c2 == null)
			{
				return 1;
			}

			if (c1 == null)
			{
				return -1;
			}

			if (c1.RowIndex.HasValue)
			{
				if (c2.RowIndex.HasValue)
				{
					int row = c1.RowIndex.Value.CompareTo(c2.RowIndex.Value);
					return row == 0
						? c1.Column.Name.CompareTo(c2.Column.Name)
						: row;
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

	public static class CellExtensions
	{
		public static bool IsDataCell(this Cell cell)
		{
			return cell != null && cell.RowIndex != null && cell.Column != null;
		}
	}
}
