using System.Diagnostics;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA512"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public static class SHA512Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 512;

		internal const string PREFIX = "SHA512";

#if NET7_0_OR_GREATER
		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> SHA512.HashData(data);
#else
		private static SHA512? _sha512Impl;

		private static SHA512 SHA512Impl
		{
			get
			{
				if (_sha512Impl == null)
				{
					_sha512Impl = SHA512.Create();
					Debug.Assert(_sha512Impl.CanReuseTransform && _sha512Impl.HashSize is EXPECTED_LENGTH, "nonstandard implementation?");
				}
				return _sha512Impl;
			}
		}

		public static byte[] Compute(byte[] data)
			=> SHA512Impl.ComputeHash(data);

		public static string ComputeDigestHex(byte[] data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(byte[] data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";

		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());
#endif

		public static string ComputeDigestHex(ReadOnlySpan<byte> data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(ReadOnlySpan<byte> data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";
	}
}
