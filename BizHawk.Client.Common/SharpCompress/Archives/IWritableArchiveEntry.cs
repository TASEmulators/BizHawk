using System.IO;

namespace SharpCompress.Archives
{
    internal interface IWritableArchiveEntry
    {
        Stream Stream { get; }
    }
}