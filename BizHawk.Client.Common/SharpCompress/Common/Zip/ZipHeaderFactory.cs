using System;
using System.IO;
#if !NO_CRYPTO
using System.Linq;
#endif
using SharpCompress.Common.Zip.Headers;
using SharpCompress.IO;
using System.Text;

namespace SharpCompress.Common.Zip
{
    internal class ZipHeaderFactory
    {
        internal const uint ENTRY_HEADER_BYTES = 0x04034b50;
        internal const uint POST_DATA_DESCRIPTOR = 0x08074b50;
        internal const uint DIRECTORY_START_HEADER_BYTES = 0x02014b50;
        internal const uint DIRECTORY_END_HEADER_BYTES = 0x06054b50;
        internal const uint DIGITAL_SIGNATURE = 0x05054b50;
        internal const uint SPLIT_ARCHIVE_HEADER_BYTES = 0x30304b50;

        internal const uint ZIP64_END_OF_CENTRAL_DIRECTORY = 0x06064b50;
        internal const uint ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR = 0x07064b50;

        protected LocalEntryHeader _lastEntryHeader;
        private readonly string _password;
        private readonly StreamingMode _mode;
        private readonly ArchiveEncoding _archiveEncoding;

        protected ZipHeaderFactory(StreamingMode mode, string password, ArchiveEncoding archiveEncoding)
        {
            this._mode = mode;
            this._password = password;
            this._archiveEncoding = archiveEncoding;
        }

        protected ZipHeader ReadHeader(uint headerBytes, BinaryReader reader, bool zip64 = false)
        {
            switch (headerBytes)
            {
                case ENTRY_HEADER_BYTES:
                {
                    var entryHeader = new LocalEntryHeader(_archiveEncoding);
                    entryHeader.Read(reader);
                    LoadHeader(entryHeader, reader.BaseStream);

                    _lastEntryHeader = entryHeader;
                    return entryHeader;
                }
                case DIRECTORY_START_HEADER_BYTES:
                {
                    var entry = new DirectoryEntryHeader(_archiveEncoding);
                    entry.Read(reader);
                    return entry;
                }
                case POST_DATA_DESCRIPTOR:
                    {
                        if (FlagUtility.HasFlag(_lastEntryHeader.Flags, HeaderFlags.UsePostDataDescriptor))
                        {
                            _lastEntryHeader.Crc = reader.ReadUInt32();
                            _lastEntryHeader.CompressedSize = zip64 ? (long)reader.ReadUInt64() : reader.ReadUInt32();
                            _lastEntryHeader.UncompressedSize = zip64 ? (long)reader.ReadUInt64() : reader.ReadUInt32();
                        }
                        else
                        {
                            reader.ReadBytes(zip64 ? 20 : 12);
                        }
                        return null;
                    }
                case DIGITAL_SIGNATURE:
                    return null;
                case DIRECTORY_END_HEADER_BYTES:
                    {
                        var entry = new DirectoryEndHeader();
                        entry.Read(reader);
                        return entry;
                    }
                case SPLIT_ARCHIVE_HEADER_BYTES:
                    {
                        return new SplitHeader();
                    }
                case ZIP64_END_OF_CENTRAL_DIRECTORY:
                    {
                        var entry = new Zip64DirectoryEndHeader();
                        entry.Read(reader);
                        return entry;
                    }
                case ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR:
                    {
                        var entry = new Zip64DirectoryEndLocatorHeader();
                        entry.Read(reader);
                        return entry;
                    }
                default:
                    return null;
            }
        }

        internal static bool IsHeader(uint headerBytes)
        {
            switch (headerBytes)
            {
                case ENTRY_HEADER_BYTES:
                case DIRECTORY_START_HEADER_BYTES:
                case POST_DATA_DESCRIPTOR:
                case DIGITAL_SIGNATURE:
                case DIRECTORY_END_HEADER_BYTES:
                case SPLIT_ARCHIVE_HEADER_BYTES:
                case ZIP64_END_OF_CENTRAL_DIRECTORY:
                case ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR:
                    return true;
                default:
                    return false;
            }
        }

        private void LoadHeader(ZipFileEntry entryHeader, Stream stream)
        {
            if (FlagUtility.HasFlag(entryHeader.Flags, HeaderFlags.Encrypted))
            {
                if (!entryHeader.IsDirectory && entryHeader.CompressedSize == 0 &&
                    FlagUtility.HasFlag(entryHeader.Flags, HeaderFlags.UsePostDataDescriptor))
                {
                    throw new NotSupportedException("SharpCompress cannot currently read non-seekable Zip Streams with encrypted data that has been written in a non-seekable manner.");
                }

                if (_password == null)
                {
                    throw new CryptographicException("No password supplied for encrypted zip.");
                }

                entryHeader.Password = _password;

                if (entryHeader.CompressionMethod == ZipCompressionMethod.WinzipAes)
                {
#if NO_CRYPTO
                    throw new NotSupportedException("Cannot decrypt Winzip AES with Silverlight or WP7.");
#else

                    ExtraData data = entryHeader.Extra.SingleOrDefault(x => x.Type == ExtraDataType.WinZipAes);
                    if (data != null)
                    {
                        var keySize = (WinzipAesKeySize)data.DataBytes[4];

                        var salt = new byte[WinzipAesEncryptionData.KeyLengthInBytes(keySize) / 2];
                        var passwordVerifyValue = new byte[2];
                        stream.Read(salt, 0, salt.Length);
                        stream.Read(passwordVerifyValue, 0, 2);
                        entryHeader.WinzipAesEncryptionData =
                            new WinzipAesEncryptionData(keySize, salt, passwordVerifyValue, _password);

                        entryHeader.CompressedSize -= (uint)(salt.Length + 2);
                    }
#endif
                }
            }

            if (entryHeader.IsDirectory)
            {
                return;
            }

            //if (FlagUtility.HasFlag(entryHeader.Flags, HeaderFlags.UsePostDataDescriptor))
            //{
            //    entryHeader.PackedStream = new ReadOnlySubStream(stream);
            //}
            //else
            //{
            switch (_mode)
            {
                case StreamingMode.Seekable:
                    {
                        entryHeader.DataStartPosition = stream.Position;
                        stream.Position += entryHeader.CompressedSize;
                        break;
                    }

                case StreamingMode.Streaming:
                    {
                        entryHeader.PackedStream = stream;
                        break;
                    }

                default:
                    {
                        throw new InvalidFormatException("Invalid StreamingMode");
                    }
            }

            //}
        }
    }
}