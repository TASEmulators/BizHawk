using System;
using System.IO;
using System.Text;
using SharpCompress.Compressors.LZMA;

namespace SharpCompress.Common.SevenZip
{
    internal class DataReader
    {
        #region Static Methods

        public static uint Get32(byte[] buffer, int offset)
        {
            return buffer[offset]
                   + ((uint)buffer[offset + 1] << 8)
                   + ((uint)buffer[offset + 2] << 16)
                   + ((uint)buffer[offset + 3] << 24);
        }

        public static ulong Get64(byte[] buffer, int offset)
        {
            return buffer[offset]
                   + ((ulong)buffer[offset + 1] << 8)
                   + ((ulong)buffer[offset + 2] << 16)
                   + ((ulong)buffer[offset + 3] << 24)
                   + ((ulong)buffer[offset + 4] << 32)
                   + ((ulong)buffer[offset + 5] << 40)
                   + ((ulong)buffer[offset + 6] << 48)
                   + ((ulong)buffer[offset + 7] << 56);
        }

        #endregion

        #region Variables

        private readonly byte[] _buffer;
        private readonly int _ending;

        #endregion

        #region Public Methods

        public DataReader(byte[] buffer, int offset, int length)
        {
            _buffer = buffer;
            Offset = offset;
            _ending = offset + length;
        }

        public int Offset { get; private set; }

        public Byte ReadByte()
        {
            if (Offset >= _ending)
            {
                throw new EndOfStreamException();
            }

            return _buffer[Offset++];
        }

        public void ReadBytes(byte[] buffer, int offset, int length)
        {
            if (length > _ending - Offset)
            {
                throw new EndOfStreamException();
            }

            while (length-- > 0)
            {
                buffer[offset++] = _buffer[Offset++];
            }
        }

        public void SkipData(long size)
        {
            if (size > _ending - Offset)
            {
                throw new EndOfStreamException();
            }

            Offset += (int)size;
#if DEBUG
            Log.WriteLine("SkipData {0}", size);
#endif
        }

        public void SkipData()
        {
            SkipData(checked((long)ReadNumber()));
        }

        public ulong ReadNumber()
        {
            if (Offset >= _ending)
            {
                throw new EndOfStreamException();
            }

            byte firstByte = _buffer[Offset++];
            byte mask = 0x80;
            ulong value = 0;

            for (int i = 0; i < 8; i++)
            {
                if ((firstByte & mask) == 0)
                {
                    ulong highPart = firstByte & (mask - 1u);
                    value += highPart << (i * 8);
                    return value;
                }

                if (Offset >= _ending)
                {
                    throw new EndOfStreamException();
                }

                value |= (ulong)_buffer[Offset++] << (8 * i);
                mask >>= 1;
            }

            return value;
        }

        public int ReadNum()
        {
            ulong value = ReadNumber();
            if (value > Int32.MaxValue)
            {
                throw new NotSupportedException();
            }

            return (int)value;
        }

        public uint ReadUInt32()
        {
            if (Offset + 4 > _ending)
            {
                throw new EndOfStreamException();
            }

            uint res = Get32(_buffer, Offset);
            Offset += 4;
            return res;
        }

        public ulong ReadUInt64()
        {
            if (Offset + 8 > _ending)
            {
                throw new EndOfStreamException();
            }

            ulong res = Get64(_buffer, Offset);
            Offset += 8;
            return res;
        }

        public string ReadString()
        {
            int ending = Offset;

            for (;;)
            {
                if (ending + 2 > _ending)
                {
                    throw new EndOfStreamException();
                }

                if (_buffer[ending] == 0 && _buffer[ending + 1] == 0)
                {
                    break;
                }

                ending += 2;
            }

            string str = Encoding.Unicode.GetString(_buffer, Offset, ending - Offset);
            Offset = ending + 2;
            return str;
        }

        #endregion
    }
}