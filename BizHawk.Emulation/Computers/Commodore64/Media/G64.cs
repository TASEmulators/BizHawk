using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Media
{
	public static class G64
	{
		public static Disk Read(byte[] source)
		{
			MemoryStream mem = new MemoryStream(source);
			BinaryReader reader = new BinaryReader(mem);
			Disk result = new Disk();
			string id = new string(reader.ReadChars(8));

			if (id == @"GCR-1541")
			{
				int trackCount;
				int[] trackOffsetTable = new int[84];
				int[] trackSpeedTable = new int[84];

				reader.ReadByte(); //version
				trackCount = reader.ReadByte();
				reader.ReadInt16(); //max track size in bytes

				for (int i = 0; i < 84; i++)
					trackOffsetTable[i] = reader.ReadInt32();

				for (int i = 0; i < 84; i++)
					trackSpeedTable[i] = reader.ReadInt32();

				for (int i = 0; i < 84; i++)
				{
					if (trackOffsetTable[i] > 0)
					{
						int trackLength;
						byte[] trackData;
						Track track = new Track();

						mem.Position = trackOffsetTable[i];
						trackLength = reader.ReadInt16();
						trackData = reader.ReadBytes(trackLength);
						track.bits = trackLength * 8;
						track.data = trackData;
						track.density = trackSpeedTable[i];
						track.index = i;
						result.tracks.Add(track);
					}
				}
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
