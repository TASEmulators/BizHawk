using System.IO;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class G64
	{
		public static Disk Read(byte[] source)
		{
			var mem = new MemoryStream(source);
			var reader = new BinaryReader(mem);
			var result = new Disk();
			var id = new string(reader.ReadChars(8));

			if (id == @"GCR-1541")
			{
				int trackCount;
				var trackOffsetTable = new int[84];
				var trackSpeedTable = new int[84];

				reader.ReadByte(); //version
				trackCount = reader.ReadByte();
				reader.ReadInt16(); //max track size in bytes

				for (var i = 0; i < 84; i++)
					trackOffsetTable[i] = reader.ReadInt32();

				for (var i = 0; i < 84; i++)
					trackSpeedTable[i] = reader.ReadInt32();

				for (var i = 0; i < 84; i++)
				{
					if (trackOffsetTable[i] > 0)
					{
					    var track = new Track();

						mem.Position = trackOffsetTable[i];
						int trackLength = reader.ReadInt16();
						var trackData = reader.ReadBytes(trackLength);
						track.bits = trackLength * 8;
						track.data = trackData;
						track.density = trackSpeedTable[i];
						track.index = i;
						result.tracks.Add(track);
					}
				}
			}

			result.valid = result.tracks.Count > 0;
			return result;
		}

		public static byte[] Write(Disk source)
		{
			return null;
		}
	}
}
