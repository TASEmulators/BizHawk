#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	public class RollColumns : List<RollColumn>
	{
		public RollColumn? this[string name]
			=> this.SingleOrDefault(column => column.Name == name);

		public IEnumerable<RollColumn> VisibleColumns => this.Where(c => c.Visible);

		public Action? ChangedCallback { get; set; } = null;

		// TODO: this shouldn't be exposed.  But in order to not expose it, each RollColumn must have a change callback, and all property changes must call it, it is quicker and easier to just call this when needed
		public void ColumnsChanged()
		{
			int pos = 0;

			foreach (var col in VisibleColumns)
			{
				col.Left = pos;
				pos += col.Width;
				col.Right = pos;
			}

			ChangedCallback?.Invoke();
		}

		public new void Add(RollColumn column)
		{
			if (this[column.Name] == null)
			{
				base.Add(column);
				ColumnsChanged();
			}
		}

		public new void AddRange(IEnumerable<RollColumn> collection)
		{
			foreach (var column in collection)
			{
				if (this[column.Name] == null)
				{
					Add(column);
				}
			}

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
			var items = collection.ToList();
			foreach (var column in items)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					throw new InvalidOperationException("A column with this name already exists.");
				}
			}

			base.InsertRange(index, items);
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
	}
}
