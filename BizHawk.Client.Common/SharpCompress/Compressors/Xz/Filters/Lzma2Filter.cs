using System;
using System.IO;
using SharpCompress.Compressors.LZMA;

namespace SharpCompress.Compressors.Xz.Filters
{
    internal class Lzma2Filter : BlockFilter
    {
        public override bool AllowAsLast => true;
        public override bool AllowAsNonLast => false;
        public override bool ChangesDataSize => true;

        private byte _dictionarySize;
        public uint DictionarySize
        {
            get
            {
                if (_dictionarySize > 40)
                    throw new OverflowException("Dictionary size greater than UInt32.Max");
                if (_dictionarySize == 40)
                {
                    return uint.MaxValue;
                }
                int mantissa = 2 | (_dictionarySize & 1);
                int exponent = _dictionarySize / 2 + 11;
                return (uint)mantissa << exponent;
            }
        }

        public override void Init(byte[] properties)
        {
            if (properties.Length != 1)
                throw new InvalidDataException("LZMA properties unexpected length");

            _dictionarySize = (byte)(properties[0] & 0x3F);
            var reserved = properties[0] & 0xC0;
            if (reserved != 0)
                throw new InvalidDataException("Reserved bits used in LZMA properties");
        }

        public override void ValidateFilter()
        {
        }

        public override void SetBaseStream(Stream stream)
        {
            BaseStream = new LzmaStream(new[] { _dictionarySize }, stream);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return BaseStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return BaseStream.ReadByte();
        }
    }
}
