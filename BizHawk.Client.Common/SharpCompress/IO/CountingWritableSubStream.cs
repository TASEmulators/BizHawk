using System;
using System.IO;

namespace SharpCompress.IO
{
    internal class CountingWritableSubStream : NonDisposingStream
    {
        internal CountingWritableSubStream(Stream stream) : base(stream, throwOnDispose: false)
        {
        }

        public ulong Count { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
            Stream.Flush();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
            Count += (uint)count;
        }

        public override void WriteByte(byte value)
        {
            Stream.WriteByte(value);
            ++Count;
        }
    }
}