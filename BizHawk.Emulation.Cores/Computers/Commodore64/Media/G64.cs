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
		    using (var mem = new MemoryStream(source))
		    {
                var reader = new BinaryReader(mem);
                var id = new string(reader.ReadChars(8));
                var trackDatas = new List<byte[]>();
                var trackLengths = new List<int>();
                var trackNumbers = new List<int>();
                var trackDensities = new List<int>();

                if (id == @"GCR-1541")
                {

                    reader.ReadByte(); //version
                    int trackCount = reader.ReadByte();
                    reader.ReadInt16(); //max track size in bytes

                    var trackOffsetTable = new int[trackCount];
                    var trackSpeedTable = new int[trackCount];

                    for (var i = 0; i < trackCount; i++)
                        trackOffsetTable[i] = reader.ReadInt32();

                    for (var i = 0; i < trackCount; i++)
                        trackSpeedTable[i] = reader.ReadInt32();

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

                    return new Disk(trackDatas, trackNumbers, trackDensities, 84);
                }

                return new Disk(84);
            }
        }
    }
}
