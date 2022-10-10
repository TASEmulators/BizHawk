using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>Uses a simple magic number to detect N64 rom format, then byteswaps the ROM to ensure a consistent endianness/order</summary>
	/// <remarks>http://n64dev.org/romformats.html</remarks>
	public static class N64RomByteswapper
	{
		/// <remarks>not actually magic, just always the same in commercial carts? https://n64brew.dev/wiki/ROM_Header works all the same</remarks>
		private static readonly byte[] MAGIC_BYTES = { 0x80, 0x37, 0x12, 0x40 };

		/// <summary>ensures <paramref name="rom"/> is in the native (<c>.z64</c>) format, mutating it in-place if necessary</summary>
		public static void ToZ64Native(Span<byte> rom)
		{
			if (rom[0] == MAGIC_BYTES[1]) EndiannessUtils.MutatingByteSwap16(rom); // byte-swapped (.v64)
			else if (rom[0] == MAGIC_BYTES[3]) EndiannessUtils.MutatingByteSwap32(rom); // rare little-endian .n64
			// else already native (.z64)
		}
	}
}
