using System.IO;

namespace SharpCompress.Compressors.Xz
{
    public abstract class XZReadOnlyStream : ReadOnlyStream
    {
        public XZReadOnlyStream(Stream stream)
        {
            BaseStream = stream;
            if (!BaseStream.CanRead)
                throw new InvalidDataException("Must be able to read from stream");
        }
    }
}
