using System;
using System.IO;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Compressors.Rar
{
    internal class RarStream : Stream
    {
        private readonly IRarUnpack unpack;
        private readonly FileHeader fileHeader;
        private readonly Stream readStream;

        private bool fetch;

        private byte[] tmpBuffer = new byte[65536];
        private int tmpOffset;
        private int tmpCount;

        private byte[] outBuffer;
        private int outOffset;
        private int outCount;
        private int outTotal;
        private bool isDisposed;

        public RarStream(IRarUnpack unpack, FileHeader fileHeader, Stream readStream)
        {
            this.unpack = unpack;
            this.fileHeader = fileHeader;
            this.readStream = readStream;
            fetch = true;
            unpack.DoUnpack(fileHeader, readStream, this);
            fetch = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed) {
                isDisposed = true;
                base.Dispose(disposing);
                readStream.Dispose();
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
        }

        public override long Length => fileHeader.UncompressedSize;

        public override long Position { get => fileHeader.UncompressedSize - unpack.DestSize; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            outTotal = 0;
            if (tmpCount > 0)
            {
                int toCopy = tmpCount < count ? tmpCount : count;
                Buffer.BlockCopy(tmpBuffer, tmpOffset, buffer, offset, toCopy);
                tmpOffset += toCopy;
                tmpCount -= toCopy;
                offset += toCopy;
                count -= toCopy;
                outTotal += toCopy;
            }
            if (count > 0 && unpack.DestSize > 0)
            {
                outBuffer = buffer;
                outOffset = offset;
                outCount = count;
                fetch = true;
                unpack.DoUnpack();
                fetch = false;
            }
            return outTotal;
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
            if (!fetch)
            {
                throw new NotSupportedException();
            }
            if (outCount > 0)
            {
                int toCopy = outCount < count ? outCount : count;
                Buffer.BlockCopy(buffer, offset, outBuffer, outOffset, toCopy);
                outOffset += toCopy;
                outCount -= toCopy;
                offset += toCopy;
                count -= toCopy;
                outTotal += toCopy;
            }
            if (count > 0)
            {
                if (tmpBuffer.Length < tmpCount + count)
                {
                    byte[] newBuffer =
                        new byte[tmpBuffer.Length * 2 > tmpCount + count ? tmpBuffer.Length * 2 : tmpCount + count];
                    Buffer.BlockCopy(tmpBuffer, 0, newBuffer, 0, tmpCount);
                    tmpBuffer = newBuffer;
                }
                Buffer.BlockCopy(buffer, offset, tmpBuffer, tmpCount, count);
                tmpCount += count;
                tmpOffset = 0;
                unpack.Suspended = true;
            }
            else
            {
                unpack.Suspended = false;
            }
        }
    }
}