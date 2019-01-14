using System;
using System.IO;

namespace SharpCompress.Compressors.Xz
{
    internal static class MultiByteIntegers
    {
        public static ulong ReadXZInteger(this BinaryReader reader, int MaxBytes = 9)
        {
            if (MaxBytes <= 0)
                throw new ArgumentOutOfRangeException();
            if (MaxBytes > 9)
                MaxBytes = 9;

            byte LastByte = reader.ReadByte();
            ulong Output = (ulong)LastByte & 0x7F;

            int i = 0;
            while ((LastByte & 0x80) != 0)
            {
                if (++i >= MaxBytes)
                    throw new InvalidDataException();
                LastByte = reader.ReadByte();
                if (LastByte == 0)
                    throw new InvalidDataException();

                Output |= ((ulong)(LastByte & 0x7F)) << (i * 7);
            }
            return Output;
        }
    }
}
