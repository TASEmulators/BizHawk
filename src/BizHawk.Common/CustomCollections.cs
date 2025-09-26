using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common
{
	public partial class SortedList<T> : IList, IList<T>, IReadOnlyList<T>
		where T : IComparable<T>
	{
		private const string ERR_MSG_OUT_OF_ORDER = "setting/inserting elements must preserve ordering";

		private const string ERR_MSG_WRONG_TYPE = "wrong type";

		[CLSCompliant(false)] //TODO just needs renaming
		protected readonly List<T> _list;

		public virtual int Count => _list.Count;

		bool IList.IsFixedSize
			=> false;

		public virtual bool IsReadOnly { get; } = false;

		bool ICollection.IsSynchronized
			=> false;

		object ICollection.SyncRoot
			=> this;

		protected SortedList(List<T> wrapped)
			=> _list = wrapped;

		public SortedList()
			: this(new()) {}

		public SortedList(IEnumerable<T> collection)
			: this(new(collection))
		{
			_list.Sort();
		}

		public virtual T this[int index]
		{
			get => _list[index];
			set
			{
				// NOT allowing appends, to match BCL `List<T>`
				if (index < 0 || Count <= index) throw new ArgumentOutOfRangeException(paramName: nameof(index), index, message: $"index must be in 0..<{Count}");
				if (Count is 0)
				{
					_list.Add(value);
					return;
				}
				var willBeGeqPrevious = index is 0 || value.CompareTo(_list[index - 1]) >= 0;
				var willBeLeqFollowing = index == Count - 1 || _list[index + 1].CompareTo(value) >= 0;
				if (willBeGeqPrevious && willBeLeqFollowing) _list[index] = value;
				else throw new NotSupportedException(ERR_MSG_OUT_OF_ORDER);
			}
		}

		object? IList.this[int index]
		{
			get => _list[index];
			set
			{
				if (value is not T value1) throw new ArgumentException(paramName: nameof(value), message: ERR_MSG_WRONG_TYPE);
				this[index] = value1;
			}
		}

		public virtual void Add(T item)
		{
			var i = _list.BinarySearch(item);
			_list.Insert(i < 0 ? ~i : i, item);
		}

		int IList.Add(object? item)
		{
			if (item is not T item1) throw new ArgumentException(paramName: nameof(item), message: ERR_MSG_WRONG_TYPE);
			Add(item1);
			return IndexOf(item1);
		}

		public virtual int BinarySearch(T item) => _list.BinarySearch(item);

		public virtual void Clear() => _list.Clear();

		public virtual bool Contains(T item) => _list.BinarySearch(item) >= 0;

		bool IList.Contains(object? item)
			=> item is T item1 && Contains(item1);

		public virtual void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		void ICollection.CopyTo(Array array, int arrayIndex)
			=> ((ICollection) _list).CopyTo(array, arrayIndex);

		public T FirstOrDefault()
			=> _list.Count is 0 ? default! : _list[0];

		public virtual IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

		/// <remarks>throws if list is empty</remarks>
		public T Max()
			=> _list[_list.Count - 1];

		/// <remarks>throws if list is empty</remarks>
		public T Min()
			=> _list[0];

		public virtual int IndexOf(T item)
		{
			var i = _list.BinarySearch(item);
			return i < 0 ? -1 : i;
		}

		int IList.IndexOf(object? item)
			=> item is T item1
				? IndexOf(item1)
				: throw new ArgumentException(paramName: nameof(item), message: ERR_MSG_WRONG_TYPE);

		public virtual void Insert(int index, T item)
		{
			// allowing appends per `IList<T>` docs
			if (index < 0 || Count < index) throw new ArgumentOutOfRangeException(paramName: nameof(index), index, message: $"index must be in 0..{Count}");
			if (Count is 0)
			{
				_list.Add(item);
				return;
			}
			var willBeGeqPrevious = index is 0 || item.CompareTo(_list[index - 1]) >= 0;
			var willBeLeqFollowing = index >= Count - 1 || _list[index].CompareTo(item) >= 0;
			if (willBeGeqPrevious && willBeLeqFollowing) _list.Insert(index, item);
			else throw new NotSupportedException(ERR_MSG_OUT_OF_ORDER);
		}

		void IList.Insert(int index, object? item)
		{
			if (item is not T item1) throw new ArgumentException(paramName: nameof(item), message: ERR_MSG_WRONG_TYPE);
			Insert(index, item1);
		}

		public T LastOrDefault()
			=> _list.Count is 0 ? default! : _list[_list.Count - 1];

		public virtual bool Remove(T item)
		{
#if true
			var i = _list.BinarySearch(item);
			if (i < 0) return false;
			_list.RemoveAt(i);
			return true;
#else //TODO is this any slower?
			return _list.Remove(item);
#endif
		}

		void IList.Remove(object? item)
		{
			if (item is not T item1) throw new ArgumentException(paramName: nameof(item), message: ERR_MSG_WRONG_TYPE);
			_ = Remove(item1);
		}


		public virtual int RemoveAll(Predicate<T> match) => _list.RemoveAll(match);

		public virtual void RemoveAt(int index) => _list.RemoveAt(index);

		/// <summary>Remove all items after the specific item (but not the given item).</summary>
		public virtual void RemoveAfter(T item)
		{
			var startIndex = _list.BinarySearch(item);
			if (startIndex < 0)
			{
				// If BinarySearch doesn't find the item,
				// it returns the bitwise complement of the index of the next element
				// that is larger than item
				startIndex = ~startIndex;
			}
			else
			{
				// All items *after* the item
				startIndex++;
			}
			if (startIndex < _list.Count)
			{
				_list.RemoveRange(startIndex, _list.Count - startIndex);
			}
		}

		public SortedList<T> Slice(int start, int length)
			=> new(SliceImpl(start: start, length: length));

		protected List<T> SliceImpl(int start, int length)
			=> _list.Skip(start).Take(length).ToList();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
