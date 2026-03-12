using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class D64
	{
		private const int D64_DISK_ID_OFFSET = 0x165A2; // track 18, sector 0, 0xA2

		private enum ErrorType
		{
			NoError = 0x01,
			HeaderNotFound = 0x02,
			NoSyncSequence = 0x03,
			DataNotFound = 0x04,
			DataChecksumError = 0x05,
			WriteVerifyFormatError = 0x06,
			WriteVerifyError = 0x07,
			WriteProtectOn = 0x08,
			HeaderChecksumError = 0x09,
			WriteError = 0x0A,
			IdMismatch = 0x0B,
			DriveNotReady = 0x0F
		}

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
			0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 00xxx
			0xFF, 0x08, 0x00, 0x01, 0xFF, 0x0C, 0x04, 0x05, // 01xxx
			0xFF, 0xFF, 0x02, 0x03, 0xFF, 0x0F, 0x06, 0x07, // 10xxx
			0xFF, 0x09, 0x0A, 0x0B, 0xFF, 0x0D, 0x0E, 0xFF  // 11xxx
		};

		private static readonly int[] GcrEncodeTable =
		{
			0b01010, // 0
			0b01011, // 1
			0b10010, // 2
			0b10011, // 3
			0b01110, // 4
			0b01111, // 5
			0b10110, // 6
			0b10111, // 7
			0b01001, // 8
			0b11001, // 9
			0b11010, // A
			0b11011, // B
			0b01101, // C
			0b11101, // D
			0b11110, // E
			0b10101, // F
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

		private static readonly int[] StandardSectorGapLength =
		{
			9, 19, 13, 10
		};

		private static byte Checksum(byte[] source)
		{
			var count = source.Length;
			byte result = 0;

			for (var i = 0; i < count; i++)
			{
				result ^= source[i];
			}

			return result;
		}

		private static byte[] ConvertSectorToGcr(byte[] source, byte sectorNo, byte trackNo, byte formatA, byte formatB, int gapLength, ErrorType errorType, out int bitsWritten)
		{
			using var mem = new MemoryStream();
			using var writer = new BinaryWriter(mem);

			if (errorType == ErrorType.IdMismatch)
			{
				formatA ^= 0xFF;
				formatB ^= 0xFF;
			}

			var headerChecksum = (byte)(sectorNo ^ trackNo ^ formatA ^ formatB ^ (errorType == ErrorType.HeaderChecksumError ? 0xFF : 0x00));

			// assemble written data for GCR encoding
			var writtenData = new byte[260];
			var syncBytes40 = Enumerable.Repeat((byte) (errorType == ErrorType.NoSyncSequence ? 0x00 : 0xFF), 5).ToArray();

			Array.Copy(source, 0, writtenData, 1, 256);
			writtenData[0] = (byte)(errorType == ErrorType.HeaderNotFound ? 0x00 : 0x07);
			writtenData[0x101] = (byte)(Checksum(source) ^ (errorType == ErrorType.DataChecksumError ? 0xFF : 0x00));
			writtenData[0x102] = 0x00;
			writtenData[0x103] = 0x00;

			writer.Write(syncBytes40); // sync
			writer.Write(EncodeGcr(new byte[] { (byte)(errorType == ErrorType.DataNotFound ? 0x00 : 0x08), headerChecksum, sectorNo, trackNo, formatA, formatB, 0x0F, 0x0F })); // header
			writer.Write("UUUUUUUUU"u8.ToArray()); // gap
			writer.Write(syncBytes40); // sync
			writer.Write(EncodeGcr(writtenData)); // data
			writer.Write(Enumerable.Repeat((byte)0x55, gapLength).ToArray()); // gap

			bitsWritten = (int)mem.Length * 8;

			writer.Flush();
			return mem.ToArray();
		}

		private static byte[] EncodeGcr(byte[] source)
		{
			// 4 bytes -> 5 GCR encoded bytes
			var gcr = new int[8];
			var data = new byte[4];
			var count = source.Length;
			using var mem = new MemoryStream();
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

		public static Disk Read(byte[] source)
		{
			var formatB = source[D64_DISK_ID_OFFSET + 0x00];
			var formatA = source[D64_DISK_ID_OFFSET + 0x01];

			using var mem = new MemoryStream(source);
			var reader = new BinaryReader(mem);
			var trackDatas = new List<byte[]>();
			var trackLengths = new List<int>();
			var trackNumbers = new List<int>();
			var trackDensities = new List<int>();
			var errorType = ErrorType.NoError;
			int trackCount;
			int errorOffset = -1;

			switch (source.Length)
			{
				case 174848: // 35 tracks no errors
					trackCount = 35;
					break;
				case 175531: // 35 tracks with errors
					trackCount = 35;
					errorOffset = 174848;
					break;
				case 196608: // 40 tracks no errors
					trackCount = 40;
					break;
				case 197376: // 40 tracks with errors
					trackCount = 40;
					errorOffset = 196608;
					break;
				default:
					throw new Exception("Not able to identify capacity of the D64 file.");
			}

			for (var i = 0; i < trackCount; i++)
			{
				if (errorOffset >= 0)
				{
					errorType = (ErrorType) source[errorOffset];
					errorOffset++;
				}
				var sectors = SectorsPerTrack[i];
				var trackLengthBits = 0;
				using var trackMem = new MemoryStream();
				for (var j = 0; j < sectors; j++)
				{
					var sectorData = reader.ReadBytes(256);
					var diskData = ConvertSectorToGcr(sectorData, (byte)j, (byte)(i + 1), formatA, formatB, StandardSectorGapLength[DensityTable[i]], errorType, out var bitsWritten);
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

			return new Disk(trackDatas, trackNumbers, trackDensities, 84) {WriteProtected = false};
		}
	}
}
