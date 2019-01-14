using System;
using System.IO;

namespace SharpCompress.Compressors.Filters
{
    internal class BCJ2Filter : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[] _input = new byte[4096];
        private int _inputOffset;
        private int _inputCount;
        private bool _endReached;

        private long _position;
        private readonly byte[] _output = new byte[4];
        private int _outputOffset;
        private int _outputCount;

        private readonly byte[] _control;
        private readonly byte[] _data1;
        private readonly byte[] _data2;

        private int _controlPos;
        private int _data1Pos;
        private int _data2Pos;

        private readonly ushort[] _p = new ushort[256 + 2];
        private uint _range, _code;
        private byte _prevByte;
        private bool _isDisposed;

        private const int K_NUM_TOP_BITS = 24;
        private const int K_TOP_VALUE = 1 << K_NUM_TOP_BITS;

        private const int K_NUM_BIT_MODEL_TOTAL_BITS = 11;
        private const int K_BIT_MODEL_TOTAL = 1 << K_NUM_BIT_MODEL_TOTAL_BITS;
        private const int K_NUM_MOVE_BITS = 5;

        private static bool IsJ(byte b0, byte b1)
        {
            return (b1 & 0xFE) == 0xE8 || IsJcc(b0, b1);
        }

        private static bool IsJcc(byte b0, byte b1)
        {
            return b0 == 0x0F && (b1 & 0xF0) == 0x80;
        }

        public BCJ2Filter(byte[] control, byte[] data1, byte[] data2, Stream baseStream)
        {
            _control = control;
            _data1 = data1;
            _data2 = data2;
            _baseStream = baseStream;

            int i;
            for (i = 0; i < _p.Length; i++)
            {
                _p[i] = K_BIT_MODEL_TOTAL >> 1;
            }

            _code = 0;
            _range = 0xFFFFFFFF;
            for (i = 0; i < 5; i++)
            {
                _code = (_code << 8) | control[_controlPos++];
            }
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

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length => _baseStream.Length + _data1.Length + _data2.Length;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int size = 0;
            byte b = 0;

            while (!_endReached && size < count)
            {
                while (_outputOffset < _outputCount)
                {
                    b = _output[_outputOffset++];
                    buffer[offset++] = b;
                    size++;
                    _position++;

                    _prevByte = b;
                    if (size == count)
                    {
                        return size;
                    }
                }

                if (_inputOffset == _inputCount)
                {
                    _inputOffset = 0;
                    _inputCount = _baseStream.Read(_input, 0, _input.Length);
                    if (_inputCount == 0)
                    {
                        _endReached = true;
                        break;
                    }
                }

                b = _input[_inputOffset++];
                buffer[offset++] = b;
                size++;
                _position++;

                if (!IsJ(_prevByte, b))
                {
                    _prevByte = b;
                }
                else
                {
                    int prob;
                    if (b == 0xE8)
                    {
                        prob = _prevByte;
                    }
                    else if (b == 0xE9)
                    {
                        prob = 256;
                    }
                    else
                    {
                        prob = 257;
                    }

                    uint bound = (_range >> K_NUM_BIT_MODEL_TOTAL_BITS) * _p[prob];
                    if (_code < bound)
                    {
                        _range = bound;
                        _p[prob] += (ushort)((K_BIT_MODEL_TOTAL - _p[prob]) >> K_NUM_MOVE_BITS);
                        if (_range < K_TOP_VALUE)
                        {
                            _range <<= 8;
                            _code = (_code << 8) | _control[_controlPos++];
                        }
                        _prevByte = b;
                    }
                    else
                    {
                        _range -= bound;
                        _code -= bound;
                        _p[prob] -= (ushort)(_p[prob] >> K_NUM_MOVE_BITS);
                        if (_range < K_TOP_VALUE)
                        {
                            _range <<= 8;
                            _code = (_code << 8) | _control[_controlPos++];
                        }

                        uint dest;
                        if (b == 0xE8)
                        {
                            dest =
                                (uint)
                                ((_data1[_data1Pos++] << 24) | (_data1[_data1Pos++] << 16) | (_data1[_data1Pos++] << 8) |
                                 _data1[_data1Pos++]);
                        }
                        else
                        {
                            dest =
                                (uint)
                                ((_data2[_data2Pos++] << 24) | (_data2[_data2Pos++] << 16) | (_data2[_data2Pos++] << 8) |
                                 _data2[_data2Pos++]);
                        }
                        dest -= (uint)(_position + 4);

                        _output[0] = (byte)dest;
                        _output[1] = (byte)(dest >> 8);
                        _output[2] = (byte)(dest >> 16);
                        _output[3] = (byte)(dest >> 24);
                        _outputOffset = 0;
                        _outputCount = 4;
                    }
                }
            }

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
            throw new NotSupportedException();
        }
    }
}