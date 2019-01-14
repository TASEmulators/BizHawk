using System;
using System.IO;

namespace SharpCompress.Compressors.Filters
{
    internal abstract class Filter : Stream
    {
        protected bool _isEncoder;
        protected Stream _baseStream;

        private readonly byte[] _tail;
        private readonly byte[] _window;
        private int _transformed;
        private int _read;
        private bool _endReached;
        private bool _isDisposed;

        protected Filter(bool isEncoder, Stream baseStream, int lookahead)
        {
            _isEncoder = isEncoder;
            _baseStream = baseStream;
            _tail = new byte[lookahead - 1];
            _window = new byte[_tail.Length * 2];
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            base.Dispose(disposing);
            _baseStream.Dispose();
        }

        public override bool CanRead => !_isEncoder;

        public override bool CanSeek => false;

        public override bool CanWrite => _isEncoder;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int size = 0;

            if (_transformed > 0)
            {
                int copySize = _transformed;
                if (copySize > count)
                {
                    copySize = count;
                }
                Buffer.BlockCopy(_tail, 0, buffer, offset, copySize);
                _transformed -= copySize;
                _read -= copySize;
                offset += copySize;
                count -= copySize;
                size += copySize;
                Buffer.BlockCopy(_tail, copySize, _tail, 0, _read);
            }
            if (count == 0)
            {
                return size;
            }

            int inSize = _read;
            if (inSize > count)
            {
                inSize = count;
            }
            Buffer.BlockCopy(_tail, 0, buffer, offset, inSize);
            _read -= inSize;
            Buffer.BlockCopy(_tail, inSize, _tail, 0, _read);
            while (!_endReached && inSize < count)
            {
                int baseRead = _baseStream.Read(buffer, offset + inSize, count - inSize);
                inSize += baseRead;
                if (baseRead == 0)
                {
                    _endReached = true;
                }
            }
            while (!_endReached && _read < _tail.Length)
            {
                int baseRead = _baseStream.Read(_tail, _read, _tail.Length - _read);
                _read += baseRead;
                if (baseRead == 0)
                {
                    _endReached = true;
                }
            }

            if (inSize > _tail.Length)
            {
                _transformed = Transform(buffer, offset, inSize);
                offset += _transformed;
                count -= _transformed;
                size += _transformed;
                inSize -= _transformed;
                _transformed = 0;
            }

            if (count == 0)
            {
                return size;
            }

            Buffer.BlockCopy(buffer, offset, _window, 0, inSize);
            Buffer.BlockCopy(_tail, 0, _window, inSize, _read);
            if (inSize + _read > _tail.Length)
            {
                _transformed = Transform(_window, 0, inSize + _read);
            }
            else
            {
                _transformed = inSize + _read;
            }
            Buffer.BlockCopy(_window, 0, buffer, offset, inSize);
            Buffer.BlockCopy(_window, inSize, _tail, 0, _read);
            size += inSize;
            _transformed -= inSize;

            return size;
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
            Transform(buffer, offset, count);
            _baseStream.Write(buffer, offset, count);
        }

        protected abstract int Transform(byte[] buffer, int offset, int count);
    }
}