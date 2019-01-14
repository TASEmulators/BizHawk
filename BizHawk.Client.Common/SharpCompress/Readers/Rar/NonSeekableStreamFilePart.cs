using System.IO;
using SharpCompress.Common.Rar;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Readers.Rar
{
    internal class NonSeekableStreamFilePart : RarFilePart
    {
        internal NonSeekableStreamFilePart(MarkHeader mh, FileHeader fh)
            : base(mh, fh)
        {
        }

        internal override Stream GetCompressedStream()
        {
            return FileHeader.PackedStream;
        }

        internal override string FilePartName => "Unknown Stream - File Entry: " + FileHeader.FileName;
    }
}