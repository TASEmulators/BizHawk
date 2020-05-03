#nullable disable

using System;
using System.Collections.Generic;

namespace BizHawk.Common
{
	// If you're wondering what the point of this is: It's mostly to have .Clear() be fast.
	// only intended to be used with value types. If used on references you may get GC issues.
	// Also, being in the same assembly means the JITer might inline these calls.
	// There is less overhead by not being dynamically resizable and stuff.
	public sealed class QuickList<T> where T : struct
	{
		public T[] buffer;
		public int Position;

		public QuickList(int capacity)
		{
			buffer = new T[capacity];
		}

		public T this[int index]
		{
			get => buffer[index];
			set => buffer[index] = value;
		}

		public int Count => Position;

		public void Add(T item)
		{
			buffer[Position++] = item;
		}

		public void Clear()
		{
			Position = 0;
		}
	}

	// and the point of this one is to be easier to serialize quickly. AND fast to clear.
	// only intended to be used with value types. If used on references you may get GC issues.
	public sealed class QuickQueue<T> where T : struct
	{
		public T[] buffer;
		public int head;
		public int tail;
		public int size;

		public QuickQueue(int capacity)
		{
			buffer = new T[capacity];
		}

		public int Count => size;

		/// <exception cref="Exception">called while at capacity</exception>
		public void Enqueue(T item)
		{
			if (size >= buffer.Length)
				throw new Exception($"{nameof(QuickQueue<T>)} capacity breached!");

			buffer[tail] = item;
			tail = (tail + 1) % buffer.Length;
			size++;
		}

		public T[] ToArray(int elemSize)
		{
			T[] ret = new T[size];
			int todo;
			if (tail > head) todo = tail - head;
			else todo = buffer.Length - head;
			Buffer.BlockCopy(buffer, head, ret, 0, elemSize * todo);
			int todo2;
			if (tail < head) todo2 = tail;
			else todo2 = 0;
			if (todo2 != 0) Buffer.BlockCopy(buffer, 0, ret, todo, elemSize * todo2);
			return ret;
		}

		/// <exception cref="Exception">called while empty</exception>
		public T Dequeue()
		{
			if (size == 0)
				throw new Exception($"{nameof(QuickQueue<T>)} is empty!");

			T item = buffer[head];
			head = (head + 1) % buffer.Length;
			size--;
			return item;
		}

		public void Clear()
		{
			head = 0;
			tail = 0;
			size = 0;
		}

		public T[] GetBuffer()
		{
			return buffer;
		}

		public void SignalBufferFilled(int count)
		{
			head = 0;
			tail = count;
			size = count;
		}
	}

	// .net has no built-in read only dictionary
	public sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> dict;

		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
		{
			dict = dictionary;
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void Add(TKey key, TValue value)
		{
			throw new InvalidOperationException();
		}

		public bool ContainsKey(TKey key)
		{
			return dict.ContainsKey(key);
		}

		public ICollection<TKey> Keys => dict.Keys;

		/// <exception cref="InvalidOperationException">always</exception>
		public bool Remove(TKey key)
		{
			throw new InvalidOperationException();
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dict.TryGetValue(key, out value);
		}

		public ICollection<TValue> Values => dict.Values;

		/// <exception cref="InvalidOperationException">(from setter) always</exception>
		public TValue this[TKey key]
		{
			get => dict[key];
			set => throw new InvalidOperationException();
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			throw new InvalidOperationException();
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void Clear()
		{
			throw new InvalidOperationException();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return dict.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			dict.CopyTo(array, arrayIndex);
		}

		public int Count => dict.Count;

		public bool IsReadOnly => true;

		/// <exception cref="InvalidOperationException">always</exception>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new InvalidOperationException();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return dict.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)dict).GetEnumerator();
		}
	}
}