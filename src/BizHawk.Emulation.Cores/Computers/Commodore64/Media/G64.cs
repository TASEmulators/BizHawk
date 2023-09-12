using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class G64
	{
		public static Disk Read(byte[] source)
		{
			using MemoryStream mem = new(source);
			using BinaryReader reader = new(mem);
			string id = new(reader.ReadChars(8));
			List<byte[]> trackDatas = new();
			List<int> trackLengths = new();
			List<int> trackNumbers = new();
			List<int> trackDensities = new();

			if (id == @"GCR-1541")
			{
				reader.ReadByte(); // version
				int trackCount = reader.ReadByte();
				reader.ReadInt16(); // max track size in bytes

				int[] trackOffsetTable = new int[trackCount];
				int[] trackSpeedTable = new int[trackCount];

				for (int i = 0; i < trackCount; i++)
				{
					trackOffsetTable[i] = reader.ReadInt32();
				}

				for (int i = 0; i < trackCount; i++)
				{
					trackSpeedTable[i] = reader.ReadInt32();
				}

				for (int i = 0; i < trackCount; i++)
				{
					if (trackOffsetTable[i] > 0)
					{
						mem.Position = trackOffsetTable[i];
						int trackLength = reader.ReadInt16();
						byte[] trackData = reader.ReadBytes(trackLength);

						trackDatas.Add(trackData);
						trackLengths.Add(trackLength * 8);
						trackDensities.Add(trackSpeedTable[i]);
						trackNumbers.Add(i);
					}
				}

				if (trackSpeedTable.Any(ts => ts is > 3 or < 0))
				{
					throw new Exception("Byte-level speeds are not yet supported in the G64 loader.");
				}

				return new Disk(trackDatas, trackNumbers, trackDensities, 84) {WriteProtected = true};
			}

			return new Disk(84) {WriteProtected = false};
		}
		
		public static byte[] Write(IList<byte[]> trackData, IList<int> trackNumbers, IList<int> trackDensities)
		{
			const byte version = 0;
			const byte trackCount = 84;
			const int headerLength = 0xC;
			const byte dataFillerValue = 0xFF;

			ushort trackMaxLength = (ushort)Math.Max(7928, trackData.Max(d => d.Length));

			using MemoryStream mem = new();
			using BinaryWriter writer = new(mem);

			// header ID
			writer.Write("GCR-1541".ToCharArray());

			// version #
			writer.Write(version);

			// tracks in the image
			writer.Write(trackCount);

			// maximum track size in bytes
			writer.Write(trackMaxLength);

			// combine track data
			List<int> offsets = new();
			List<int> densities = new();
			using (MemoryStream trackMem = new())
			{
				BinaryWriter trackMemWriter = new(trackMem);
				for (int i = 0; i < trackCount; i++)
				{
					if (trackNumbers.Contains(i))
					{
						int trackIndex = trackNumbers.IndexOf(i);
						offsets.Add((int)trackMem.Length);
						densities.Add(trackDensities[trackIndex]);

						byte[] data = trackData[trackIndex];
						byte[] buffer = Enumerable.Repeat(dataFillerValue, trackMaxLength).ToArray();
						byte[] dataBytes = data.Select(d => unchecked(d)).ToArray();
						Array.Copy(dataBytes, buffer, dataBytes.Length);
						trackMemWriter.Write((ushort)dataBytes.Length);
						trackMemWriter.Write(buffer);
					}
					else
					{
						offsets.Add(-1);
						densities.Add(0);
					}
				}
				trackMemWriter.Flush();

				// offset table
				foreach (int offset in offsets.Select(o => o >= 0 ? o + headerLength + trackCount * 8 : 0))
				{
					writer.Write(offset);
				}

				// speed zone data
				foreach (int density in densities)
				{
					writer.Write(density);
				}

				// track data
				writer.Write(trackMem.ToArray());
			}

			writer.Flush();
			return mem.ToArray();
		}
	}
}
