using System;
using System.Text;
using SharpCompress.Converters;

namespace SharpCompress.Common.Zip.Headers
{
    internal enum ExtraDataType : ushort
    {
        WinZipAes = 0x9901,

        NotImplementedExtraData = 0xFFFF,

        // Third Party Mappings
        // -Info-ZIP Unicode Path Extra Field
        UnicodePathExtraField = 0x7075,
        Zip64ExtendedInformationExtraField = 0x0001
    }

    internal class ExtraData
    {
        internal ExtraDataType Type { get; set; }
        internal ushort Length { get; set; }
        internal byte[] DataBytes { get; set; }
    }

    internal class ExtraUnicodePathExtraField : ExtraData
    {
        internal byte Version => DataBytes[0];

        internal byte[] NameCrc32
        {
            get
            {
                var crc = new byte[4];
                Buffer.BlockCopy(DataBytes, 1, crc, 0, 4);
                return crc;
            }
        }

        internal string UnicodeName
        {
            get
            {
                // PathNamelength = dataLength - Version(1 byte) - NameCRC32(4 bytes)
                var length = Length - 5;
                var nameStr = Encoding.UTF8.GetString(DataBytes, 5, length);
                return nameStr;
            }
        }
    }

    internal class Zip64ExtendedInformationExtraField : ExtraData
    {

        public Zip64ExtendedInformationExtraField(ExtraDataType type, ushort length, byte[] dataBytes)
        {
            Type = type;
            Length = length;
            DataBytes = dataBytes;
            Process();
        }

        //From the spec values are only in the extradata if the standard
        //value is set to 0xFFFF, but if one of the sizes are present, both are.
        //Hence if length == 4 volume only
        //      if length == 8 offset only
        //      if length == 12 offset + volume
        //      if length == 16 sizes only
        //      if length == 20 sizes + volume
        //      if length == 24 sizes + offset
        //      if length == 28 everything.
        //It is unclear how many of these are used in the wild.

        private void Process()
        {
            switch (DataBytes.Length)
            {
                case 4:
                    VolumeNumber = DataConverter.LittleEndian.GetUInt32(DataBytes, 0);
                    return;
                case 8:
                    RelativeOffsetOfEntryHeader = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                    return;
                case 12:
                    RelativeOffsetOfEntryHeader = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                    VolumeNumber = DataConverter.LittleEndian.GetUInt32(DataBytes, 8);
                    return;
                case 16:
                     UncompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                     CompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 8);
                    return;
                case 20:
                    UncompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                    CompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 8);
                    VolumeNumber = DataConverter.LittleEndian.GetUInt32(DataBytes, 16);
                    return;
                case 24:
                    UncompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                    CompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 8);
                    RelativeOffsetOfEntryHeader = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 16);
                    return;
                case 28:
                    UncompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 0);
                    CompressedSize = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 8);
                    RelativeOffsetOfEntryHeader = (long)DataConverter.LittleEndian.GetUInt64(DataBytes, 16);
                    VolumeNumber = DataConverter.LittleEndian.GetUInt32(DataBytes, 24);
                    return;
                default:
                throw new ArchiveException("Unexpected size of of Zip64 extended information extra field");
            }
        }

        public long UncompressedSize { get; private set; }
        public long CompressedSize { get; private set; }
        public long RelativeOffsetOfEntryHeader { get; private set; }
        public uint VolumeNumber { get; private set; }
    }

    internal static class LocalEntryHeaderExtraFactory
    {
        internal static ExtraData Create(ExtraDataType type, ushort length, byte[] extraData)
        {
            switch (type)
            {
                case ExtraDataType.UnicodePathExtraField:
                    return new ExtraUnicodePathExtraField
                           {
                               Type = type,
                               Length = length,
                               DataBytes = extraData
                           };
                case ExtraDataType.Zip64ExtendedInformationExtraField:
                    return new Zip64ExtendedInformationExtraField
                            (
                                type, 
                                length,
                                extraData
                           );
                default:
                    return new ExtraData
                           {
                               Type = type,
                               Length = length,
                               DataBytes = extraData
                           };
            }
        }
    }
}