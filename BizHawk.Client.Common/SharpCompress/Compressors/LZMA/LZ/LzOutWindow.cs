using System;
using System.IO;

namespace SharpCompress.Compressors.LZMA.LZ
{
    internal class OutWindow
    {
        private byte[] _buffer;
        private int _windowSize;
        private int _pos;
        private int _streamPos;
        private int _pendingLen;
        private int _pendingDist;
        private Stream _stream;

        public long _total;
        public long _limit;

        public void Create(int windowSize)
        {
            if (_windowSize != windowSize)
            {
                _buffer = new byte[windowSize];
            }
            else
            {
                _buffer[windowSize - 1] = 0;
            }
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
            _pendingLen = 0;
            _total = 0;
            _limit = 0;
        }

        public void Reset()
        {
            Create(_windowSize);
        }

        public void Init(Stream stream)
        {
            ReleaseStream();
            _stream = stream;
        }

        public void Train(Stream stream)
        {
            long len = stream.Length;
            int size = (len < _windowSize) ? (int)len : _windowSize;
            stream.Position = len - size;
            _total = 0;
            _limit = size;
            _pos = _windowSize - size;
            CopyStream(stream, size);
            if (_pos == _windowSize)
            {
                _pos = 0;
            }
            _streamPos = _pos;
        }

        public void ReleaseStream()
        {
            Flush();
            _stream = null;
        }

        public void Flush()
        {
            if (_stream == null)
            {
                return;
            }
            int size = _pos - _streamPos;
            if (size == 0)
            {
                return;
            }
            _stream.Write(_buffer, _streamPos, size);
            if (_pos >= _windowSize)
            {
                _pos = 0;
            }
            _streamPos = _pos;
        }

        public void CopyBlock(int distance, int len)
        {
            int size = len;
            int pos = _pos - distance - 1;
            if (pos < 0)
            {
                pos += _windowSize;
            }
            for (; size > 0 && _pos < _windowSize && _total < _limit; size--)
            {
                if (pos >= _windowSize)
                {
                    pos = 0;
                }
                _buffer[_pos++] = _buffer[pos++];
                _total++;
                if (_pos >= _windowSize)
                {
                    Flush();
                }
            }
            _pendingLen = size;
            _pendingDist = distance;
        }

        public void PutByte(byte b)
        {
            _buffer[_pos++] = b;
            _total++;
            if (_pos >= _windowSize)
            {
                Flush();
            }
        }

        public byte GetByte(int distance)
        {
            int pos = _pos - distance - 1;
            if (pos < 0)
            {
                pos += _windowSize;
            }
            return _buffer[pos];
        }

        public int CopyStream(Stream stream, int len)
        {
            int size = len;
            while (size > 0 && _pos < _windowSize && _total < _limit)
            {
                int curSize = _windowSize - _pos;
                if (curSize > _limit - _total)
                {
                    curSize = (int)(_limit - _total);
                }
                if (curSize > size)
                {
                    curSize = size;
                }
                int numReadBytes = stream.Read(_buffer, _pos, curSize);
                if (numReadBytes == 0)
                {
                    throw new DataErrorException();
                }
                size -= numReadBytes;
                _pos += numReadBytes;
                _total += numReadBytes;
                if (_pos >= _windowSize)
                {
                    Flush();
                }
            }
            return len - size;
        }

        public void SetLimit(long size)
        {
            _limit = _total + size;
        }

        public bool HasSpace => _pos < _windowSize && _total < _limit;

        public bool HasPending => _pendingLen > 0;

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_streamPos >= _pos)
            {
                return 0;
            }

            int size = _pos - _streamPos;
            if (size > count)
            {
                size = count;
            }
            Buffer.BlockCopy(_buffer, _streamPos, buffer, offset, size);
            _streamPos += size;
            if (_streamPos >= _windowSize)
            {
                _pos = 0;
                _streamPos = 0;
            }
            return size;
        }

        public void CopyPending()
        {
            if (_pendingLen > 0)
            {
                CopyBlock(_pendingDist, _pendingLen);
            }
        }

        public int AvailableBytes => _pos - _streamPos;
    }
}