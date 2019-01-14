using System;
using System.Collections.Generic;

namespace SharpCompress.Common.SevenZip
{
    public class SevenZipEntry : Entry
    {
        internal SevenZipEntry(SevenZipFilePart filePart)
        {
            FilePart = filePart;
        }

        internal SevenZipFilePart FilePart { get; }

        public override CompressionType CompressionType => FilePart.CompressionType;

        public override long Crc => FilePart.Header.Crc ?? 0;

        public override string Key => FilePart.Header.Name;

        public override string LinkTarget => null;

        public override long CompressedSize => 0;

        public override long Size => FilePart.Header.Size;

        public override DateTime? LastModifiedTime => FilePart.Header.MTime;

        public override DateTime? CreatedTime => null;

        public override DateTime? LastAccessedTime => null;

        public override DateTime? ArchivedTime => null;

        public override bool IsEncrypted => false;

        public override bool IsDirectory => FilePart.Header.IsDir;

        public override bool IsSplitAfter => false;

        public override int? Attrib => (int)FilePart.Header.Attrib;

        internal override IEnumerable<FilePart> Parts => FilePart.AsEnumerable<FilePart>();
    }
}