using System.IO;
using SharpCompress.Common;
using SharpCompress.Common.Rar;

namespace SharpCompress.Readers.Rar
{
    internal class SingleVolumeRarReader : RarReader
    {
        private readonly Stream stream;

        internal SingleVolumeRarReader(Stream stream, ReaderOptions options)
            : base(options)
        {
            this.stream = stream;
        }

        internal override void ValidateArchive(RarVolume archive)
        {
            if (archive.IsMultiVolume) {
                var msg = "Streamed archive is a Multi-volume archive.  Use different RarReader method to extract.";
                throw new MultiVolumeExtractionException(msg);
            }
        }

        protected override Stream RequestInitialStream()
        {
            return stream;
        }
    }
}