using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Common.Tar;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.IO;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Xz;

namespace SharpCompress.Readers.Tar
{
    public class TarReader : AbstractReader<TarEntry, TarVolume>
    {
        private readonly CompressionType compressionType;

        internal TarReader(Stream stream, ReaderOptions options, CompressionType compressionType)
            : base(options, ArchiveType.Tar)
        {
            this.compressionType = compressionType;
            Volume = new TarVolume(stream, options);
        }

        public override TarVolume Volume { get; }

        protected override Stream RequestInitialStream()
        {
            var stream = base.RequestInitialStream();
            switch (compressionType)
            {
                case CompressionType.BZip2:
                {
                    return new BZip2Stream(stream, CompressionMode.Decompress, false);
                }
                case CompressionType.GZip:
                {
                    return new GZipStream(stream, CompressionMode.Decompress);
                }
                case CompressionType.LZip:
                {
                    return new LZipStream(stream, CompressionMode.Decompress);
                }
                case CompressionType.Xz:
                {
                    return new XZStream(stream);
                }
                case CompressionType.None:
                {
                    return stream;
                }
                default:
                {
                    throw new NotSupportedException("Invalid compression type: " + compressionType);
                }
            }
        }

        #region Open

        /// <summary>
        /// Opens a TarReader for Non-seeking usage with a single volume
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TarReader Open(Stream stream, ReaderOptions options = null)
        {
            stream.CheckNotNull("stream");
            options = options ?? new ReaderOptions();
            RewindableStream rewindableStream = new RewindableStream(stream);
            rewindableStream.StartRecording();
            if (GZipArchive.IsGZipFile(rewindableStream))
            {
                rewindableStream.Rewind(false);
                GZipStream testStream = new GZipStream(rewindableStream, CompressionMode.Decompress);
                if (TarArchive.IsTarFile(testStream))
                {
                    rewindableStream.Rewind(true);
                    return new TarReader(rewindableStream, options, CompressionType.GZip);
                }
                throw new InvalidFormatException("Not a tar file.");
            }

            rewindableStream.Rewind(false);
            if (BZip2Stream.IsBZip2(rewindableStream))
            {
                rewindableStream.Rewind(false);
                BZip2Stream testStream = new BZip2Stream(rewindableStream, CompressionMode.Decompress, false);
                if (TarArchive.IsTarFile(testStream))
                {
                    rewindableStream.Rewind(true);
                    return new TarReader(rewindableStream, options, CompressionType.BZip2);
                }
                throw new InvalidFormatException("Not a tar file.");
            }

            rewindableStream.Rewind(false);
            if (LZipStream.IsLZipFile(rewindableStream))
            {
                rewindableStream.Rewind(false);
                LZipStream testStream = new LZipStream(rewindableStream, CompressionMode.Decompress);
                if (TarArchive.IsTarFile(testStream))
                {
                    rewindableStream.Rewind(true);
                    return new TarReader(rewindableStream, options, CompressionType.LZip);
                }
                throw new InvalidFormatException("Not a tar file.");
            }
            rewindableStream.Rewind(true);
            return new TarReader(rewindableStream, options, CompressionType.None);
        }

        #endregion Open

        protected override IEnumerable<TarEntry> GetEntries(Stream stream)
        {
            return TarEntry.GetEntries(StreamingMode.Streaming, stream, compressionType, Options.ArchiveEncoding);
        }
    }
}