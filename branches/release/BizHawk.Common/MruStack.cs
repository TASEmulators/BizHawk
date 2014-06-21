namespace BizHawk.Common
{
	public class MruStack<T>
	{
		private readonly T[] _store;
		private int _count;
		private int _head;

		public MruStack(int capacity)
		{
			_store = new T[capacity];
			Clear();
		}

		public int Count { get { return _count; } }

		public void Clear()
		{
			_head = 0;
			_count = 0;
			for (int i = 0; i < _store.Length; i++)
			{
				_store[i] = default(T);
			}
		}

		public void Push(T value)
		{
			_store[_head] = value;
			_head = (_head + 1) % _store.Length;

			if (_count < _store.Length)
			{
				_count++;
			}
		}

		public T Pop()
		{
			if (_count == 0)
			{
				return default(T);
			}

			_head--;
			if (_head < 0)
			{
				_head = _store.Length - 1;
			}

			_count--;
			T value = _store[_head];
			_store[_head] = default(T);
			return value;
		}

		public bool HasElements()
		{
			return _count > 0;
		}
	}
}