using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>Uses a simple magic number to detect N64 rom format, then byteswaps the ROM to ensure a consistent endianness/order</summary>
	/// <remarks>http://n64dev.org/romformats.html</remarks>
	public static class N64RomByteswapper
	{
		private static readonly byte[] MAGIC_BYTES_LE = { 0x40, 0x12, 0x37, 0x80 };

		/// <remarks>not actually magic, just always the same in commercial carts? https://n64brew.dev/wiki/ROM_Header works all the same</remarks>
		private static readonly byte[] MAGIC_BYTES_NATIVE = { 0x80, 0x37, 0x12, 0x40 };

		private static readonly byte[] MAGIC_BYTES_SWAPPED = { 0x37, 0x80, 0x40, 0x12 };

		/// <summary>ensures <paramref name="rom"/> is in the rare little-endian (<c>.n64</c>) format, mutating it in-place if necessary</summary>
		/// <returns><see langword="true"/> iff <paramref name="rom"/> was one of the 3 valid formats</returns>
		public static bool ToN64LittleEndian(Span<byte> rom)
		{
			var romMagicBytes = rom.Slice(start: 0, length: 4);
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_NATIVE))
			{
				EndiannessUtils.MutatingByteSwap32(rom);
				return true;
			}
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_SWAPPED))
			{
				EndiannessUtils.MutatingShortSwap32(rom);
				return true;
			}
			return romMagicBytes.SequenceEqual(MAGIC_BYTES_LE);
		}

		/// <summary>ensures <paramref name="rom"/> is in the byte-swapped (<c>.v64</c>) format, mutating it in-place if necessary</summary>
		/// <returns><see langword="true"/> iff <paramref name="rom"/> was one of the 3 valid formats</returns>
		public static bool ToV64ByteSwapped(Span<byte> rom)
		{
			var romMagicBytes = rom.Slice(start: 0, length: 4);
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_NATIVE))
			{
				EndiannessUtils.MutatingByteSwap16(rom);
				return true;
			}
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_SWAPPED)) return true;
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_LE))
			{
				EndiannessUtils.MutatingShortSwap32(rom);
				return true;
			}
			return false;
		}

		/// <summary>ensures <paramref name="rom"/> is in the native (<c>.z64</c>) format, mutating it in-place if necessary</summary>
		/// <returns><see langword="true"/> iff <paramref name="rom"/> was one of the 3 valid formats</returns>
		public static bool ToZ64Native(Span<byte> rom)
		{
			var romMagicBytes = rom.Slice(start: 0, length: 4);
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_NATIVE)) return true;
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_SWAPPED))
			{
				EndiannessUtils.MutatingByteSwap16(rom);
				return true;
			}
			if (romMagicBytes.SequenceEqual(MAGIC_BYTES_LE))
			{
				EndiannessUtils.MutatingByteSwap32(rom);
				return true;
			}
			return false;
		}
	}
}
