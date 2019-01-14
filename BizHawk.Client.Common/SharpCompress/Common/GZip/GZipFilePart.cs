using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common.Tar.Headers;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Converters;
using System.Text;

namespace SharpCompress.Common.GZip
{
    internal class GZipFilePart : FilePart
    {
        private string _name;
        private readonly Stream _stream;

        internal GZipFilePart(Stream stream, ArchiveEncoding archiveEncoding)
        : base(archiveEncoding)
        {
            ReadAndValidateGzipHeader(stream);
            EntryStartPosition = stream.Position;
            _stream = stream;
        }

        internal long EntryStartPosition { get; }

        internal DateTime? DateModified { get; private set; }

        internal override string FilePartName => _name;

        internal override Stream GetCompressedStream()
        {
            return new DeflateStream(_stream, CompressionMode.Decompress, CompressionLevel.Default);
        }

        internal override Stream GetRawStream()
        {
            return _stream;
        }

        private void ReadAndValidateGzipHeader(Stream stream)
        {
            // read the header on the first read
            byte[] header = new byte[10];
            int n = stream.Read(header, 0, header.Length);

            // workitem 8501: handle edge case (decompress empty stream)
            if (n == 0)
            {
                return;
            }

            if (n != 10)
            {
                throw new ZlibException("Not a valid GZIP stream.");
            }

            if (header[0] != 0x1F || header[1] != 0x8B || header[2] != 8)
            {
                throw new ZlibException("Bad GZIP header.");
            }

            Int32 timet = DataConverter.LittleEndian.GetInt32(header, 4);
            DateModified = TarHeader.EPOCH.AddSeconds(timet);
            if ((header[3] & 0x04) == 0x04)
            {
                // read and discard extra field
                n = stream.Read(header, 0, 2); // 2-byte length field

                Int16 extraLength = (Int16)(header[0] + header[1] * 256);
                byte[] extra = new byte[extraLength];
                
                if (!stream.ReadFully(extra))
                {
                    throw new ZlibException("Unexpected end-of-file reading GZIP header.");
                }
                n = extraLength;
            }
            if ((header[3] & 0x08) == 0x08)
            {
                _name = ReadZeroTerminatedString(stream);
            }
            if ((header[3] & 0x10) == 0x010)
            {
                ReadZeroTerminatedString(stream);
            }
            if ((header[3] & 0x02) == 0x02)
            {
                stream.ReadByte(); // CRC16, ignore
            }
        }

        private string ReadZeroTerminatedString(Stream stream)
        {
            byte[] buf1 = new byte[1];
            var list = new List<byte>();
            bool done = false;
            do
            {
                // workitem 7740
                int n = stream.Read(buf1, 0, 1);
                if (n != 1)
                {
                    throw new ZlibException("Unexpected EOF reading GZIP header.");
                }
                if (buf1[0] == 0)
                {
                    done = true;
                }
                else
                {
                    list.Add(buf1[0]);
                }
            }
            while (!done);
            byte[] buffer = list.ToArray();
            return ArchiveEncoding.Decode(buffer);
        }
    }
}