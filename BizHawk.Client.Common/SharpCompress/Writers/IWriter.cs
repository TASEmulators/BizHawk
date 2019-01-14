using System;
using System.IO;
using SharpCompress.Common;

namespace SharpCompress.Writers
{
    public interface IWriter : IDisposable
    {
        ArchiveType WriterType { get; }
        void Write(string filename, Stream source, DateTime? modificationTime);
    }
}