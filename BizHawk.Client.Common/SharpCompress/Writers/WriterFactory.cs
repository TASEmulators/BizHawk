using System;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Writers.GZip;
using SharpCompress.Writers.Tar;
using SharpCompress.Writers.Zip;

namespace SharpCompress.Writers
{
    public static class WriterFactory
    {
        public static IWriter Open(Stream stream, ArchiveType archiveType, WriterOptions writerOptions)
        {
            switch (archiveType)
            {
                case ArchiveType.GZip:
                {
                    if (writerOptions.CompressionType != CompressionType.GZip)
                    {
                        throw new InvalidFormatException("GZip archives only support GZip compression type.");
                    }
                    return new GZipWriter(stream, new GZipWriterOptions(writerOptions));
                }
                case ArchiveType.Zip:
                {
                    return new ZipWriter(stream, new ZipWriterOptions(writerOptions));
                }
                case ArchiveType.Tar:
                {
                    return new TarWriter(stream, new TarWriterOptions(writerOptions));
                }
                default:
                {
                    throw new NotSupportedException("Archive Type does not have a Writer: " + archiveType);
                }
            }
        }
    }
}