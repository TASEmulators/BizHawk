using System.IO;

namespace SharpCompress.Common.Zip.Headers
{
    internal abstract class ZipHeader
    {
        protected ZipHeader(ZipHeaderType type)
        {
            ZipHeaderType = type;
            HasData = true;
        }

        internal ZipHeaderType ZipHeaderType { get; }

        internal abstract void Read(BinaryReader reader);

        internal bool HasData { get; set; }
    }
}