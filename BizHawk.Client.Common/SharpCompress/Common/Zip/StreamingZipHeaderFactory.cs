using System.Collections.Generic;
using System.IO;
using SharpCompress.Common.Zip.Headers;
using SharpCompress.IO;
using System.Text;

namespace SharpCompress.Common.Zip
{
    internal class StreamingZipHeaderFactory : ZipHeaderFactory
    {
        internal StreamingZipHeaderFactory(string password, ArchiveEncoding archiveEncoding)
            : base(StreamingMode.Streaming, password, archiveEncoding)
        {
        }

        internal IEnumerable<ZipHeader> ReadStreamHeader(Stream stream)
        {
            RewindableStream rewindableStream;
            if (stream is RewindableStream)
            {
                rewindableStream = stream as RewindableStream;
            }
            else
            {
                rewindableStream = new RewindableStream(stream);
            }
            while (true)
            {
                ZipHeader header = null;
                BinaryReader reader = new BinaryReader(rewindableStream);
                if (_lastEntryHeader != null &&
                    (FlagUtility.HasFlag(_lastEntryHeader.Flags, HeaderFlags.UsePostDataDescriptor) || _lastEntryHeader.IsZip64))
                {
                    reader = (_lastEntryHeader.Part as StreamingZipFilePart).FixStreamedFileLocation(ref rewindableStream);
                    long? pos = rewindableStream.CanSeek ? (long?)rewindableStream.Position : null;
                    uint crc = reader.ReadUInt32();
                    if (crc == POST_DATA_DESCRIPTOR)
                    {
                        crc = reader.ReadUInt32();
                    }
                    _lastEntryHeader.Crc = crc;
                    _lastEntryHeader.CompressedSize = reader.ReadUInt32();
                    _lastEntryHeader.UncompressedSize = reader.ReadUInt32();
                    if (pos.HasValue)
                    {
                        _lastEntryHeader.DataStartPosition = pos - _lastEntryHeader.CompressedSize;
                    }
                }
                _lastEntryHeader = null;
                uint headerBytes = reader.ReadUInt32();
                header = ReadHeader(headerBytes, reader);
                if (header == null) { yield break; }

                //entry could be zero bytes so we need to know that.
                if (header.ZipHeaderType == ZipHeaderType.LocalEntry)
                {
                    bool isRecording = rewindableStream.IsRecording;
                    if (!isRecording)
                    {
                        rewindableStream.StartRecording();
                    }
                    uint nextHeaderBytes = reader.ReadUInt32();
                    header.HasData = !IsHeader(nextHeaderBytes);
                    rewindableStream.Rewind(!isRecording);
                }
                yield return header;
            }
        }
    }
}