using System.IO;
using System.Linq;
using SharpCompress.Common.GZip;

namespace SharpCompress.Archives.GZip
{
    public class GZipArchiveEntry : GZipEntry, IArchiveEntry
    {
        internal GZipArchiveEntry(GZipArchive archive, GZipFilePart part)
            : base(part)
        {
            Archive = archive;
        }

        public virtual Stream OpenEntryStream()
        {
            //this is to reset the stream to be read multiple times
            var part = Parts.Single() as GZipFilePart;
            if (part.GetRawStream().Position != part.EntryStartPosition)
            {
                part.GetRawStream().Position = part.EntryStartPosition;
            }
            return Parts.Single().GetCompressedStream();
        }

        #region IArchiveEntry Members

        public IArchive Archive { get; }

        public bool IsComplete => true;

        #endregion
    }
}