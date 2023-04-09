using System;
using System.IO;

namespace SharpAudio.Codec.Wave
{
    internal class DviAdpcmParser : WavParser
    {
        private T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        private static readonly int[] ImaIndexTable = new int[8] {
              -1, -1, -1, -1, 2, 4, 6, 8
            };

        private static readonly int[] ImaStepTable = new int[89] {
              7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
              19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
              50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
              130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
              337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
              876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
              2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
              5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
              15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
            };

        //DviAdpcm is always 16 bits
        public override int BitsPerSample => 16;

        private Int16 Int16FromLittleEndianBytes(byte[] packed)
        {
            // This is always little endian, unlike the C# builtin for unpacking a byte array.
            return (short) (packed[1] << 8 | packed[0]);
        }

        public override byte[] Parse(BinaryReader reader, int size, WaveFormat format)
        {
            int samplesPerBlock = Int16FromLittleEndianBytes(format.ExtraBytes);

            int blockSize = (4 + (samplesPerBlock - 1) / 2);
            if (size % blockSize != 0)
            {
                throw new InvalidDataException("Invalid .wav DVI ADPCM data!");
            }
            int numBlocks = size / blockSize;
            var buffer = new byte[samplesPerBlock * 2 * numBlocks];
            using (var memoryStream = new MemoryStream(buffer))
            using (var writer = new BinaryWriter(memoryStream))
            {
                for (int i = 0; i < numBlocks; i++)
                {
                    int sample = reader.ReadInt16();
                    int stepTableIndex = reader.ReadByte();
                    byte reserved = reader.ReadByte(); // unused, commonly 0
                    int step = ImaStepTable[stepTableIndex];
                    writer.Write((Int16) sample);
                    for (int j = 0; j < (samplesPerBlock - 1) / 2; j++)
                    {
                        byte packed = reader.ReadByte();
                        Span<byte> nibbles = stackalloc byte[2];
                        nibbles[0] = (byte) (packed & 0xF);
                        nibbles[1] = (byte) (packed >> 4);
                        foreach (var nibble in nibbles)
                        {
                            stepTableIndex += ImaIndexTable[nibble & 0x7];
                            stepTableIndex = Clamp(stepTableIndex, 0, 88);
                            byte sign = (byte) (nibble & 8);
                            byte delta = (byte) (nibble & 7);
                            int diff = step >> 3;
                            if ((delta & 4) != 0)
                            {
                                diff += step;
                            }
                            if ((delta & 2) != 0)
                            {
                                diff += (step >> 1);
                            }
                            if ((delta & 1) != 0)
                            {
                                diff += (step >> 2);
                            }
                            if (sign != 0)
                            {
                                sample -= diff;
                            }
                            else
                            {
                                sample += diff;
                            }
                            step = ImaStepTable[stepTableIndex];
                            sample = Clamp(sample, Int16.MinValue, Int16.MaxValue);
                            writer.Write((Int16) sample);
                        }
                    }
                }
            }
            return buffer;
        }
    }
}
