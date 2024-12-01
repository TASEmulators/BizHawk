using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class G64
	{
		public static Disk Read(byte[] source)
		{
			using var mem = new MemoryStream(source);
			using var reader = new BinaryReader(mem);
			var id = new string(reader.ReadChars(8));
			var trackDatas = new List<byte[]>();
			var trackLengths = new List<int>();
			var trackNumbers = new List<int>();
			var trackDensities = new List<int>();

			if (id == @"GCR-1541")
			{
				reader.ReadByte(); // version
				int trackCount = reader.ReadByte();
				reader.ReadInt16(); // max track size in bytes

				var trackOffsetTable = new int[trackCount];
				var trackSpeedTable = new int[trackCount];

				for (var i = 0; i < trackCount; i++)
				{
					trackOffsetTable[i] = reader.ReadInt32();
				}

				for (var i = 0; i < trackCount; i++)
				{
					trackSpeedTable[i] = reader.ReadInt32();
				}

				for (var i = 0; i < trackCount; i++)
				{
					if (trackOffsetTable[i] > 0)
					{
						mem.Position = trackOffsetTable[i];
						int trackLength = reader.ReadInt16();
						var trackData = reader.ReadBytes(trackLength);

						trackDatas.Add(trackData);
						trackLengths.Add(trackLength * 8);
						trackDensities.Add(trackSpeedTable[i]);
						trackNumbers.Add(i);
					}
				}

				if (trackSpeedTable.Any(ts => ts > 3 || ts < 0))
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

			var trackMaxLength = (ushort)Math.Max(7928, trackData.Max(d => d.Length));

			using var mem = new MemoryStream();
			using var writer = new BinaryWriter(mem);

			// header ID
			writer.Write("GCR-1541".ToCharArray());

			// version #
			writer.Write(version);

			// tracks in the image
			writer.Write(trackCount);

			// maximum track size in bytes
			writer.Write(trackMaxLength);

			// combine track data
			var offsets = new List<int>();
			var densities = new List<int>();
			using (var trackMem = new MemoryStream())
			{
				var trackMemWriter = new BinaryWriter(trackMem);
				for (var i = 0; i < trackCount; i++)
				{
					if (trackNumbers.Contains(i))
					{
						var trackIndex = trackNumbers.IndexOf(i);
						offsets.Add((int)trackMem.Length);
						densities.Add(trackDensities[trackIndex]);

						var buffer = Enumerable.Repeat(dataFillerValue, trackMaxLength).ToArray();
						var dataBytes = trackData[trackIndex];
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
				foreach (var offset in offsets.Select(o => o >= 0 ? o + headerLength + trackCount * 8 : 0))
				{
					writer.Write(offset);
				}

				// speed zone data
				foreach (var density in densities)
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
