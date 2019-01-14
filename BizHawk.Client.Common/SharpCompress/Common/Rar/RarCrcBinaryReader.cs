using System.IO;
using SharpCompress.Compressors.Rar;
using SharpCompress.IO;

namespace SharpCompress.Common.Rar
{
    internal class RarCrcBinaryReader : MarkingBinaryReader
    {
        private uint _currentCrc;

        public RarCrcBinaryReader(Stream stream)
            : base(stream)
        {
        }

        public uint GetCrc32()
        {
            return ~_currentCrc;
        }

        public void ResetCrc()
        {
            _currentCrc = 0xffffffff;
        }

        protected void UpdateCrc(byte b)
        {
            _currentCrc = RarCRC.CheckCrc(_currentCrc, b);
        }

        protected byte[] ReadBytesNoCrc(int count)
        {
            return base.ReadBytes(count);
        }

        public override byte ReadByte()
        {
            var b = base.ReadByte();
            _currentCrc = RarCRC.CheckCrc(_currentCrc, b);
            return b;
        }

        public override byte[] ReadBytes(int count)
        {
            var result = base.ReadBytes(count);
            _currentCrc = RarCRC.CheckCrc(_currentCrc, result, 0, result.Length);
            return result;
        }
    }
}