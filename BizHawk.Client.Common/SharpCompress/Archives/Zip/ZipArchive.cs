using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Common;
using SharpCompress.Common.Zip;
using SharpCompress.Common.Zip.Headers;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Readers;
using SharpCompress.Readers.Zip;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;

namespace SharpCompress.Archives.Zip
{
    public class ZipArchive : AbstractWritableArchive<ZipArchiveEntry, ZipVolume>
    {
        private readonly SeekableZipHeaderFactory headerFactory;

        /// <summary>
        /// Gets or sets the compression level applied to files added to the archive,
        /// if the compression method is set to deflate
        /// </summary>
        public CompressionLevel DeflateCompressionLevel { get; set; }

#if !NO_FILE

        /// <summary>
        /// Constructor expects a filepath to an existing file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="readerOptions"></param>
        public static ZipArchive Open(string filePath, ReaderOptions readerOptions = null)
        {
            filePath.CheckNotNullOrEmpty("filePath");
            return Open(new FileInfo(filePath), readerOptions ?? new ReaderOptions());
        }

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="readerOptions"></param>
        public static ZipArchive Open(FileInfo fileInfo, ReaderOptions readerOptions = null)
        {
            fileInfo.CheckNotNull("fileInfo");
            return new ZipArchive(fileInfo, readerOptions ?? new ReaderOptions());
        }
#endif

        /// <summary>
        /// Takes a seekable Stream as a source
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readerOptions"></param>
        public static ZipArchive Open(Stream stream, ReaderOptions readerOptions = null)
        {
            stream.CheckNotNull("stream");
            return new ZipArchive(stream, readerOptions ?? new ReaderOptions());
        }

#if !NO_FILE

        public static bool IsZipFile(string filePath, string password = null)
        {
            return IsZipFile(new FileInfo(filePath), password);
        }

        public static bool IsZipFile(FileInfo fileInfo, string password = null)
        {
            if (!fileInfo.Exists)
            {
                return false;
            }
            using (Stream stream = fileInfo.OpenRead())
            {
                return IsZipFile(stream, password);
            }
        }
#endif

        public static bool IsZipFile(Stream stream, string password = null)
        {
            StreamingZipHeaderFactory headerFactory = new StreamingZipHeaderFactory(password, new ArchiveEncoding());
            try
            {
                ZipHeader header =
                    headerFactory.ReadStreamHeader(stream).FirstOrDefault(x => x.ZipHeaderType != ZipHeaderType.Split);
                if (header == null)
                {
                    return false;
                }
                return Enum.IsDefined(typeof(ZipHeaderType), header.ZipHeaderType);
            }
            catch (CryptographicException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

#if !NO_FILE

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="readerOptions"></param>
        internal ZipArchive(FileInfo fileInfo, ReaderOptions readerOptions)
            : base(ArchiveType.Zip, fileInfo, readerOptions)
        {
            headerFactory = new SeekableZipHeaderFactory(readerOptions.Password, readerOptions.ArchiveEncoding);
        }

        protected override IEnumerable<ZipVolume> LoadVolumes(FileInfo file)
        {
            return new ZipVolume(file.OpenRead(), ReaderOptions).AsEnumerable();
        }
#endif

        internal ZipArchive()
            : base(ArchiveType.Zip)
        {
        }

        /// <summary>
        /// Takes multiple seekable Streams for a multi-part archive
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readerOptions"></param>
        internal ZipArchive(Stream stream, ReaderOptions readerOptions)
            : base(ArchiveType.Zip, stream, readerOptions)
        {
            headerFactory = new SeekableZipHeaderFactory(readerOptions.Password, readerOptions.ArchiveEncoding);
        }

        protected override IEnumerable<ZipVolume> LoadVolumes(IEnumerable<Stream> streams)
        {
            return new ZipVolume(streams.First(), ReaderOptions).AsEnumerable();
        }

        protected override IEnumerable<ZipArchiveEntry> LoadEntries(IEnumerable<ZipVolume> volumes)
        {
            var volume = volumes.Single();
            Stream stream = volume.Stream;
            foreach (ZipHeader h in headerFactory.ReadSeekableHeader(stream))
            {
                if (h != null)
                {
                    switch (h.ZipHeaderType)
                    {
                        case ZipHeaderType.DirectoryEntry:
                            {
                                yield return new ZipArchiveEntry(this,
                                                                 new SeekableZipFilePart(headerFactory,
                                                                                         h as DirectoryEntryHeader,
                                                                                         stream));
                            }
                            break;
                        case ZipHeaderType.DirectoryEnd:
                            {
                                byte[] bytes = (h as DirectoryEndHeader).Comment;
                                volume.Comment = ReaderOptions.ArchiveEncoding.Decode(bytes);
                                yield break;
                            }
                    }
                }
            }
        }

        public void SaveTo(Stream stream)
        {
            SaveTo(stream, new WriterOptions(CompressionType.Deflate));
        }

        protected override void SaveTo(Stream stream, WriterOptions options,
                                       IEnumerable<ZipArchiveEntry> oldEntries,
                                       IEnumerable<ZipArchiveEntry> newEntries)
        {
            using (var writer = new ZipWriter(stream, new ZipWriterOptions(options)))
            {
                foreach (var entry in oldEntries.Concat(newEntries)
                                                .Where(x => !x.IsDirectory))
                {
                    using (var entryStream = entry.OpenEntryStream())
                    {
                        writer.Write(entry.Key, entryStream, entry.LastModifiedTime);
                    }
                }
            }
        }

        protected override ZipArchiveEntry CreateEntryInternal(string filePath, Stream source, long size, DateTime? modified,
                                                               bool closeStream)
        {
            return new ZipWritableArchiveEntry(this, source, filePath, size, modified, closeStream);
        }

        public static ZipArchive Create()
        {
            return new ZipArchive();
        }

        protected override IReader CreateReaderForSolidExtraction()
        {
            var stream = Volumes.Single().Stream;
            stream.Position = 0;
            return ZipReader.Open(stream, ReaderOptions);
        }
    }
}