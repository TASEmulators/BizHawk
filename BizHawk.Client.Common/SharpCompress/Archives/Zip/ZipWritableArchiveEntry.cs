using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common;
using SharpCompress.IO;

namespace SharpCompress.Archives.Zip
{
    internal class ZipWritableArchiveEntry : ZipArchiveEntry, IWritableArchiveEntry
    {
        private readonly bool closeStream;
        private readonly Stream stream;
        private bool isDisposed;

        internal ZipWritableArchiveEntry(ZipArchive archive, Stream stream, string path, long size,
                                         DateTime? lastModified, bool closeStream)
            : base(archive, null)
        {
            this.stream = stream;
            Key = path;
            Size = size;
            LastModifiedTime = lastModified;
            this.closeStream = closeStream;
        }

        public override long Crc => 0;

        public override string Key { get; }

        public override long CompressedSize => 0;

        public override long Size { get; }

        public override DateTime? LastModifiedTime { get; }

        public override DateTime? CreatedTime => null;

        public override DateTime? LastAccessedTime => null;

        public override DateTime? ArchivedTime => null;

        public override bool IsEncrypted => false;

        public override bool IsDirectory => false;

        public override bool IsSplitAfter => false;

        internal override IEnumerable<FilePart> Parts => throw new NotImplementedException();

        Stream IWritableArchiveEntry.Stream => stream;

        public override Stream OpenEntryStream()
        {
            //ensure new stream is at the start, this could be reset
            stream.Seek(0, SeekOrigin.Begin);
            return new NonDisposingStream(stream);
        }

        internal override void Close()
        {
            if (closeStream && !isDisposed)
            {
                stream.Dispose();
                isDisposed = true;
            }
        }
    }
}