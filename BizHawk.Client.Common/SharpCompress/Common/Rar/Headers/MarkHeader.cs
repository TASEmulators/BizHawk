using System;
using System.IO;

namespace SharpCompress.Common.Rar.Headers
{
    internal class MarkHeader : IRarHeader
    {
        private const int MAX_SFX_SIZE = 0x80000 - 16; //archive.cpp line 136

        internal bool OldNumberingFormat { get; private set; }

        public bool IsRar5 { get; }

        private MarkHeader(bool isRar5) 
        { 
            IsRar5 = isRar5;
        }

        public HeaderType HeaderType => HeaderType.Mark;

        private static byte GetByte(Stream stream) 
        {
            var b = stream.ReadByte();
            if (b != -1) 
            {
                return (byte)b;
            }
            throw new EndOfStreamException();
        }

        public static MarkHeader Read(Stream stream, bool leaveStreamOpen, bool lookForHeader) 
        {
            int maxScanIndex = lookForHeader ? MAX_SFX_SIZE : 0;
            try
            {
                int start = -1;
                var b = GetByte(stream); start++;
                while (start <= maxScanIndex) 
                {
                    // Rar old signature: 52 45 7E 5E
                    // Rar4 signature:    52 61 72 21 1A 07 00
                    // Rar5 signature:    52 61 72 21 1A 07 01 00
                    if (b == 0x52) 
                    {
                        b = GetByte(stream); start++;
                        if (b == 0x61) 
                        {
                            b = GetByte(stream); start++;
                            if (b != 0x72) continue;
                            b = GetByte(stream); start++;
                            if (b != 0x21) continue;
                            b = GetByte(stream); start++;
                            if (b != 0x1a) continue;
                            b = GetByte(stream); start++;
                            if (b != 0x07) continue;

                            b = GetByte(stream); start++;
                            if (b == 1) 
                            {
                                b = GetByte(stream); start++;
                                if (b != 0) continue;
                                return new MarkHeader(true); // Rar5
                            } 
                            else if (b == 0) 
                            {
                                return new MarkHeader(false); // Rar4
                            } 
                        } 
                        else if (b == 0x45) 
                        {
                            b = GetByte(stream); start++;
                            if (b != 0x7e) continue;
                            b = GetByte(stream); start++;
                            if (b != 0x5e) continue;
                            throw new InvalidFormatException("Rar format version pre-4 is unsupported.");
                        } 
                    } 
                    else 
                    {
                        b = GetByte(stream); start++;
                    }
                }
            }
            catch (Exception e)
            {
                if (!leaveStreamOpen)
                {
                    stream.Dispose();
                }
                throw new InvalidFormatException("Error trying to read rar signature.", e);
            }

            throw new InvalidFormatException("Rar signature not found");
        }
    }
}
