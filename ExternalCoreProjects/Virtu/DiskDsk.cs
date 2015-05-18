using System;
using System.IO;
using Jellyfish.Library;

namespace Jellyfish.Virtu
{
    public enum SectorSkew { None = 0, Dos, ProDos };

    public sealed class DiskDsk : Disk525
    {
		public DiskDsk() { }
        public DiskDsk(string name, byte[] data, bool isWriteProtected, SectorSkew sectorSkew) :
            base(name, data, isWriteProtected)
        {
            _sectorSkew = SectorSkewMode[(int)sectorSkew];
        }

        public DiskDsk(string name, Stream stream, bool isWriteProtected, SectorSkew sectorSkew) :
            base(name, new byte[TrackCount * SectorCount * SectorSize], isWriteProtected)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.ReadBlock(Data);
            _sectorSkew = SectorSkewMode[(int)sectorSkew];
        }

        public override void ReadTrack(int number, int fraction, byte[] buffer)
        {
            int track = number / 2;

            _trackBuffer = buffer;
            _trackOffset = 0;

            WriteNibble(0xFF, 48); // gap 0

            for (int sector = 0; sector < SectorCount; sector++)
            {
                WriteNibble(0xD5); // address prologue
                WriteNibble(0xAA);
                WriteNibble(0x96);

                WriteNibble44(Volume);
                WriteNibble44(track);
                WriteNibble44(sector);
                WriteNibble44(Volume ^ track ^ sector);

                WriteNibble(0xDE); // address epilogue
                WriteNibble(0xAA);
                WriteNibble(0xEB);
                WriteNibble(0xFF, 8);

                WriteNibble(0xD5); // data prologue
                WriteNibble(0xAA);
                WriteNibble(0xAD);

                WriteDataNibbles((track * SectorCount + _sectorSkew[sector]) * SectorSize);

                WriteNibble(0xDE); // data epilogue
                WriteNibble(0xAA);
                WriteNibble(0xEB);
                WriteNibble(0xFF, 16);
            }
        }

        public override void WriteTrack(int number, int fraction, byte[] buffer)
        {
            if (IsWriteProtected)
                return;

            int track = number / 2;

            _trackBuffer = buffer;
            _trackOffset = 0;
            int sectorsDone = 0;

            for (int sector = 0; sector < SectorCount; sector++)
            {
                if (!Read3Nibbles(0xD5, 0xAA, 0x96, 0x304))
                    break; // no address prologue

                /*int readVolume = */ReadNibble44();

                int readTrack = ReadNibble44();
                if (readTrack != track)
                    break; // bad track number

                int readSector = ReadNibble44();
                if (readSector > SectorCount)
                    break; // bad sector number
                if ((sectorsDone & (0x1 << readSector)) != 0)
                    break; // already done this sector

                if (ReadNibble44() != (Volume ^ readTrack ^ readSector))
                    break; // bad address checksum

                if ((ReadNibble() != 0xDE) || (ReadNibble() != 0xAA))
                    break; // bad address epilogue

                if (!Read3Nibbles(0xD5, 0xAA, 0xAD, 0x20))
                    break; // no data prologue

                if (!ReadDataNibbles((track * SectorCount + _sectorSkew[sector]) * SectorSize))
                    break; // bad data checksum

                if ((ReadNibble() != 0xDE) || (ReadNibble() != 0xAA))
                    break; // bad data epilogue

                sectorsDone |= 0x1 << sector;
            }

            if (sectorsDone != 0xFFFF)
                throw new InvalidOperationException("disk error"); // TODO: we should alert the user and "dump" a NIB
        }

        private byte ReadNibble()
        {
            byte data = _trackBuffer[_trackOffset];
            if (_trackOffset++ == TrackSize)
            {
                _trackOffset = 0;
            }
            return data;
        }

        private bool Read3Nibbles(byte data1, byte data2, byte data3, int maxReads)
        {
            bool result = false;
            while (--maxReads > 0)
            {
                if (ReadNibble() != data1)
                    continue;

                if (ReadNibble() != data2)
                    continue;

                if (ReadNibble() != data3)
                    continue;

                result = true;
                break;
            }
            return result;
        }

        private int ReadNibble44()
        {
            return (((ReadNibble() << 1) | 0x1) & ReadNibble());
        }

        private byte ReadTranslatedNibble()
        {
            byte data = NibbleToByte[ReadNibble()];
            // TODO: check that invalid nibbles aren't used
            // (put 0xFFs for invalid nibbles in the table)
            //if (data == 0xFF)
            //{
                //throw an exception
            //}
            return data;
        }

        private bool ReadDataNibbles(int sectorOffset)
        {
            byte a, x, y;

            y = SecondaryBufferLength;
            a = 0;
            do // fill and de-nibblize secondary buffer
            {
                a = _secondaryBuffer[--y] = (byte)(a ^ ReadTranslatedNibble());
            }
            while (y > 0);

            do // fill and de-nibblize secondary buffer
            {
                a = _primaryBuffer[y++] = (byte)(a ^ ReadTranslatedNibble());
            }
            while (y != 0);

            int checksum = a ^ ReadTranslatedNibble(); // should be 0

            x = y = 0;
            do // decode data
            {
                if (x == 0)
                {
                    x = SecondaryBufferLength;
                }
                a = (byte)((_primaryBuffer[y] << 2) | SwapBits[_secondaryBuffer[--x] & 0x03]);
                _secondaryBuffer[x] >>= 2;
                Data[sectorOffset + y] = a;
            }
            while (++y != 0);

            return (checksum == 0);
        }

        private void WriteNibble(int data)
        {
            _trackBuffer[_trackOffset++] = (byte)data;
        }

        private void WriteNibble(int data, int count)
        {
            while (count-- > 0)
            {
                WriteNibble(data);
            }
        }

        private void WriteNibble44(int data)
        {
            WriteNibble((data >> 1) | 0xAA);
            WriteNibble(data | 0xAA);
        }

        private void WriteDataNibbles(int sectorOffset)
        {
            byte a, x, y;

            for (x = 0; x < SecondaryBufferLength; x++)
            {
                _secondaryBuffer[x] = 0; // zero secondary buffer
            }

            y = 2;
            do // fill buffers
            {
                x = 0;
                do
                {
                    a = Data[sectorOffset + --y];
                    _secondaryBuffer[x] = (byte)((_secondaryBuffer[x] << 2) | SwapBits[a & 0x03]); // b1,b0 -> secondary buffer
                    _primaryBuffer[y] = (byte)(a >> 2); // b7-b2 -> primary buffer
                }
                while (++x < SecondaryBufferLength);
            }
            while (y != 0);

            y = SecondaryBufferLength;
            do // write secondary buffer
            {
                WriteNibble(ByteToNibble[_secondaryBuffer[y] ^ _secondaryBuffer[y - 1]]);
            }
            while (--y != 0);

            a = _secondaryBuffer[0];
            do // write primary buffer
            {
                WriteNibble(ByteToNibble[a ^ _primaryBuffer[y]]);
                a = _primaryBuffer[y];
            }
            while (++y != 0);
            
            WriteNibble(ByteToNibble[a]); // data checksum
        }

        private byte[] _trackBuffer;
        private int _trackOffset;
        private byte[] _primaryBuffer = new byte[0x100];
        private const int SecondaryBufferLength = 0x56;
        private byte[] _secondaryBuffer = new byte[SecondaryBufferLength + 1];
        private int[] _sectorSkew;
        private const int Volume = 0xFE;

        private static readonly byte[] SwapBits = { 0, 2, 1, 3 };

        private static readonly int[] SectorSkewNone = new int[SectorCount]
        {
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF
        };

        private static readonly int[] SectorSkewDos = new int[SectorCount]
        {
            0x0, 0x7, 0xE, 0x6, 0xD, 0x5, 0xC, 0x4, 0xB, 0x3, 0xA, 0x2, 0x9, 0x1, 0x8, 0xF
        };

        private static readonly int[] SectorSkewProDos = new int[SectorCount]
        {
            0x0, 0x8, 0x1, 0x9, 0x2, 0xA, 0x3, 0xB, 0x4, 0xC, 0x5, 0xD, 0x6, 0xE, 0x7, 0xF
        };

        private const int SectorSkewCount = 3;

        private static readonly int[][] SectorSkewMode = new int[SectorSkewCount][]
        {
            SectorSkewNone, SectorSkewDos, SectorSkewProDos
        };

        private static readonly byte[] ByteToNibble = new byte[]
        {
            0x96, 0x97, 0x9A, 0x9B, 0x9D, 0x9E, 0x9F, 0xA6, 0xA7, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB2, 0xB3,
            0xB4, 0xB5, 0xB6, 0xB7, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xCB, 0xCD, 0xCE, 0xCF, 0xD3,
            0xD6, 0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF, 0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC,
            0xED, 0xEE, 0xEF, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
        };

        private static readonly byte[] NibbleToByte = new byte[]
        {
            // padding for offset (not used)
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
            0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
            0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
            0x90, 0x91, 0x92, 0x93, 0x94, 0x95,

            // nibble translate table
                                                0x00, 0x01, 0x98, 0x99, 0x02, 0x03, 0x9C, 0x04, 0x05, 0x06,
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x07, 0x08, 0xA8, 0xA9, 0xAA, 0x09, 0x0A, 0x0B, 0x0C, 0x0D,
            0xB0, 0xB1, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0xB8, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A,
            0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0x1B, 0xCC, 0x1C, 0x1D, 0x1E,
            0xD0, 0xD1, 0xD2, 0x1F, 0xD4, 0xD5, 0x20, 0x21, 0xD8, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0x29, 0x2A, 0x2B, 0xE8, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32,
            0xF0, 0xF1, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0xF8, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
        };
    }
}
