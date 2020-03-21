using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public static class IPS
	{
		public static byte[] Patch(byte[] rom, Stream patch)
		{
			var ipsHeader = new byte[5];
			patch.Read(ipsHeader, 0, 5);

			const string header = "PATCH";
			for (int i = 0; i < 5; i++)
			{
				if (ipsHeader[i] != header[i])
				{
					Console.WriteLine("Patch file specified is invalid.");
					return null;
				}
			}

			// header verified, loop over patch entries
			const uint eof = ('E' * 0x10000) + ('O' * 0x100) + 'F';

			var ret = new MemoryStream(rom.Length);
			ret.Write(rom, 0, rom.Length);

			while (true)
			{
				uint offset = Read24(patch);
				if (offset == eof)
				{
					return ret.ToArray();
				}

				ushort size = Read16(patch);

				ret.Seek(offset, SeekOrigin.Begin);

				if (size != 0) // non-RLE patch
				{
					var patchData = new byte[size];
					patch.Read(patchData, 0, size);

					ret.Write(patchData, 0, patchData.Length);
				}
				else // RLE patch
				{
					size = Read16(patch);
					byte value = (byte)patch.ReadByte();
					for (int i = 0; i < size; i++)
					{
						ret.WriteByte(value);
					}
				}
			}
		}

		private static ushort Read16(Stream patch)
		{
			int upper = patch.ReadByte();
			int lower = patch.ReadByte();
			return (ushort)((upper * 0x100) + lower);
		}

		private static uint Read24(Stream patch)
		{
			int upper = patch.ReadByte();
			int middle = patch.ReadByte();
			int lower = patch.ReadByte();
			return (uint)((upper * 0x10000) + (middle * 0x100) + lower);
		}
	}
}