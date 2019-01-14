using System.IO;
using SharpCompress.Common;

namespace SharpCompress.IO
{
    internal class ListeningStream : Stream
    {
        private long currentEntryTotalReadBytes;
        private readonly IExtractionListener listener;

        public ListeningStream(IExtractionListener listener, Stream stream)
        {
            Stream = stream;
            this.listener = listener;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public Stream Stream { get; }

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => Stream.CanSeek;

        public override bool CanWrite => Stream.CanWrite;

        public override void Flush()
        {
            Stream.Flush();
        }

        public override long Length => Stream.Length;

        public override long Position { get => Stream.Position; set => Stream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = Stream.Read(buffer, offset, count);
            currentEntryTotalReadBytes += read;
            listener.FireCompressedBytesRead(currentEntryTotalReadBytes, currentEntryTotalReadBytes);
            return read;
        }

        public override int ReadByte()
        {
            int value = Stream.ReadByte();
            if (value == -1)
            {
                return -1;
            }

            ++currentEntryTotalReadBytes;
            listener.FireCompressedBytesRead(currentEntryTotalReadBytes, currentEntryTotalReadBytes);
            return value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }
    }
}