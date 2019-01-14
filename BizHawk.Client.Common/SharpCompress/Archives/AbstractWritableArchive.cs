using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;

namespace SharpCompress.Archives
{
    public abstract class AbstractWritableArchive<TEntry, TVolume> : AbstractArchive<TEntry, TVolume>, IWritableArchive
        where TEntry : IArchiveEntry
        where TVolume : IVolume
    {
        private readonly List<TEntry> newEntries = new List<TEntry>();
        private readonly List<TEntry> removedEntries = new List<TEntry>();

        private readonly List<TEntry> modifiedEntries = new List<TEntry>();
        private bool hasModifications;

        internal AbstractWritableArchive(ArchiveType type)
            : base(type)
        {
        }

        internal AbstractWritableArchive(ArchiveType type, Stream stream, ReaderOptions readerFactoryOptions)
            : base(type, stream.AsEnumerable(), readerFactoryOptions)
        {
        }

#if !NO_FILE
        internal AbstractWritableArchive(ArchiveType type, FileInfo fileInfo, ReaderOptions readerFactoryOptions)
            : base(type, fileInfo, readerFactoryOptions)
        {
        }
#endif

        public override ICollection<TEntry> Entries
        {
            get
            {
                if (hasModifications)
                {
                    return modifiedEntries;
                }
                return base.Entries;
            }
        }

        private void RebuildModifiedCollection()
        {
            hasModifications = true;
            newEntries.RemoveAll(v => removedEntries.Contains(v));
            modifiedEntries.Clear();
            modifiedEntries.AddRange(OldEntries.Concat(newEntries));
        }

        private IEnumerable<TEntry> OldEntries { get { return base.Entries.Where(x => !removedEntries.Contains(x)); } }

        public void RemoveEntry(TEntry entry)
        {
            if (!removedEntries.Contains(entry))
            {
                removedEntries.Add(entry);
                RebuildModifiedCollection();
            }
        }

        void IWritableArchive.RemoveEntry(IArchiveEntry entry)
        {
            RemoveEntry((TEntry)entry);
        }

        public TEntry AddEntry(string key, Stream source,
                               long size = 0, DateTime? modified = null)
        {
            return AddEntry(key, source, false, size, modified);
        }

        IArchiveEntry IWritableArchive.AddEntry(string key, Stream source, bool closeStream, long size, DateTime? modified)
        {
            return AddEntry(key, source, closeStream, size, modified);
        }

        public TEntry AddEntry(string key, Stream source, bool closeStream,
                               long size = 0, DateTime? modified = null)
        {
            if (key.StartsWith("/")
                || key.StartsWith("\\"))
            {
                key = key.Substring(1);
            }
            if (DoesKeyMatchExisting(key))
            {
                throw new ArchiveException("Cannot add entry with duplicate key: " + key);
            }
            var entry = CreateEntry(key, source, size, modified, closeStream);
            newEntries.Add(entry);
            RebuildModifiedCollection();
            return entry;
        }

        private bool DoesKeyMatchExisting(string key)
        {
            foreach (var path in Entries.Select(x => x.Key))
            {
                var p = path.Replace('/', '\\');
                if (p.StartsWith("\\"))
                {
                    p = p.Substring(1);
                }
                return string.Equals(p, key, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public void SaveTo(Stream stream, WriterOptions options)
        {
            //reset streams of new entries
            newEntries.Cast<IWritableArchiveEntry>().ForEach(x => x.Stream.Seek(0, SeekOrigin.Begin));
            SaveTo(stream, options, OldEntries, newEntries);
        }

        protected TEntry CreateEntry(string key, Stream source, long size, DateTime? modified,
                                     bool closeStream)
        {
            if (!source.CanRead || !source.CanSeek)
            {
                throw new ArgumentException("Streams must be readable and seekable to use the Writing Archive API");
            }
            return CreateEntryInternal(key, source, size, modified, closeStream);
        }

        protected abstract TEntry CreateEntryInternal(string key, Stream source, long size, DateTime? modified,
                                                      bool closeStream);

        protected abstract void SaveTo(Stream stream, WriterOptions options, IEnumerable<TEntry> oldEntries, IEnumerable<TEntry> newEntries);

        public override void Dispose()
        {
            base.Dispose();
            newEntries.Cast<Entry>().ForEach(x => x.Close());
            removedEntries.Cast<Entry>().ForEach(x => x.Close());
            modifiedEntries.Cast<Entry>().ForEach(x => x.Close());
        }
    }
}