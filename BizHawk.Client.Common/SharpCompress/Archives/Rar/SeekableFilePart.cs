using System.IO;
using SharpCompress.Common.Rar;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Archives.Rar
{
    internal class SeekableFilePart : RarFilePart
    {
        private readonly Stream stream;
        private readonly string password;

        internal SeekableFilePart(MarkHeader mh, FileHeader fh, Stream stream, string password)
            : base(mh, fh)
        {
            this.stream = stream;
            this.password = password;
        }

        internal override Stream GetCompressedStream()
        {
            stream.Position = FileHeader.DataStartPosition;
#if !NO_CRYPTO
            if (FileHeader.R4Salt != null)
            {
                return new RarCryptoWrapper(stream, password, FileHeader.R4Salt);
            }
#endif
            return stream;
        }

        internal override string FilePartName => "Unknown Stream - File Entry: " + FileHeader.FileName;
    }
}