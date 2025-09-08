namespace BizHawk.Emulation.Cores.PCEngine
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
				throw new Exception($"{nameof(QuickQueue<>)} capacity breached!");

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
				throw new Exception($"{nameof(QuickQueue<>)} is empty!");

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
}