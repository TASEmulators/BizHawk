using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Common;
using SharpCompress.Common.SevenZip;
using SharpCompress.Compressors.LZMA.Utilites;
using SharpCompress.IO;
using SharpCompress.Readers;

namespace SharpCompress.Archives.SevenZip
{
    public class SevenZipArchive : AbstractArchive<SevenZipArchiveEntry, SevenZipVolume>
    {
        private ArchiveDatabase database;
#if !NO_FILE

        /// <summary>
        /// Constructor expects a filepath to an existing file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="readerOptions"></param>
        public static SevenZipArchive Open(string filePath, ReaderOptions readerOptions = null)
        {
            filePath.CheckNotNullOrEmpty("filePath");
            return Open(new FileInfo(filePath), readerOptions ?? new ReaderOptions());
        }

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="readerOptions"></param>
        public static SevenZipArchive Open(FileInfo fileInfo, ReaderOptions readerOptions = null)
        {
            fileInfo.CheckNotNull("fileInfo");
            return new SevenZipArchive(fileInfo, readerOptions ?? new ReaderOptions());
        }
#endif
        /// <summary>
        /// Takes a seekable Stream as a source
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readerOptions"></param>
        public static SevenZipArchive Open(Stream stream, ReaderOptions readerOptions = null)
        {
            stream.CheckNotNull("stream");
            return new SevenZipArchive(stream, readerOptions ?? new ReaderOptions());
        }

#if !NO_FILE
        internal SevenZipArchive(FileInfo fileInfo, ReaderOptions readerOptions)
            : base(ArchiveType.SevenZip, fileInfo, readerOptions)
        {
        }

        protected override IEnumerable<SevenZipVolume> LoadVolumes(FileInfo file)
        {
            return new SevenZipVolume(file.OpenRead(), ReaderOptions).AsEnumerable();
        }

        public static bool IsSevenZipFile(string filePath)
        {
            return IsSevenZipFile(new FileInfo(filePath));
        }

        public static bool IsSevenZipFile(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                return false;
            }
            using (Stream stream = fileInfo.OpenRead())
            {
                return IsSevenZipFile(stream);
            }
        }
#endif

        internal SevenZipArchive(Stream stream, ReaderOptions readerOptions)
            : base(ArchiveType.SevenZip, stream.AsEnumerable(), readerOptions)
        {
        }

        internal SevenZipArchive()
            : base(ArchiveType.SevenZip)
        {
        }

        protected override IEnumerable<SevenZipVolume> LoadVolumes(IEnumerable<Stream> streams)
        {
            foreach (Stream s in streams)
            {
                if (!s.CanRead || !s.CanSeek)
                {
                    throw new ArgumentException("Stream is not readable and seekable");
                }
                SevenZipVolume volume = new SevenZipVolume(s, ReaderOptions);
                yield return volume;
            }
        }

        protected override IEnumerable<SevenZipArchiveEntry> LoadEntries(IEnumerable<SevenZipVolume> volumes)
        {
            var stream = volumes.Single().Stream;
            LoadFactory(stream);
            for (int i = 0; i < database._files.Count; i++)
            {
                var file = database._files[i];
                yield return new SevenZipArchiveEntry(this, new SevenZipFilePart(stream, database, i, file, ReaderOptions.ArchiveEncoding));
            }
        }

        private void LoadFactory(Stream stream)
        {
            if (database == null)
            {
                stream.Position = 0;
                var reader = new ArchiveReader();
                reader.Open(stream);
                database = reader.ReadDatabase(new PasswordProvider(ReaderOptions.Password));
            }
        }

        public static bool IsSevenZipFile(Stream stream)
        {
            try
            {
                return SignatureMatch(stream);
            }
            catch
            {
                return false;
            }
        }

        private static readonly byte[] SIGNATURE = {(byte)'7', (byte)'z', 0xBC, 0xAF, 0x27, 0x1C};

        private static bool SignatureMatch(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            byte[] signatureBytes = reader.ReadBytes(6);
            return signatureBytes.BinaryEquals(SIGNATURE);
        }

        protected override IReader CreateReaderForSolidExtraction()
        {
            return new SevenZipReader(ReaderOptions, this);
        }

        public override bool IsSolid { get { return Entries.Where(x => !x.IsDirectory).GroupBy(x => x.FilePart.Folder).Count() > 1; } }

        public override long TotalSize
        {
            get
            {
                int i = Entries.Count;
                return database._packSizes.Aggregate(0L, (total, packSize) => total + packSize);
            }
        }

        private class SevenZipReader : AbstractReader<SevenZipEntry, SevenZipVolume>
        {
            private readonly SevenZipArchive archive;
            private CFolder currentFolder;
            private Stream currentStream;
            private CFileItem currentItem;

            internal SevenZipReader(ReaderOptions readerOptions, SevenZipArchive archive)
                : base(readerOptions, ArchiveType.SevenZip)
            {
                this.archive = archive;
            }

            public override SevenZipVolume Volume => archive.Volumes.Single();

            protected override IEnumerable<SevenZipEntry> GetEntries(Stream stream)
            {
                List<SevenZipArchiveEntry> entries = archive.Entries.ToList();
                stream.Position = 0;
                foreach (var dir in entries.Where(x => x.IsDirectory))
                {
                    yield return dir;
                }
                foreach (var group in entries.Where(x => !x.IsDirectory).GroupBy(x => x.FilePart.Folder))
                {
                    currentFolder = group.Key;
                    if (group.Key == null)
                    {
                        currentStream = Stream.Null;
                    }
                    else
                    {
                        currentStream = archive.database.GetFolderStream(stream, currentFolder, new PasswordProvider(Options.Password));
                    }
                    foreach (var entry in group)
                    {
                        currentItem = entry.FilePart.Header;
                        yield return entry;
                    }
                }
            }

            protected override EntryStream GetEntryStream()
            {
                return CreateEntryStream(new ReadOnlySubStream(currentStream, currentItem.Size));
            }
        }
        
        private class PasswordProvider : IPasswordProvider
        {
            private readonly string _password;

            public PasswordProvider(string password)
            {
                _password = password;

            }

            public string CryptoGetTextPassword()
            {
                return _password;
            }
        }
    }
}
