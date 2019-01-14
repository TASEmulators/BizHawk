using System;
using System.Collections.Generic;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace SharpCompress.Archives
{
    public interface IArchive : IDisposable
    {
        event EventHandler<ArchiveExtractionEventArgs<IArchiveEntry>> EntryExtractionBegin;
        event EventHandler<ArchiveExtractionEventArgs<IArchiveEntry>> EntryExtractionEnd;

        event EventHandler<CompressedBytesReadEventArgs> CompressedBytesRead;
        event EventHandler<FilePartExtractionBeginEventArgs> FilePartExtractionBegin;

        IEnumerable<IArchiveEntry> Entries { get; }
        IEnumerable<IVolume> Volumes { get; }

        ArchiveType Type { get; }

        /// <summary>
        /// Use this method to extract all entries in an archive in order.
        /// This is primarily for SOLID Rar Archives or 7Zip Archives as they need to be 
        /// extracted sequentially for the best performance.
        /// </summary>
        IReader ExtractAllEntries();

        /// <summary>
        /// Archive is SOLID (this means the Archive saved bytes by reusing information which helps for archives containing many small files).
        /// Rar Archives can be SOLID while all 7Zip archives are considered SOLID.
        /// </summary>
        bool IsSolid { get; }

        /// <summary>
        /// This checks to see if all the known entries have IsComplete = true
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// The total size of the files compressed in the archive.
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// The total size of the files as uncompressed in the archive.
        /// </summary>
        long TotalUncompressSize { get; }
    }
}