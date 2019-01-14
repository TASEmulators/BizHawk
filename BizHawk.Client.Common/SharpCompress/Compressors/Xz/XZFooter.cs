using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.IO;

namespace SharpCompress.Compressors.Xz
{
    public class XZFooter
    {
        private readonly BinaryReader _reader;
        private readonly byte[] _magicBytes = { 0x59, 0x5A };
        public long StreamStartPosition { get; private set; }
        public long BackwardSize { get; private set; }
        public byte[] StreamFlags { get; private set; }

        public XZFooter(BinaryReader reader)
        {
            _reader = reader;
            StreamStartPosition = reader.BaseStream.Position;
        }

        public static XZFooter FromStream(Stream stream)
        {
            var footer = new XZFooter(new BinaryReader(new NonDisposingStream(stream), Encoding.UTF8));
            footer.Process();
            return footer;
        }

        public void Process()
        {
            uint crc = _reader.ReadLittleEndianUInt32();
            byte[] footerBytes = _reader.ReadBytes(6);
            uint myCrc = Crc32.Compute(footerBytes);
            if (crc != myCrc)
                throw new InvalidDataException("Footer corrupt");
            using (var stream = new MemoryStream(footerBytes))
            using (var reader = new BinaryReader(stream))
            {
                BackwardSize = (reader.ReadLittleEndianUInt32() + 1) * 4;
                StreamFlags = reader.ReadBytes(2);
            }
            byte[] magBy = _reader.ReadBytes(2);
            if (!magBy.SequenceEqual(_magicBytes))
            {
                throw new InvalidDataException("Magic footer missing");
            }
        }
    }
}
