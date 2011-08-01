using System;

namespace BizHawk.Emulation
{
    // If you're wondering what the point of this is: It's mostly to have .Clear() be fast.
    // only intended to be used with value types. If used on references you may get GC issues.
    // Also, being in the same assembly means the JITer might inline these calls.
    // There is less overhead by not being dynamically resizable and stuff.
    public sealed class QuickList<T> where T : struct
    {
        private T[] buffer;
        public int Position;

        public QuickList(int capacity)
        {
            buffer = new T[capacity];
        }

        public T this[int index]
        {
            get { return buffer[index]; }
            set { buffer[index] = value; }
        }

        public int Count
        {
            get { return Position; }
        }

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
    public class QuickQueue<T> where T : struct
    {
        private T[] buffer;
        private int head;
        private int tail;
        private int size;

        public QuickQueue(int capacity)
        {
            buffer = new T[capacity];
        }

        public int Count { get { return tail - head; } }

        public void Enqueue(T item)
        {
            if (size >= buffer.Length)
                throw new Exception("QuickQueue capacity breached!");

            buffer[tail] = item;
            tail = (tail + 1) % buffer.Length;
            size++;
        }

        public T Dequeue()
        {
            if (size == 0)
                throw new Exception("QuickQueue is empty!");

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

        // TODO serialization functions
    }
}
