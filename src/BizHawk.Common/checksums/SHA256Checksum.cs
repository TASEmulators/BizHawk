using System;
using System.Diagnostics;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA256"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	public static class SHA256Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 256;

		internal const string PREFIX = "SHA256";

#if NET6_0
		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> SHA256.HashData(data);
#else
		private static SHA256? _sha256Impl;

		private static SHA256 SHA256Impl
		{
			get
			{
				if (_sha256Impl == null)
				{
					_sha256Impl = SHA256.Create();
					Debug.Assert(_sha256Impl.CanReuseTransform && _sha256Impl.HashSize is EXPECTED_LENGTH);
				}
				return _sha256Impl;
			}
		}

		public static byte[] Compute(byte[] data)
			=> SHA256Impl.ComputeHash(data);

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
