using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA256"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	public sealed class SHA256Checksum : Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 256;

		internal const string NAME = "SHA-256";

		internal const string PREFIX = "SHA256";

#if NET6_0
		public static SHA256Checksum Compute(ReadOnlySpan<byte> data)
			=> new(SHA256.HashData(data));
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

		public static SHA256Checksum Compute(byte[] data)
			=> new(SHA256Impl.ComputeHash(data));

		public static SHA256Checksum Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());
#endif

		public static SHA256Checksum FromDigestBytes(byte[] digest)
		{
			AssertCorrectLength(EXPECTED_LENGTH, digest.Length * 8, NAME);
			return new(digest);
		}

		/// <param name="hexEncode">w/o prefix</param>
		public static SHA256Checksum FromHexEncoding(string hexEncode)
		{
			AssertCorrectLength(EXPECTED_LENGTH, hexEncode.Length * 4, NAME);
			return new(hexEncode.HexStringToBytes());
		}

		protected override string Prefix => PREFIX;

		private SHA256Checksum(byte[] digest)
			: base(digest) {}
	}
}
