using System;
using System.IO;

namespace SharpCompress.Compressors.Xz
{
    public static class BinaryUtils
    {
        public static int ReadLittleEndianInt32(this BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return (bytes[0] + (bytes[1] << 8) + (bytes[2] << 16) + (bytes[3] << 24));
        }

        internal static uint ReadLittleEndianUInt32(this BinaryReader reader)
        {
            return unchecked((uint)ReadLittleEndianInt32(reader));
        }
        public static int ReadLittleEndianInt32(this Stream stream)
        {
            byte[] bytes = new byte[4];
            var read = stream.ReadFully(bytes);
            if (!read)
            {
                throw new EndOfStreamException();
            }
            return (bytes[0] + (bytes[1] << 8) + (bytes[2] << 16) + (bytes[3] << 24));
        }

        internal static uint ReadLittleEndianUInt32(this Stream stream)
        {
            return unchecked((uint)ReadLittleEndianInt32(stream));
        }

        internal static byte[] ToBigEndianBytes(this uint uint32)
        {
            var result = BitConverter.GetBytes(uint32);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        internal static byte[] ToLittleEndianBytes(this uint uint32)
        {
            var result = BitConverter.GetBytes(uint32);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
    }
}
