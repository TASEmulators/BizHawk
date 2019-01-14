using System;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.IO;

namespace SharpCompress.Compressors.Xz
{
    public class XZHeader
    {
        private readonly BinaryReader _reader;
        private readonly byte[] MagicHeader = { 0xFD, 0x37, 0x7A, 0x58, 0x5a, 0x00 };

        public CheckType BlockCheckType { get; private set; }
        public int BlockCheckSize => ((((int)BlockCheckType) + 2) / 3) * 4;

        public XZHeader(BinaryReader reader)
        {
            _reader = reader;
        }

        public static XZHeader FromStream(Stream stream)
        {
            var header = new XZHeader(new BinaryReader(new NonDisposingStream(stream), Encoding.UTF8));
            header.Process();
            return header;
        }

        public void Process()
        {
            CheckMagicBytes(_reader.ReadBytes(6));
            ProcessStreamFlags();
        }

        private void ProcessStreamFlags()
        {
            byte[] streamFlags = _reader.ReadBytes(2);
            UInt32 crc = _reader.ReadLittleEndianUInt32();
            UInt32 calcCrc = Crc32.Compute(streamFlags);
            if (crc != calcCrc)
                throw new InvalidDataException("Stream header corrupt");

            BlockCheckType = (CheckType)(streamFlags[1] & 0x0F);
            byte futureUse = (byte)(streamFlags[1] & 0xF0);
            if (futureUse != 0 || streamFlags[0] != 0)
                throw new InvalidDataException("Unknown XZ Stream Version");
        }

        private void CheckMagicBytes(byte[] header)
        {
            if (!header.SequenceEqual(MagicHeader))
                throw new InvalidDataException("Invalid XZ Stream");
        }
    }
}
