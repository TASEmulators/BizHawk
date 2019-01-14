using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common.Tar.Headers;
using SharpCompress.IO;
using System.Text;

namespace SharpCompress.Common.Tar
{
    public class TarEntry : Entry
    {
        private readonly TarFilePart _filePart;

        internal TarEntry(TarFilePart filePart, CompressionType type)
        {
            this._filePart = filePart;
            CompressionType = type;
        }

        public override CompressionType CompressionType { get; }

        public override long Crc => 0;

        public override string Key => _filePart.Header.Name;

        public override string LinkTarget => _filePart.Header.LinkName;

        public override long CompressedSize => _filePart.Header.Size;

        public override long Size => _filePart.Header.Size;

        public override DateTime? LastModifiedTime => _filePart.Header.LastModifiedTime;

        public override DateTime? CreatedTime => null;

        public override DateTime? LastAccessedTime => null;

        public override DateTime? ArchivedTime => null;

        public override bool IsEncrypted => false;

        public override bool IsDirectory => _filePart.Header.EntryType == EntryType.Directory;

        public override bool IsSplitAfter => false;

        internal override IEnumerable<FilePart> Parts => _filePart.AsEnumerable<FilePart>();

        internal static IEnumerable<TarEntry> GetEntries(StreamingMode mode, Stream stream,
                                                         CompressionType compressionType, ArchiveEncoding archiveEncoding)
        {
            foreach (TarHeader h in TarHeaderFactory.ReadHeader(mode, stream, archiveEncoding))
            {
                if (h != null)
                {
                    if (mode == StreamingMode.Seekable)
                    {
                        yield return new TarEntry(new TarFilePart(h, stream), compressionType);
                    }
                    else
                    {
                        yield return new TarEntry(new TarFilePart(h, null), compressionType);
                    }
                }
            }
        }
    }
}