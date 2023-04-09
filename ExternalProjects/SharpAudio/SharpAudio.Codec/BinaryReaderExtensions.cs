using System.IO;

namespace SharpAudio.Codec
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadFourCc(this BinaryReader reader, bool bigEndian = false)
        {
            var a = reader.ReadChar();
            var b = reader.ReadChar();
            var c = reader.ReadChar();
            var d = reader.ReadChar();

            return bigEndian
                ? new string(new[] { d, c, b, a })
                : new string(new[] { a, b, c, d });
        }
    }

    internal static class StreamExtensions
    {
        public static byte[] ReadFourCc(this Stream reader, bool bigEndian = false)
        {
            byte a = (byte) reader.ReadByte();
            byte b = (byte) reader.ReadByte();
            byte c = (byte) reader.ReadByte();
            byte d = (byte) reader.ReadByte();

            return new[] { a, b, c, d };
        }
    }
}
