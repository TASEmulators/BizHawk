using System.IO;

namespace SharpCompress.Common
{
    public abstract class FilePart
    {
        protected FilePart(ArchiveEncoding archiveEncoding)
        {
            ArchiveEncoding = archiveEncoding;
        }

        internal ArchiveEncoding ArchiveEncoding { get; }
        
        internal abstract string FilePartName { get; }

        internal abstract Stream GetCompressedStream();
        internal abstract Stream GetRawStream();
        internal bool Skipped { get; set; }
    }
}