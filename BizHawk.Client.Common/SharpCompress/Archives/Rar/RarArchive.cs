using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Common;
using SharpCompress.Common.Rar;
using SharpCompress.Common.Rar.Headers;
using SharpCompress.Compressors.Rar;
using SharpCompress.Readers;
using SharpCompress.Readers.Rar;

namespace SharpCompress.Archives.Rar
{
    public class RarArchive : AbstractArchive<RarArchiveEntry, RarVolume>
    {
        internal Lazy<IRarUnpack> UnpackV2017 { get; } = new Lazy<IRarUnpack>(() => new SharpCompress.Compressors.Rar.UnpackV2017.Unpack());
        internal Lazy<IRarUnpack> UnpackV1 { get; } = new Lazy<IRarUnpack>(() => new SharpCompress.Compressors.Rar.UnpackV1.Unpack());

#if !NO_FILE

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="options"></param>
        internal RarArchive(FileInfo fileInfo, ReaderOptions options)
            : base(ArchiveType.Rar, fileInfo, options)
        {
        }

        protected override IEnumerable<RarVolume> LoadVolumes(FileInfo file)
        {
            return RarArchiveVolumeFactory.GetParts(file, ReaderOptions);
        }
#endif

        /// <summary>
        /// Takes multiple seekable Streams for a multi-part archive
        /// </summary>
        /// <param name="streams"></param>
        /// <param name="options"></param>
        internal RarArchive(IEnumerable<Stream> streams, ReaderOptions options)
            : base(ArchiveType.Rar, streams, options)
        {
        }

        protected override IEnumerable<RarArchiveEntry> LoadEntries(IEnumerable<RarVolume> volumes)
        {
            return RarArchiveEntryFactory.GetEntries(this, volumes);
        }

        protected override IEnumerable<RarVolume> LoadVolumes(IEnumerable<Stream> streams)
        {
            return RarArchiveVolumeFactory.GetParts(streams, ReaderOptions);
        }

        protected override IReader CreateReaderForSolidExtraction()
        {
            var stream = Volumes.First().Stream;
            stream.Position = 0;
            return RarReader.Open(stream, ReaderOptions);
        }

        public override bool IsSolid => Volumes.First().IsSolidArchive;

        #region Creation

#if !NO_FILE

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="options"></param>
        public static RarArchive Open(string filePath, ReaderOptions options = null)
        {
            filePath.CheckNotNullOrEmpty("filePath");
            return new RarArchive(new FileInfo(filePath), options ?? new ReaderOptions());
        }

        /// <summary>
        /// Constructor with a FileInfo object to an existing file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="options"></param>
        public static RarArchive Open(FileInfo fileInfo, ReaderOptions options = null)
        {
            fileInfo.CheckNotNull("fileInfo");
            return new RarArchive(fileInfo, options ?? new ReaderOptions());
        }
#endif

        /// <summary>
        /// Takes a seekable Stream as a source
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        public static RarArchive Open(Stream stream, ReaderOptions options = null)
        {
            stream.CheckNotNull("stream");
            return Open(stream.AsEnumerable(), options ?? new ReaderOptions());
        }

        /// <summary>
        /// Takes multiple seekable Streams for a multi-part archive
        /// </summary>
        /// <param name="streams"></param>
        /// <param name="options"></param>
        public static RarArchive Open(IEnumerable<Stream> streams, ReaderOptions options = null)
        {
            streams.CheckNotNull("streams");
            return new RarArchive(streams, options ?? new ReaderOptions());
        }

#if !NO_FILE
        public static bool IsRarFile(string filePath)
        {
            return IsRarFile(new FileInfo(filePath));
        }

        public static bool IsRarFile(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                return false;
            }
            using (Stream stream = fileInfo.OpenRead())
            {
                return IsRarFile(stream);
            }
        }
#endif
        
        public static bool IsRarFile(Stream stream, ReaderOptions options = null)
        {
            try
            {
                MarkHeader.Read(stream, true, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}