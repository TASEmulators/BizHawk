using System;
using System.Collections.Generic;
using SharpCompress.Common.Zip.Headers;

namespace SharpCompress.Common.Zip
{
    public class ZipEntry : Entry
    {
        private readonly ZipFilePart _filePart;

        internal ZipEntry(ZipFilePart filePart)
        {
            if (filePart != null)
            {
                this._filePart = filePart;
                LastModifiedTime = Utility.DosDateToDateTime(filePart.Header.LastModifiedDate,
                                                             filePart.Header.LastModifiedTime);
            }
        }

        public override CompressionType CompressionType
        {
            get
            {
                switch (_filePart.Header.CompressionMethod)
                {
                    case ZipCompressionMethod.BZip2:
                    {
                        return CompressionType.BZip2;
                    }
                    case ZipCompressionMethod.Deflate:
                    {
                        return CompressionType.Deflate;
                    }
                    case ZipCompressionMethod.Deflate64:
                    {
                        return CompressionType.Deflate64;
                    }
                    case ZipCompressionMethod.LZMA:
                    {
                        return CompressionType.LZMA;
                    }
                    case ZipCompressionMethod.PPMd:
                    {
                        return CompressionType.PPMd;
                    }
                    case ZipCompressionMethod.None:
                    {
                        return CompressionType.None;
                    }
                    default:
                    {
                        return CompressionType.Unknown;
                    }
                }
            }
        }

        public override long Crc => _filePart.Header.Crc;

        public override string Key => _filePart.Header.Name;

        public override string LinkTarget => null;

        public override long CompressedSize => _filePart.Header.CompressedSize;

        public override long Size => _filePart.Header.UncompressedSize;

        public override DateTime? LastModifiedTime { get; }

        public override DateTime? CreatedTime => null;

        public override DateTime? LastAccessedTime => null;

        public override DateTime? ArchivedTime => null;

        public override bool IsEncrypted => FlagUtility.HasFlag(_filePart.Header.Flags, HeaderFlags.Encrypted);

        public override bool IsDirectory => _filePart.Header.IsDirectory;

        public override bool IsSplitAfter => false;

        internal override IEnumerable<FilePart> Parts => _filePart.AsEnumerable<FilePart>();
    }
}