using System;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="CRC32">custom implementation</see> of CRC-32 (i.e. POSIX cksum)</summary>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public static class CRC32Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 32;

		internal const string PREFIX = "CRC32";

		public static byte[] BytesAsDigest(uint digest)
			=> BitConverter.GetBytes(digest);

		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> BytesAsDigest(CRC32.Calculate(data));

		public static string ComputeDigestHex(ReadOnlySpan<byte> data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(ReadOnlySpan<byte> data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";
	}
}
