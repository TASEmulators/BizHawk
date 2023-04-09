using System;

namespace SharpAudio
{
    /// <summary>
    ///     A thread-safe variable-size circular buffer
    /// </summary>
    public class CircularBuffer
    {
        private byte[] m_Buffer;
        private int m_HeadOffset;
        private int m_TailOffset;

        /// <summary>
        ///     Constructs a new instance of a <see cref="CircularBuffer" />
        /// </summary>
        public CircularBuffer()
        {
            m_Buffer = new byte[2048];
        }

        /// <summary>
        ///     Constructs a new instance of a <see cref="CircularBuffer" /> with the specified capacity
        /// </summary>
        /// <param name="capacity">The number of entries that the <see cref="CircularBuffer" /> can initially contain</param>
        public CircularBuffer(int capacity)
        {
            m_Buffer = new byte[capacity];
        }

        /// <summary>
        ///     Gets the available bytes in the ring buffer
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        ///     Clears the ring buffer
        /// </summary>
        public void Clear()
        {
            Length = 0;
            m_HeadOffset = 0;
            m_TailOffset = 0;
        }

        /// <summary>
        ///     Clears the specified amount of bytes from the ring buffer
        /// </summary>
        /// <param name="size">The amount of bytes to clear from the ring buffer</param>
        public void Clear(int size)
        {
            lock (this)
            {
                if (size > Length) size = Length;

                if (size == 0) return;

                m_HeadOffset = (m_HeadOffset + size) % m_Buffer.Length;
                Length -= size;

                if (Length == 0)
                {
                    m_HeadOffset = 0;
                    m_TailOffset = 0;
                }
            }
        }

        /// <summary>
        ///     Extends the capacity of the ring buffer
        /// </summary>
        private void SetCapacity(int capacity)
        {
            var buffer = new byte[capacity];

            if (Length > 0)
            {
                if (m_HeadOffset < m_TailOffset)
                {
                    Buffer.BlockCopy(m_Buffer, m_HeadOffset, buffer, 0, Length);
                }
                else
                {
                    Buffer.BlockCopy(m_Buffer, m_HeadOffset, buffer, 0, m_Buffer.Length - m_HeadOffset);
                    Buffer.BlockCopy(m_Buffer, 0, buffer, m_Buffer.Length - m_HeadOffset, m_TailOffset);
                }
            }

            m_Buffer = buffer;
            m_HeadOffset = 0;
            m_TailOffset = Length;
        }


        /// <summary>
        ///     Writes a sequence of bytes to the ring buffer
        /// </summary>
        /// <param name="buffer">A byte array containing the data to write</param>
        /// <param name="index">
        ///     The zero-based byte offset in <paramref name="buffer" /> from which to begin copying bytes to the
        ///     ring buffer
        /// </param>
        /// <param name="count">The number of bytes to write</param>
        public void Write<T>(T[] buffer, int index, int count)
        {
            if (count == 0) return;

            lock (this)
            {
                if (Length + count > m_Buffer.Length) SetCapacity((Length + count + 2047) & ~2047);

                if (m_HeadOffset < m_TailOffset)
                {
                    var tailLength = m_Buffer.Length - m_TailOffset;

                    if (tailLength >= count)
                    {
                        Buffer.BlockCopy(buffer, index, m_Buffer, m_TailOffset, count);
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, index, m_Buffer, m_TailOffset, tailLength);
                        Buffer.BlockCopy(buffer, index + tailLength, m_Buffer, 0, count - tailLength);
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, index, m_Buffer, m_TailOffset, count);
                }

                Length += count;
                m_TailOffset = (m_TailOffset + count) % m_Buffer.Length;
            }
        }

        /// <summary>
        ///     Reads a sequence of bytes from the ring buffer and advances the position within the ring buffer by the number of
        ///     bytes read
        /// </summary>
        /// <param name="buffer">The buffer to write the data into</param>
        /// <param name="index">The zero-based byte offset in <paramref name="buffer" /> at which the read bytes will be placed</param>
        /// <param name="count">The maximum number of bytes to read</param>
        /// <returns>
        ///     The total number of bytes read into the buffer. This might be less than the number of bytes requested if that
        ///     number of bytes are not currently available, or zero if the ring buffer is empty
        /// </returns>
        public int Read<T>(T[] buffer, int index, int count)
        {
            lock (this)
            {
                if (count > Length) count = Length;

                if (count == 0) return 0;

                if (m_HeadOffset < m_TailOffset)
                {
                    Buffer.BlockCopy(m_Buffer, m_HeadOffset, buffer, index, count);
                }
                else
                {
                    var tailLength = m_Buffer.Length - m_HeadOffset;

                    if (tailLength >= count)
                    {
                        Buffer.BlockCopy(m_Buffer, m_HeadOffset, buffer, index, count);
                    }
                    else
                    {
                        Buffer.BlockCopy(m_Buffer, m_HeadOffset, buffer, index, tailLength);
                        Buffer.BlockCopy(m_Buffer, 0, buffer, index + tailLength, count - tailLength);
                    }
                }

                Length -= count;
                m_HeadOffset = (m_HeadOffset + count) % m_Buffer.Length;

                if (Length == 0)
                {
                    m_HeadOffset = 0;
                    m_TailOffset = 0;
                }

                return count;
            }
        }
    }
}
