using System;
using System.IO;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Common.Zip;
using SharpCompress.Common.Zip.Headers;
using SharpCompress.Converters;

namespace SharpCompress.Writers.Zip
{
    internal class ZipCentralDirectoryEntry
    {
        private readonly ZipCompressionMethod compression;
        private readonly string fileName;
        private readonly ArchiveEncoding archiveEncoding;

        public ZipCentralDirectoryEntry(ZipCompressionMethod compression, string fileName, ulong headerOffset, ArchiveEncoding archiveEncoding)
        {
            this.compression = compression;
            this.fileName = fileName;
            HeaderOffset = headerOffset;
            this.archiveEncoding = archiveEncoding;
        }

        internal DateTime? ModificationTime { get; set; }
        internal string Comment { get; set; }
        internal uint Crc { get; set; }
        internal ulong Compressed { get; set; }
        internal ulong Decompressed { get; set; }
        internal ushort Zip64HeaderOffset { get; set; }
        internal ulong HeaderOffset { get; }

        internal uint Write(Stream outputStream)
        {
            byte[] encodedFilename = archiveEncoding.Encode(fileName);
            byte[] encodedComment = archiveEncoding.Encode(Comment);

            var zip64_stream = Compressed >= uint.MaxValue || Decompressed >= uint.MaxValue;
            var zip64 = zip64_stream || HeaderOffset >= uint.MaxValue || Zip64HeaderOffset != 0;

            var compressedvalue = zip64 ? uint.MaxValue : (uint)Compressed;
            var decompressedvalue = zip64 ? uint.MaxValue : (uint)Decompressed;
            var headeroffsetvalue = zip64 ? uint.MaxValue : (uint)HeaderOffset;
            var extralength = zip64 ? (2 + 2 + 8 + 8 + 8 + 4) : 0;
            var version = (byte)(zip64 ? 45 : 20); // Version 20 required for deflate/encryption

            HeaderFlags flags = Equals(archiveEncoding.GetEncoding(), Encoding.UTF8) ? HeaderFlags.Efs : HeaderFlags.None;
            if (!outputStream.CanSeek)
            {
                // Cannot use data descriptors with zip64:
                // https://blogs.oracle.com/xuemingshen/entry/is_zipinput_outputstream_handling_of

                // We check that streams are not written too large in the ZipWritingStream,
                // so this extra guard is not required, but kept to simplify changing the code
                // once the zip64 post-data issue is resolved
                if (!zip64_stream)
                    flags |= HeaderFlags.UsePostDataDescriptor;

                if (compression == ZipCompressionMethod.LZMA)
                {
                    flags |= HeaderFlags.Bit1; // eos marker
                }
            }

            //constant sig, then version made by, then version to extract
            outputStream.Write(new byte[] { 80, 75, 1, 2, version, 0, version, 0 }, 0, 8);

            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)flags), 0, 2);
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)compression), 0, 2); // zipping method
            outputStream.Write(DataConverter.LittleEndian.GetBytes(ModificationTime.DateTimeToDosTime()), 0, 4);

            // zipping date and time
            outputStream.Write(DataConverter.LittleEndian.GetBytes(Crc), 0, 4); // file CRC
            outputStream.Write(DataConverter.LittleEndian.GetBytes(compressedvalue), 0, 4); // compressed file size
            outputStream.Write(DataConverter.LittleEndian.GetBytes(decompressedvalue), 0, 4); // uncompressed file size
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)extralength), 0, 2); // extra length
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)encodedComment.Length), 0, 2);

            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)0), 0, 2); // disk=0
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)0), 0, 2); // file type: binary
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)0), 0, 2); // Internal file attributes
            outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)0x8100), 0, 2);

            // External file attributes (normal/readable)
            outputStream.Write(DataConverter.LittleEndian.GetBytes(headeroffsetvalue), 0, 4); // Offset of header

            outputStream.Write(encodedFilename, 0, encodedFilename.Length);
            if (zip64)
            {
                outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)0x0001), 0, 2);
                outputStream.Write(DataConverter.LittleEndian.GetBytes((ushort)(extralength - 4)), 0, 2);

                outputStream.Write(DataConverter.LittleEndian.GetBytes(Decompressed), 0, 8);
                outputStream.Write(DataConverter.LittleEndian.GetBytes(Compressed), 0, 8);
                outputStream.Write(DataConverter.LittleEndian.GetBytes(HeaderOffset), 0, 8);
                outputStream.Write(DataConverter.LittleEndian.GetBytes(0), 0, 4); // VolumeNumber = 0
            }

            outputStream.Write(encodedComment, 0, encodedComment.Length);

            return (uint)(8 + 2 + 2 + 4 + 4 + 4 + 4 + 2 + 2 + 2
                                    + 2 + 2 + 2 + 2 + 4 + encodedFilename.Length + extralength + encodedComment.Length);
        }
    }
}