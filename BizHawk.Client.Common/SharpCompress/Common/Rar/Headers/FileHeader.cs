#if !Rar2017_64bit
using nint = System.Int32;
using nuint = System.UInt32;
using size_t = System.UInt32;
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif

using SharpCompress.IO;
using System;
using System.IO;
using System.Text;

namespace SharpCompress.Common.Rar.Headers
{
    internal class FileHeader : RarHeader
    {
        private uint _fileCrc;

        public FileHeader(RarHeader header, RarCrcBinaryReader reader, HeaderType headerType) 
            : base(header, reader, headerType) 
        {
        }

        protected override void ReadFinish(MarkingBinaryReader reader) 
        {
            if (IsRar5) 
            {
                ReadFromReaderV5(reader);
            } 
            else 
            {
                ReadFromReaderV4(reader);
            }
        }

        private void ReadFromReaderV5(MarkingBinaryReader reader)
        {
            Flags = reader.ReadRarVIntUInt16();

            var lvalue = checked((long)reader.ReadRarVInt());

            // long.MaxValue causes the unpack code to finish when the input stream is exhausted
            UncompressedSize = HasFlag(FileFlagsV5.UNPACKED_SIZE_UNKNOWN) ? long.MaxValue : lvalue;

            FileAttributes = reader.ReadRarVIntUInt32();

            if (HasFlag(FileFlagsV5.HAS_MOD_TIME)) {
                FileLastModifiedTime = Utility.UnixTimeToDateTime(reader.ReadUInt32());
            }

            if (HasFlag(FileFlagsV5.HAS_CRC32)) {
                FileCrc = reader.ReadUInt32();
            }

            var compressionInfo = reader.ReadRarVIntUInt16();

            // Lower 6 bits (0x003f mask) contain the version of compression algorithm, resulting in possible 0 - 63 values. Current version is 0.
            // "+ 50" to not mix with old RAR format algorithms. For example,
            // we may need to use the compression algorithm 15 in the future,
            // but it was already used in RAR 1.5 and Unpack needs to distinguish
            // them.
            CompressionAlgorithm = (byte)((compressionInfo & 0x3f) + 50);
            
            // 7th bit (0x0040) defines the solid flag. If it is set, RAR continues to use the compression dictionary left after processing preceding files. 
            // It can be set only for file headers and is never set for service headers.
            IsSolid = (compressionInfo & 0x40) == 0x40;

            // Bits 8 - 10 (0x0380 mask) define the compression method. Currently only values 0 - 5 are used. 0 means no compression.
            CompressionMethod = (byte)((compressionInfo >> 7) & 0x7);

            // Bits 11 - 14 (0x3c00) define the minimum size of dictionary size required to extract data. Value 0 means 128 KB, 1 - 256 KB, ..., 14 - 2048 MB, 15 - 4096 MB.
            WindowSize = IsDirectory ? 0 : ((size_t)0x20000) << ((compressionInfo>>10) & 0xf);

            HostOs = reader.ReadRarVIntByte();

            var nameSize = reader.ReadRarVIntUInt16();

            // Variable length field containing Name length bytes in UTF-8 format without trailing zero.
            // For file header this is a name of archived file. Forward slash character is used as the path separator both for Unix and Windows names.
            // Backslashes are treated as a part of name for Unix names and as invalid character for Windows file names. Type of name is defined by Host OS field.
            //
            // TODO: not sure if anything needs to be done to handle the following:
            // If Unix file name contains any high ASCII characters which cannot be correctly converted to Unicode and UTF-8
            // we map such characters to to 0xE080 - 0xE0FF private use Unicode area and insert 0xFFFE Unicode non-character
            // to resulting string to indicate that it contains mapped characters, which need to be converted back when extracting. 
            // Concrete position of 0xFFFE is not defined, we need to search the entire string for it. Such mapped names are not
            // portable and can be correctly unpacked only on the same system where they were created.
            //
            // For service header this field contains a name of service header. Now the following names are used:
            // CMT	Archive comment
            // QO	Archive quick open data
            // ACL	NTFS file permissions
            // STM	NTFS alternate data stream
            // RR	Recovery record
            var b = reader.ReadBytes(nameSize);
            FileName = ConvertPathV5(Encoding.UTF8.GetString(b, 0, b.Length));

            // extra size seems to be redudant since we know the total header size
            if (ExtraSize != RemainingHeaderBytes(reader)) 
            {
                throw new InvalidFormatException("rar5 header size / extra size inconsistency");
            }

            isEncryptedRar5 = false;

            while (RemainingHeaderBytes(reader) > 0) {
                var size = reader.ReadRarVIntUInt16();
                int n = RemainingHeaderBytes(reader);
                var type = reader.ReadRarVIntUInt16();
                switch (type) {
                    //TODO
                    case 1: // file encryption
                        {
                            isEncryptedRar5 = true;

                            //var version = reader.ReadRarVIntByte();
                            //if (version != 0) throw new InvalidFormatException("unknown encryption algorithm " + version);
                                                    }
                        break;
                    //                    case 2: // file hash
                    //                        {
                    //
                    //                        }
                    //                        break;
                    case 3: // file time
                        {
                            ushort flags = reader.ReadRarVIntUInt16();
                            var isWindowsTime = (flags & 1) == 0;
                            if ((flags & 0x2) == 0x2) {
                                FileLastModifiedTime = ReadExtendedTimeV5(reader, isWindowsTime);
                            }
                            if ((flags & 0x4) == 0x4) {
                                FileCreatedTime = ReadExtendedTimeV5(reader, isWindowsTime);
                            }
                            if ((flags & 0x8) == 0x8) {
                                FileLastAccessedTime = ReadExtendedTimeV5(reader, isWindowsTime);
                            }
                        }
                        break;
//TODO
//                    case 4: // file version
//                        {
//
//                        }
//                        break;
//                    case 5: // file system redirection
//                        {
//
//                        }
//                        break;
//                    case 6: // unix owner
//                        {
//
//                        }
//                        break;
//                    case 7: // service data
//                        {
//
//                        }
//                        break;

                    default:
                        // skip unknown record types to allow new record types to be added in the future
                        break;
                }
                // drain any trailing bytes of extra record
                int did = n - RemainingHeaderBytes(reader);
                int drain = size - did;
                if (drain > 0) 
                {
                    reader.ReadBytes(drain);
                }
            }

            if (AdditionalDataSize != 0) {
                CompressedSize = AdditionalDataSize;
            }
        }


        private static DateTime ReadExtendedTimeV5(MarkingBinaryReader reader, bool isWindowsTime) 
        {
            if (isWindowsTime) 
            {
                return DateTime.FromFileTime(reader.ReadInt64());
            } 
            else 
            {
                return Utility.UnixTimeToDateTime(reader.ReadUInt32());
            }
        }

        private static string ConvertPathV5(string path)
        {
#if NO_FILE
            // not sure what to do here
            throw new NotImplementedException("TODO");
#else
            if (Path.DirectorySeparatorChar == '\\')
            {
                // replace embedded \\ with valid filename char
                return path.Replace('\\', '-').Replace('/', '\\');
            } 
            return path;
#endif
        }


        private void ReadFromReaderV4(MarkingBinaryReader reader)
        {
            Flags = HeaderFlags;
            IsSolid = HasFlag(FileFlagsV4.SOLID);
            WindowSize = IsDirectory ? 0U : ((size_t)0x10000) << ((Flags & FileFlagsV4.WINDOW_MASK) >> 5);

            uint lowUncompressedSize = reader.ReadUInt32();

            HostOs = reader.ReadByte();

            FileCrc = reader.ReadUInt32();

            FileLastModifiedTime = Utility.DosDateToDateTime(reader.ReadUInt32());

            CompressionAlgorithm = reader.ReadByte();
            CompressionMethod = (byte)(reader.ReadByte() - 0x30);

            short nameSize = reader.ReadInt16();

            FileAttributes = reader.ReadUInt32();

            uint highCompressedSize = 0;
            uint highUncompressedkSize = 0;
            if (HasFlag(FileFlagsV4.LARGE))
            {
                highCompressedSize = reader.ReadUInt32();
                highUncompressedkSize = reader.ReadUInt32();
            }
            else
            {
                if (lowUncompressedSize == 0xffffffff)
                {
                    lowUncompressedSize = 0xffffffff;
                    highUncompressedkSize = int.MaxValue;
                }
            }
            CompressedSize = UInt32To64(highCompressedSize, checked((uint)AdditionalDataSize));
            UncompressedSize = UInt32To64(highUncompressedkSize, lowUncompressedSize);

            nameSize = nameSize > 4 * 1024 ? (short)(4 * 1024) : nameSize;

            byte[] fileNameBytes = reader.ReadBytes(nameSize);

            const int saltSize = 8;
            const int newLhdSize = 32;

            switch (HeaderCode)
            {
                case HeaderCodeV.RAR4_FILE_HEADER:
                    {
                        if (HasFlag(FileFlagsV4.UNICODE))
                        {
                            int length = 0;
                            while (length < fileNameBytes.Length
                                   && fileNameBytes[length] != 0)
                            {
                                length++;
                            }
                            if (length != nameSize)
                            {
                                length++;
                                FileName = FileNameDecoder.Decode(fileNameBytes, length);
                            }
                            else
                            {
                                FileName = ArchiveEncoding.Decode(fileNameBytes);
                            }
                        }
                        else
                        {
                            FileName = ArchiveEncoding.Decode(fileNameBytes);
                        }
                        FileName = ConvertPathV4(FileName);
                    }
                    break;
                case HeaderCodeV.RAR4_NEW_SUB_HEADER:
                    {
                        int datasize = HeaderSize - newLhdSize - nameSize;
                        if (HasFlag(FileFlagsV4.SALT))
                        {
                            datasize -= saltSize;
                        }
                        if (datasize > 0)
                        {
                            SubData = reader.ReadBytes(datasize);
                        }

                        if (NewSubHeaderType.SUBHEAD_TYPE_RR.Equals(fileNameBytes))
                        {
                            RecoverySectors = SubData[8] + (SubData[9] << 8)
                                              + (SubData[10] << 16) + (SubData[11] << 24);
                        }
                    }
                    break;
            }

            if (HasFlag(FileFlagsV4.SALT))
            {
                R4Salt = reader.ReadBytes(saltSize);
            }
            if (HasFlag(FileFlagsV4.EXT_TIME))
            {
                // verify that the end of the header hasn't been reached before reading the Extended Time.
                //  some tools incorrectly omit Extended Time despite specifying FileFlags.EXTTIME, which most parsers tolerate.
                if (RemainingHeaderBytes(reader) >= 2)
                {
                    ushort extendedFlags = reader.ReadUInt16();
                    FileLastModifiedTime = ProcessExtendedTimeV4(extendedFlags, FileLastModifiedTime, reader, 0);
                    FileCreatedTime = ProcessExtendedTimeV4(extendedFlags, null, reader, 1);
                    FileLastAccessedTime = ProcessExtendedTimeV4(extendedFlags, null, reader, 2);
                    FileArchivedTime = ProcessExtendedTimeV4(extendedFlags, null, reader, 3);
                }
            }
        }

        private static long UInt32To64(uint x, uint y)
        {
            long l = x;
            l <<= 32;
            return l + y;
        }

        private static DateTime? ProcessExtendedTimeV4(ushort extendedFlags, DateTime? time, MarkingBinaryReader reader, int i)
        {
            uint rmode = (uint)extendedFlags >> (3 - i) * 4;
            if ((rmode & 8) == 0)
            {
                return null;
            }
            if (i != 0)
            {
                uint dosTime = reader.ReadUInt32();
                time = Utility.DosDateToDateTime(dosTime);
            }
            if ((rmode & 4) == 0)
            {
                time = time.Value.AddSeconds(1);
            }
            uint nanosecondHundreds = 0;
            int count = (int)rmode & 3;
            for (int j = 0; j < count; j++)
            {
                byte b = reader.ReadByte();
                nanosecondHundreds |= (((uint)b) << ((j + 3 - count) * 8));
            }

            //10^-7 to 10^-3
            return time.Value.AddMilliseconds(nanosecondHundreds * Math.Pow(10, -4));
        }

        private static string ConvertPathV4(string path)
        {
#if NO_FILE
            return path.Replace('\\', '/');
#else
            if (Path.DirectorySeparatorChar == '/')
            {
                return path.Replace('\\', '/');
            }
            else if (Path.DirectorySeparatorChar == '\\')
            {
                return path.Replace('/', '\\');
            }
            return path;
#endif
        }

        public override string ToString()
        {
            return FileName;
        }

        private ushort Flags { get; set; }

        private bool HasFlag(ushort flag) 
        {
            return (Flags & flag) == flag;
        }

        internal uint FileCrc 
        { 
            get { 
                if (IsRar5 && !HasFlag(FileFlagsV5.HAS_CRC32)) {
//!!! rar5: 
                    throw new InvalidOperationException("TODO rar5");
                }
                return _fileCrc; 
            } 
            private set => _fileCrc = value;
        }

        // 0 - storing
        // 1 - fastest compression
        // 2 - fast compression
        // 3 - normal compression
        // 4 - good compression
        // 5 - best compression
        internal byte CompressionMethod { get; private set; }
        internal bool IsStored => CompressionMethod == 0;

        // eg (see DoUnpack())
        //case 15: // rar 1.5 compression
        //case 20: // rar 2.x compression
        //case 26: // files larger than 2GB
        //case 29: // rar 3.x compression
        //case 50: // RAR 5.0 compression algorithm.
        internal byte CompressionAlgorithm { get; private set; }
        
        public bool IsSolid { get; private set; }

        // unused for UnpackV1 implementation (limitation)
        internal size_t WindowSize { get; private set; }

        internal byte[] R4Salt { get; private set; }

        private byte HostOs { get; set; }
        internal uint FileAttributes { get; private set; }
        internal long CompressedSize { get; private set; }
        internal long UncompressedSize { get; private set; }
        internal string FileName { get; private set; }
        internal byte[] SubData { get; private set; }
        internal int RecoverySectors { get; private set; }
        internal long DataStartPosition { get; set; }
        public Stream PackedStream { get; set; }

        public bool IsSplitAfter => IsRar5 ? HasHeaderFlag(HeaderFlagsV5.SPLIT_AFTER) : HasFlag(FileFlagsV4.SPLIT_AFTER);

        public bool IsDirectory => HasFlag(IsRar5 ? FileFlagsV5.DIRECTORY : FileFlagsV4.DIRECTORY);

        private bool isEncryptedRar5 = false;
        public bool IsEncrypted => IsRar5 ? isEncryptedRar5: HasFlag(FileFlagsV4.PASSWORD);
        
        internal DateTime? FileLastModifiedTime { get; private set; }

        internal DateTime? FileCreatedTime { get; private set; }

        internal DateTime? FileLastAccessedTime { get; private set; }

        internal DateTime? FileArchivedTime { get; private set; }
    }
}