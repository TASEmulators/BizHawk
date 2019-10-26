using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	public class RollColumns : List<RollColumn>
		{
			public RollColumn this[string name]
			{
				get
				{
					return this.SingleOrDefault(column => column.Name == name);
				}
			}

			public IEnumerable<RollColumn> VisibleColumns
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
					System.Diagnostics.Debug.Fail($"{nameof(ChangedCallback)} has died!");
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

				foreach (var col in VisibleColumns)
				{
					col.Left = pos;
					pos += col.Width.Value;
					col.Right = pos;
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
}
