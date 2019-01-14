using System;

namespace SharpCompress.Common
{
    public interface IEntry
    {
        CompressionType CompressionType { get; }
        DateTime? ArchivedTime { get; }
        long CompressedSize { get; }
        long Crc { get; }
        DateTime? CreatedTime { get; }
        string Key { get; }
        string LinkTarget { get; }
        bool IsDirectory { get; }
        bool IsEncrypted { get; }
        bool IsSplitAfter { get; }
        DateTime? LastAccessedTime { get; }
        DateTime? LastModifiedTime { get; }
        long Size { get; }
        int? Attrib { get; }
    }
}