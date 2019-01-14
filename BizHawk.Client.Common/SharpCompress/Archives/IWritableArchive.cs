using System;
using System.IO;
using SharpCompress.Writers;

namespace SharpCompress.Archives
{
    public interface IWritableArchive : IArchive
    {
        void RemoveEntry(IArchiveEntry entry);

        IArchiveEntry AddEntry(string key, Stream source, bool closeStream, long size = 0, DateTime? modified = null);

        void SaveTo(Stream stream, WriterOptions options);
    }
}