using System;
using System.IO;

namespace SharpCompress.Compressors.LZMA.RangeCoder
{
    internal class Encoder
    {
        public const uint K_TOP_VALUE = (1 << 24);

        private Stream _stream;

        public UInt64 _low;
        public uint _range;
        private uint _cacheSize;
        private byte _cache;

        //long StartPosition;

        public void SetStream(Stream stream)
        {
            _stream = stream;
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public void Init()
        {
            //StartPosition = Stream.Position;

            _low = 0;
            _range = 0xFFFFFFFF;
            _cacheSize = 1;
            _cache = 0;
        }

        public void FlushData()
        {
            for (int i = 0; i < 5; i++)
            {
                ShiftLow();
            }
        }

        public void FlushStream()
        {
            _stream.Flush();
        }

        public void CloseStream()
        {
            _stream.Dispose();
        }

        public void Encode(uint start, uint size, uint total)
        {
            _low += start * (_range /= total);
            _range *= size;
            while (_range < K_TOP_VALUE)
            {
                _range <<= 8;
                ShiftLow();
            }
        }

        public void ShiftLow()
        {
            if ((uint)_low < 0xFF000000 || (uint)(_low >> 32) == 1)
            {
                byte temp = _cache;
                do
                {
                    _stream.WriteByte((byte)(temp + (_low >> 32)));
                    temp = 0xFF;
                }
                while (--_cacheSize != 0);
                _cache = (byte)(((uint)_low) >> 24);
            }
            _cacheSize++;
            _low = ((uint)_low) << 8;
        }

        public void EncodeDirectBits(uint v, int numTotalBits)
        {
            for (int i = numTotalBits - 1; i >= 0; i--)
            {
                _range >>= 1;
                if (((v >> i) & 1) == 1)
                {
                    _low += _range;
                }
                if (_range < K_TOP_VALUE)
                {
                    _range <<= 8;
                    ShiftLow();
                }
            }
        }

        public void EncodeBit(uint size0, int numTotalBits, uint symbol)
        {
            uint newBound = (_range >> numTotalBits) * size0;
            if (symbol == 0)
            {
                _range = newBound;
            }
            else
            {
                _low += newBound;
                _range -= newBound;
            }
            while (_range < K_TOP_VALUE)
            {
                _range <<= 8;
                ShiftLow();
            }
        }

        public long GetProcessedSizeAdd()
        {
            return -1;

            //return _cacheSize + Stream.Position - StartPosition + 4;
            // (long)Stream.GetProcessedSize();
        }
    }

    internal class Decoder
    {
        public const uint K_TOP_VALUE = (1 << 24);
        public uint _range;
        public uint _code;

        // public Buffer.InBuffer Stream = new Buffer.InBuffer(1 << 16);
        public Stream _stream;
        public long _total;

        public void Init(Stream stream)
        {
            // Stream.Init(stream);
            _stream = stream;

            _code = 0;
            _range = 0xFFFFFFFF;
            for (int i = 0; i < 5; i++)
            {
                _code = (_code << 8) | (byte)_stream.ReadByte();
            }
            _total = 5;
        }

        public void ReleaseStream()
        {
            // Stream.ReleaseStream();
            _stream = null;
        }

        public void CloseStream()
        {
            _stream.Dispose();
        }

        public void Normalize()
        {
            while (_range < K_TOP_VALUE)
            {
                _code = (_code << 8) | (byte)_stream.ReadByte();
                _range <<= 8;
                _total++;
            }
        }

        public void Normalize2()
        {
            if (_range < K_TOP_VALUE)
            {
                _code = (_code << 8) | (byte)_stream.ReadByte();
                _range <<= 8;
                _total++;
            }
        }

        public uint GetThreshold(uint total)
        {
            return _code / (_range /= total);
        }

        public void Decode(uint start, uint size)
        {
            _code -= start * _range;
            _range *= size;
            Normalize();
        }

        public uint DecodeDirectBits(int numTotalBits)
        {
            uint range = _range;
            uint code = _code;
            uint result = 0;
            for (int i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                /*
                result <<= 1;
                if (code >= range)
                {
                    code -= range;
                    result |= 1;
                }
                */
                uint t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < K_TOP_VALUE)
                {
                    code = (code << 8) | (byte)_stream.ReadByte();
                    range <<= 8;
                    _total++;
                }
            }
            _range = range;
            _code = code;
            return result;
        }

        public uint DecodeBit(uint size0, int numTotalBits)
        {
            uint newBound = (_range >> numTotalBits) * size0;
            uint symbol;
            if (_code < newBound)
            {
                symbol = 0;
                _range = newBound;
            }
            else
            {
                symbol = 1;
                _code -= newBound;
                _range -= newBound;
            }
            Normalize();
            return symbol;
        }

        public bool IsFinished => _code == 0;

        // ulong GetProcessedSize() {return Stream.GetProcessedSize(); }
    }
}