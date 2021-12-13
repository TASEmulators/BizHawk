using System;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="CRC32">custom implementation</see> of CRC-32 (i.e. POSIX cksum)</summary>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public sealed class CRC32Checksum : Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 32;

		internal const string NAME = "CRC32";

		internal const string PREFIX = NAME;

		public static CRC32Checksum Compute(ReadOnlySpan<byte> data)
			=> FromDigestBytes(CRC32.Calculate(data));

		public static CRC32Checksum FromDigestBytes(byte[] digest)
		{
			AssertCorrectLength(EXPECTED_LENGTH, digest.Length * 8, NAME);
			return new(digest);
		}

		public static CRC32Checksum FromDigestBytes(uint digest)
		{
			var a = BitConverter.GetBytes(digest);
			return new(new[] { a[3], a[2], a[1], a[0] });
		}

		/// <param name="hexEncode">w/o prefix</param>
		public static CRC32Checksum FromHexEncoding(string hexEncode)
		{
			AssertCorrectLength(EXPECTED_LENGTH, hexEncode.Length * 4, NAME);
			return new(hexEncode.HexStringToBytes());
		}

		protected override string Prefix => PREFIX;

		private CRC32Checksum(byte[] digest)
			: base(digest) {}
	}
}
