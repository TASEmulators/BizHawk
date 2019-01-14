using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common.Zip.Headers;
using SharpCompress.IO;
using System.Text;

namespace SharpCompress.Common.Zip
{
    internal class SeekableZipHeaderFactory : ZipHeaderFactory
    {
        private const int MAX_ITERATIONS_FOR_DIRECTORY_HEADER = 4096;
        private bool _zip64;

        internal SeekableZipHeaderFactory(string password, ArchiveEncoding archiveEncoding)
            : base(StreamingMode.Seekable, password, archiveEncoding)
        {
        }

        internal IEnumerable<ZipHeader> ReadSeekableHeader(Stream stream)
        {
            var reader = new BinaryReader(stream);

            SeekBackToHeader(stream, reader, DIRECTORY_END_HEADER_BYTES);
            var entry = new DirectoryEndHeader();
            entry.Read(reader);

            if (entry.IsZip64)
            {
                _zip64 = true;
                SeekBackToHeader(stream, reader, ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR);
                var zip64Locator = new Zip64DirectoryEndLocatorHeader();
                zip64Locator.Read(reader);

                stream.Seek(zip64Locator.RelativeOffsetOfTheEndOfDirectoryRecord, SeekOrigin.Begin);
                uint zip64Signature = reader.ReadUInt32();
                if (zip64Signature != ZIP64_END_OF_CENTRAL_DIRECTORY)
                    throw new ArchiveException("Failed to locate the Zip64 Header");

                var zip64Entry = new Zip64DirectoryEndHeader();
                zip64Entry.Read(reader);
                stream.Seek(zip64Entry.DirectoryStartOffsetRelativeToDisk, SeekOrigin.Begin);
            }
            else
            {
                stream.Seek(entry.DirectoryStartOffsetRelativeToDisk, SeekOrigin.Begin);
            }

            long position = stream.Position;
            while (true)
            {
                stream.Position = position;
                uint signature = reader.ReadUInt32();
                var nextHeader = ReadHeader(signature, reader, _zip64);
                position = stream.Position;

                if (nextHeader == null)
                    yield break;

                if (nextHeader is DirectoryEntryHeader entryHeader)
                {
                    //entry could be zero bytes so we need to know that.
                    entryHeader.HasData = entryHeader.CompressedSize != 0;
                    yield return entryHeader;
                }
                else if (nextHeader is DirectoryEndHeader endHeader)
                {
                    yield return endHeader;
                }
            }
        }

        private static void SeekBackToHeader(Stream stream, BinaryReader reader, uint headerSignature)
        {
            long offset = 0;
            uint signature;
            int iterationCount = 0;
            do
            {
                if ((stream.Length + offset) - 4 < 0)
                {
                    throw new ArchiveException("Failed to locate the Zip Header");
                }
                stream.Seek(offset - 4, SeekOrigin.End);
                signature = reader.ReadUInt32();
                offset--;
                iterationCount++;
                if (iterationCount > MAX_ITERATIONS_FOR_DIRECTORY_HEADER)
                {
                    throw new ArchiveException("Could not find Zip file Directory at the end of the file.  File may be corrupted.");
                }
            }
            while (signature != headerSignature);
        }

        internal LocalEntryHeader GetLocalHeader(Stream stream, DirectoryEntryHeader directoryEntryHeader)
        {
            stream.Seek(directoryEntryHeader.RelativeOffsetOfEntryHeader, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(stream);
            uint signature = reader.ReadUInt32();
            var localEntryHeader = ReadHeader(signature, reader, _zip64) as LocalEntryHeader;
            if (localEntryHeader == null)
            {
                throw new InvalidOperationException();
            }
            return localEntryHeader;
        }
    }
}