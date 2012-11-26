using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public static class D64
	{
		private static int[] densityTable =
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

		private static int[] gcrDecodeTable =
		{
			0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //00xxx
			0xFF, 0x08, 0x00, 0x01, 0xFF, 0x0C, 0x04, 0x05, //01xxx
			0xFF, 0xFF, 0x02, 0x03, 0xFF, 0x0F, 0x06, 0x07, //10xxx
			0xFF, 0x09, 0x0A, 0x0B, 0xFF, 0x0D, 0x0E, 0xFF  //11xxx
		};

		private static int[] gcrEncodeTable =
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

		private static int[] sectorsPerTrack =
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

		private static int[] standardTrackLengthBytes =
		{
			6250, 6666, 7142, 7692
		};

		private static byte Checksum(byte[] source)
		{
			int count = source.Length;
			byte result = 0;

			for (int i = 0; i < count; i++)
				result ^= source[i];

			return result;
		}

		private static byte[] ConvertSectorFromGCR(byte[] source, out int bitsWritten)
		{
			bitsWritten = 0;
			return new byte[] { };
		}

		private static byte[] ConvertSectorToGCR(byte[] source, byte sectorNo, byte trackNo, byte formatA, byte formatB, out int bitsWritten)
		{
			MemoryStream mem = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(mem);
			byte[] writtenData;
			byte headerChecksum = (byte)(sectorNo ^ trackNo ^ formatA ^ formatB);

			// assemble written data for GCR encoding
			writtenData = new byte[260];
			Array.Copy(source, 0, writtenData, 1, 256);
			writtenData[0] = 0x07;
			writtenData[0x101] = Checksum(source);
			writtenData[0x102] = 0x00;
			writtenData[0x103] = 0x00;

			bitsWritten = 0;
			writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // sync
			writer.Write(EncodeGCR(new byte[] { 0x08, headerChecksum, sectorNo, trackNo, formatA, formatB, 0x0F, 0x0F })); // header
			writer.Write(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 }); // gap
			writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // sync
			writer.Write(EncodeGCR(writtenData)); // data
			writer.Write(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 }); // gap

			bitsWritten = (int)mem.Length * 8;

			writer.Flush();
			return mem.ToArray();
		}

		private static byte[] DecodeGCR(byte[] source)
		{
			// 5 GCR encoded bytes -> 4 bytes
			int[] gcr = new int[8];
			byte[] data = new byte[4];
			int count = source.Length;
			int outputValue;
			MemoryStream mem = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(mem);

			for (int i = 0; i < count; i += 5)
			{
				Array.Copy(source, i, data, 0, 5);

				// -------- -------- -------- -------- --------
				// 11100000 32222211 44443333 66555554 77777666

				gcr[0] = ((data[0])) & 0x1F;
				gcr[1] = ((data[0] >> 5) | data[1] << 3) & 0x1F;
				gcr[2] = ((data[1] >> 2)) & 0x1F;
				gcr[3] = ((data[1] >> 7) | data[2] << 1) & 0x1F;
				gcr[4] = ((data[2] >> 4) | data[3] << 4) & 0x1F;
				gcr[5] = ((data[3] >> 1)) & 0x1F;
				gcr[6] = ((data[3] >> 6) | data[4] << 2) & 0x1F;
				gcr[7] = ((data[4] >> 3)) & 0x1F;

				outputValue = gcrDecodeTable[gcr[0]] | (gcrDecodeTable[gcr[1]] << 4);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = gcrDecodeTable[gcr[2]] | (gcrDecodeTable[gcr[3]] << 4);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = gcrDecodeTable[gcr[4]] | (gcrDecodeTable[gcr[5]] << 4);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = gcrDecodeTable[gcr[6]] | (gcrDecodeTable[gcr[7]] << 4);
				writer.Write((byte)(outputValue & 0xFF));
			}
			writer.Flush();
			return mem.ToArray();
		}

		private static byte[] EncodeGCR(byte[] source)
		{
			// 4 bytes -> 5 GCR encoded bytes
			int[] gcr = new int[8];
			byte[] data = new byte[4];
			int count = source.Length;
			int outputValue;
			MemoryStream mem = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(mem);

			for (int i = 0; i < count; i += 4)
			{
				Array.Copy(source, i, data, 0, 4);
				gcr[0] = gcrEncodeTable[data[0] & 0xF];
				gcr[1] = gcrEncodeTable[data[0] >> 4];
				gcr[2] = gcrEncodeTable[data[1] & 0xF];
				gcr[3] = gcrEncodeTable[data[1] >> 4];
				gcr[4] = gcrEncodeTable[data[2] & 0xF];
				gcr[5] = gcrEncodeTable[data[2] >> 4];
				gcr[6] = gcrEncodeTable[data[3] & 0xF];
				gcr[7] = gcrEncodeTable[data[3] >> 4];
				
				// -------- -------- -------- -------- --------
				// 11100000 32222211 44443333 66555554 77777666

				outputValue = (gcr[0]) | (gcr[1] << 5);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = (gcr[1] >> 3) | (gcr[2] << 2) | (gcr[3] << 7);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = (gcr[3] >> 1) | (gcr[4] << 4);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = (gcr[4] >> 4) | (gcr[5] << 1) | (gcr[6] << 6);
				writer.Write((byte)(outputValue & 0xFF));
				outputValue = (gcr[6] >> 2) | (gcr[7] << 3);
				writer.Write((byte)(outputValue & 0xFF));
			}
			writer.Flush();
			return mem.ToArray();
		}

		public static Disk Read(byte[] source)
		{
			MemoryStream mem = new MemoryStream(source);
			BinaryReader reader = new BinaryReader(mem);
			Disk result = new Disk();
			int trackCount = 0;

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
			}

			for (int i = 0; i < trackCount; i++)
			{
				Track track = new Track();
				int sectors = sectorsPerTrack[i];
				MemoryStream trackMem = new MemoryStream();

				for (int j = 0; j < sectors; j++)
				{
					int bitsWritten;
					byte[] sectorData = reader.ReadBytes(256);
					byte[] diskData = ConvertSectorToGCR(sectorData, (byte)j, (byte)i, (byte)0x00, (byte)0x00, out bitsWritten);
					trackMem.Write(diskData, 0, diskData.Length);
				}
				track.density = densityTable[i];

				// we pad the tracks with extra gap bytes to meet MNIB standards
				while (trackMem.Length < standardTrackLengthBytes[track.density])
				{
					trackMem.WriteByte(0x55);
				}
				track.data = trackMem.ToArray();
				track.bits = (int)trackMem.Length;
				track.index = i;
				result.tracks.Add(track);
				trackMem.Dispose();
			}

			result.valid = (result.tracks.Count > 0);
			return result;
		}

		public static byte[] Write(Disk source)
		{
			return null;
		}
	}
}
