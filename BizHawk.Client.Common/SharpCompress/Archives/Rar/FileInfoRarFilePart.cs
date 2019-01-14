
#if !NO_FILE
using System.IO;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Archives.Rar
{
    internal class FileInfoRarFilePart : SeekableFilePart
    {
        internal FileInfoRarFilePart(FileInfoRarArchiveVolume volume, string password, MarkHeader mh, FileHeader fh, FileInfo fi)
            : base(mh, fh, volume.Stream, password)
        {
            FileInfo = fi;
        }

        internal FileInfo FileInfo { get; }

        internal override string FilePartName
        {
            get
            {
                return "Rar File: " + FileInfo.FullName
                       + " File Entry: " + FileHeader.FileName;
            }
        }
    }
}
#endif