using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class D64
	{
		private static readonly int[] DensityTable =
		{
			3, 3, 3, 3, 3,
			3, 3, 3, 3, 3,
			3, 3, 3, 3, 3,
			3, 3, 2, 2, 2,
			2, 2, 2, 2, 1,
			1, 1, 1, 1, 1,
			0, 0, 0, 0, 0,
			0, 0, 0, 0, 0
		};

		private static readonly int[] GcrDecodeTable =
		{
			0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //00xxx
			0xFF, 0x08, 0x00, 0x01, 0xFF, 0x0C, 0x04, 0x05, //01xxx
			0xFF, 0xFF, 0x02, 0x03, 0xFF, 0x0F, 0x06, 0x07, //10xxx
			0xFF, 0x09, 0x0A, 0x0B, 0xFF, 0x0D, 0x0E, 0xFF  //11xxx
		};

		private static readonly int[] GcrEncodeTable =
		{
			Convert.ToByte("01010", 2), // 0
			Convert.ToByte("01011", 2), // 1
			Convert.ToByte("10010", 2), // 2
			Convert.ToByte("10011", 2), // 3
			Convert.ToByte("01110", 2), // 4
			Convert.ToByte("01111", 2), // 5
			Convert.ToByte("10110", 2), // 6
			Convert.ToByte("10111", 2), // 7
			Convert.ToByte("01001", 2), // 8
			Convert.ToByte("11001", 2), // 9
			Convert.ToByte("11010", 2), // A
			Convert.ToByte("11011", 2), // B
			Convert.ToByte("01101", 2), // C
			Convert.ToByte("11101", 2), // D
			Convert.ToByte("11110", 2), // E
			Convert.ToByte("10101", 2)  // F
		};

		private static readonly int[] SectorsPerTrack =
		{
			21, 21, 21, 21, 21,
			21, 21, 21, 21, 21,
			21, 21, 21, 21, 21,
			21, 21, 19, 19, 19,
			19, 19, 19, 19, 18,
			18, 18, 18, 18, 18,
			17, 17, 17, 17, 17,
			17, 17, 17, 17, 17
		};

		private static readonly int[] StandardTrackLengthBytes =
		{
			6250, 6666, 7142, 7692
		};

		private static byte Checksum(byte[] source)
		{
			var count = source.Length;
			byte result = 0;

			for (var i = 0; i < count; i++)
				result ^= source[i];

			return result;
		}

		private static byte[] ConvertSectorToGcr(byte[] source, byte sectorNo, byte trackNo, byte formatA, byte formatB, out int bitsWritten)
		{
		    using (var mem = new MemoryStream())
		    {
                var writer = new BinaryWriter(mem);
                var headerChecksum = (byte)(sectorNo ^ trackNo ^ formatA ^ formatB);

                // assemble written data for GCR encoding
                var writtenData = new byte[260];
                Array.Copy(source, 0, writtenData, 1, 256);
                writtenData[0] = 0x07;
                writtenData[0x101] = Checksum(source);
                writtenData[0x102] = 0x00;
                writtenData[0x103] = 0x00;

                writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // sync
                writer.Write(EncodeGcr(new byte[] { 0x08, headerChecksum, sectorNo, trackNo, formatA, formatB, 0x0F, 0x0F })); // header
                writer.Write(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 }); // gap
                writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // sync
                writer.Write(EncodeGcr(writtenData)); // data
                writer.Write(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 }); // gap

                bitsWritten = (int)mem.Length * 8;

                writer.Flush();
                return mem.ToArray();
            }
        }

		private static byte[] EncodeGcr(byte[] source)
		{
			// 4 bytes -> 5 GCR encoded bytes
			var gcr = new int[8];
			var data = new byte[4];
			var count = source.Length;
		    using (var mem = new MemoryStream())
		    {
                var writer = new BinaryWriter(mem);

                for (var i = 0; i < count; i += 4)
                {
                    Array.Copy(source, i, data, 0, 4);
                    gcr[0] = GcrEncodeTable[data[0] >> 4];
                    gcr[1] = GcrEncodeTable[data[0] & 0xF];
                    gcr[2] = GcrEncodeTable[data[1] >> 4];
                    gcr[3] = GcrEncodeTable[data[1] & 0xF];
                    gcr[4] = GcrEncodeTable[data[2] >> 4];
                    gcr[5] = GcrEncodeTable[data[2] & 0xF];
                    gcr[6] = GcrEncodeTable[data[3] >> 4];
                    gcr[7] = GcrEncodeTable[data[3] & 0xF];

                    // -------- -------- -------- -------- --------
                    // 00000111 11222223 33334444 45555566 66677777

                    var outputValue = (gcr[0] << 3) | (gcr[1] >> 2);
                    writer.Write((byte)(outputValue & 0xFF));
                    outputValue = (gcr[1] << 6) | (gcr[2] << 1) | (gcr[3] >> 4);
                    writer.Write((byte)(outputValue & 0xFF));
                    outputValue = (gcr[3] << 4) | (gcr[4] >> 1);
                    writer.Write((byte)(outputValue & 0xFF));
                    outputValue = (gcr[4] << 7) | (gcr[5] << 2) | (gcr[6] >> 3);
                    writer.Write((byte)(outputValue & 0xFF));
                    outputValue = (gcr[6] << 5) | (gcr[7]);
                    writer.Write((byte)(outputValue & 0xFF));
                }
                writer.Flush();
                return mem.ToArray();
            }
        }

		public static Disk Read(byte[] source)
		{
		    using (var mem = new MemoryStream(source))
		    {
                var reader = new BinaryReader(mem);
                var trackDatas = new List<byte[]>();
                var trackLengths = new List<int>();
                var trackNumbers = new List<int>();
                var trackDensities = new List<int>();
                int trackCount;

                switch (source.Length)
                {
                    case 174848: // 35 tracks no errors
                        trackCount = 35;
                        break;
                    case 175531: // 35 tracks with errors
                        trackCount = 35;
                        break;
                    case 196608: // 40 tracks no errors
                        trackCount = 40;
                        break;
                    case 197376: // 40 tracks with errors
                        trackCount = 40;
                        break;
                    default:
                        throw new Exception("Not able to identify capacity of the D64 file.");
                }

                for (var i = 0; i < trackCount; i++)
                {
                    var sectors = SectorsPerTrack[i];
                    var trackLengthBits = 0;
                    using (var trackMem = new MemoryStream())
                    {
                        for (var j = 0; j < sectors; j++)
                        {
                            int bitsWritten;
                            var sectorData = reader.ReadBytes(256);
                            var diskData = ConvertSectorToGcr(sectorData, (byte)j, (byte)(i + 1), 0xA0, 0xA0, out bitsWritten);
                            trackMem.Write(diskData, 0, diskData.Length);
                            trackLengthBits += bitsWritten;
                        }
                        var density = DensityTable[i];

                        // we pad the tracks with extra gap bytes to meet MNIB standards
                        while (trackMem.Length < StandardTrackLengthBytes[density])
                        {
                            trackMem.WriteByte(0x55);
                        }

                        trackDatas.Add(trackMem.ToArray());
                        trackLengths.Add(trackLengthBits);
                        trackNumbers.Add(i * 2);
                        trackDensities.Add(DensityTable[i]);
                    }
                }

                return new Disk(trackDatas, trackNumbers, trackDensities, 84);
            }
        }
	}
}
