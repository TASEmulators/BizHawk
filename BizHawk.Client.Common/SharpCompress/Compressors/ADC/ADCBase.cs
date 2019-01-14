//
// ADC.cs
//
// Author:
//       Natalia Portillo <claunia@claunia.com>
//
// Copyright (c) 2016 © Claunia.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;

namespace SharpCompress.Compressors.ADC
{
    /// <summary>
    /// Provides static methods for decompressing Apple Data Compression data
    /// </summary>
    public static class ADCBase
    {
        private const int PLAIN = 1;
        private const int TWO_BYTE = 2;
        private const int THREE_BYTE = 3;

        private static int GetChunkType(byte byt)
        {
            if ((byt & 0x80) == 0x80)
            {
                return PLAIN;
            }
            if ((byt & 0x40) == 0x40)
            {
                return THREE_BYTE;
            }
            return TWO_BYTE;
        }

        private static int GetChunkSize(byte byt)
        {
            switch (GetChunkType(byt))
            {
                case PLAIN:
                    return (byt & 0x7F) + 1;
                case TWO_BYTE:
                    return ((byt & 0x3F) >> 2) + 3;
                case THREE_BYTE:
                    return (byt & 0x3F) + 4;
                default:
                    return -1;
            }
        }

        private static int GetOffset(byte[] chunk, int position)
        {
            switch (GetChunkType(chunk[position]))
            {
                case PLAIN:
                    return 0;
                case TWO_BYTE:
                    return ((chunk[position] & 0x03) << 8) + chunk[position + 1];
                case THREE_BYTE:
                    return (chunk[position + 1] << 8) + chunk[position + 2];
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Decompresses a byte buffer that's compressed with ADC
        /// </summary>
        /// <param name="input">Compressed buffer</param>
        /// <param name="output">Buffer to hold decompressed data</param>
        /// <param name="bufferSize">Max size for decompressed data</param>
        /// <returns>How many bytes are stored on <paramref name="output"/></returns>
        public static int Decompress(byte[] input, out byte[] output, int bufferSize = 262144)
        {
            return Decompress(new MemoryStream(input), out output, bufferSize);
        }

        /// <summary>
        /// Decompresses a stream that's compressed with ADC
        /// </summary>
        /// <param name="input">Stream containing compressed data</param>
        /// <param name="output">Buffer to hold decompressed data</param>
        /// <param name="bufferSize">Max size for decompressed data</param>
        /// <returns>How many bytes are stored on <paramref name="output"/></returns>
        public static int Decompress(Stream input, out byte[] output, int bufferSize = 262144)
        {
            output = null;

            if (input == null || input.Length == 0)
            {
                return 0;
            }

            int start = (int)input.Position;
            int position = (int)input.Position;
            int chunkSize;
            int offset;
            int chunkType;
            byte[] buffer = new byte[bufferSize];
            int outPosition = 0;
            bool full = false;
            MemoryStream tempMs;

            while (position < input.Length)
            {
                int readByte = input.ReadByte();
                if (readByte == -1)
                {
                    break;
                }

                chunkType = GetChunkType((byte)readByte);

                switch (chunkType)
                {
                    case PLAIN:
                        chunkSize = GetChunkSize((byte)readByte);
                        if (outPosition + chunkSize > bufferSize)
                        {
                            full = true;
                            break;
                        }
                        input.Read(buffer, outPosition, chunkSize);
                        outPosition += chunkSize;
                        position += chunkSize + 1;
                        break;
                    case TWO_BYTE:
                        tempMs = new MemoryStream();
                        chunkSize = GetChunkSize((byte)readByte);
                        tempMs.WriteByte((byte)readByte);
                        tempMs.WriteByte((byte)input.ReadByte());
                        offset = GetOffset(tempMs.ToArray(), 0);
                        if (outPosition + chunkSize > bufferSize)
                        {
                            full = true;
                            break;
                        }
                        if (offset == 0)
                        {
                            byte lastByte = buffer[outPosition - 1];
                            for (int i = 0; i < chunkSize; i++)
                            {
                                buffer[outPosition] = lastByte;
                                outPosition++;
                            }
                            position += 2;
                        }
                        else
                        {
                            for (int i = 0; i < chunkSize; i++)
                            {
                                buffer[outPosition] = buffer[outPosition - offset - 1];
                                outPosition++;
                            }
                            position += 2;
                        }
                        break;
                    case THREE_BYTE:
                        tempMs = new MemoryStream();
                        chunkSize = GetChunkSize((byte)readByte);
                        tempMs.WriteByte((byte)readByte);
                        tempMs.WriteByte((byte)input.ReadByte());
                        tempMs.WriteByte((byte)input.ReadByte());
                        offset = GetOffset(tempMs.ToArray(), 0);
                        if (outPosition + chunkSize > bufferSize)
                        {
                            full = true;
                            break;
                        }
                        if (offset == 0)
                        {
                            byte lastByte = buffer[outPosition - 1];
                            for (int i = 0; i < chunkSize; i++)
                            {
                                buffer[outPosition] = lastByte;
                                outPosition++;
                            }
                            position += 3;
                        }
                        else
                        {
                            for (int i = 0; i < chunkSize; i++)
                            {
                                buffer[outPosition] = buffer[outPosition - offset - 1];
                                outPosition++;
                            }
                            position += 3;
                        }
                        break;
                }

                if (full)
                {
                    break;
                }
            }

            output = new byte[outPosition];
            Array.Copy(buffer, 0, output, 0, outPosition);
            return position - start;
        }
    }
}