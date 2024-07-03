#nullable enable

using System.Diagnostics;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Represents a single cell of the <see cref="InputRoll"/>
	/// </summary>
	public sealed class Cell : IComparable<Cell>, IEquatable<Cell>
	{
		public RollColumn? Column { get; internal set; } = null;

		public int? RowIndex { get; internal set; } = null;

		public Cell() { }

		public Cell(Cell cell)
		{
			Column = cell.Column;
			RowIndex = cell.RowIndex;
		}

		public int CompareTo(Cell other)
			=> SortCell.Compare(this, other);

		public bool Equals(Cell? other)
			=> other is not null && Column?.Name == other.Column?.Name && RowIndex == other.RowIndex;

		public override bool Equals(object? obj)
			=> obj is Cell other && Equals(other);

		public override int GetHashCode()
		{
			return Column!.GetHashCode() + RowIndex.GetHashCode();
		}

		public override string ToString()
			=> $"Cell(r: {RowIndex?.ToString() ?? "null"}, c: \"{Column?.Name ?? "(no column)"}\")";

		public static bool operator ==(Cell? a, Cell? b)
			=> a is null ? b is null : a.Equals(b);

		public static bool operator !=(Cell? a, Cell? b)
			=> a is null ? b is not null : !a.Equals(b);
	}

	internal static class SortCell
	{
		public static int Compare(Cell? c1, Cell? c2)
		{
			if (c1 is null && c2 is null)
			{
				return 0;
			}

			if (c2 is null)
			{
				return 1;
			}

			if (c1 is null)
			{
				return -1;
			}

			if (c1.RowIndex is not null)
			{
				if (c2.RowIndex is not null)
				{
					int row = c1.RowIndex.Value.CompareTo(c2.RowIndex.Value);
					return row == 0
						? string.CompareOrdinal(c1.Column?.Name, c2.Column?.Name)
						: row;
				}
					
				return 1;
			}

			if (c2.RowIndex is not null)
			{
				return -1;
			}

			return string.CompareOrdinal(c1.Column!.Name, c2.Column!.Name);
		}
	}

	public sealed class CellList : SortedList<Cell>
	{
		/// <remarks>restore the distinctness invariant from <see cref="System.Collections.Generic.SortedSet{T}"/>; though I don't think we actually rely on it anywhere --yoshi</remarks>
		public override void Add(Cell item)
		{
			var i = _list.BinarySearch(item);
			if (i >= 0)
			{
				Debug.Assert(false, $"{nameof(CellList)}'s distinctness invariant was almost broken! CellList.Add({(item is null ? "null" : item.ToString())})");
				return;
			}
			_list.Insert(~i, item);
		}

		public bool IncludesRow(int rowIndex)
#if false
			=> _list.Exists(cell => cell.RowIndex == rowIndex);
#elif false
		{
			var i = _list.BinarySearch(new() { RowIndex = rowIndex, Column = null });
			return i >= 0 || (~i < _list.Count && _list[~i].RowIndex == rowIndex);
		}
#else
		{
			var i = _list.LowerBoundBinarySearch(static c => c.RowIndex ?? -1, rowIndex);
			return i >= 0 && _list[i].RowIndex == rowIndex;
		}
#endif
	}

	public static class CellExtensions
	{
		public static bool IsDataCell(this Cell? cell)
			=> cell is { RowIndex: not null, Column: not null };
	}
}
