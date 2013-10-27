namespace BizHawk.MultiClient
{
	public class MruStack<T>
	{
		private readonly T[] store;
		private int count;
		private int head;

		public int Count { get { return count; } }

		public MruStack(int capacity)
		{
			store = new T[capacity];
			Clear();
		}

		public void Clear()
		{
			head = 0;
			count = 0;
			for (int i = 0; i < store.Length; i++)
				store[i] = default(T);
		}

		public void Push(T value)
		{
			store[head] = value;
			head = (head + 1) % store.Length;

			if (count < store.Length)
				count++;
		}

		public T Pop()
		{
			if (count == 0)
				return default(T);

			head--;
			if (head < 0)
				head = store.Length - 1;
			count--;
			T value = store[head];
			store[head] = default(T);
			return value;
		}

		public bool HasElements()
		{
			return count > 0;
		}
	}
}