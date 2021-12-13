using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="MD5"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public sealed class MD5Checksum : Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 128;

		internal const string NAME = "MD5";

		internal const string PREFIX = NAME;

#if NET6_0
		public static MD5Checksum Compute(ReadOnlySpan<byte> data)
			=> new(MD5.HashData(data));
#else
		private static MD5? _md5Impl;

		private static MD5 MD5Impl
		{
			get
			{
				if (_md5Impl == null)
				{
					_md5Impl = MD5.Create();
					Debug.Assert(_md5Impl.CanReuseTransform && _md5Impl.HashSize is EXPECTED_LENGTH);
				}
				return _md5Impl;
			}
		}

		public static MD5Checksum Compute(byte[] data)
			=> new(MD5Impl.ComputeHash(data));

		public static MD5Checksum Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());
#endif

		public static MD5Checksum FromDigestBytes(byte[] digest)
		{
			AssertCorrectLength(EXPECTED_LENGTH, digest.Length * 8, NAME);
			return new(digest);
		}

		/// <param name="hexEncode">w/o prefix</param>
		public static MD5Checksum FromHexEncoding(string hexEncode)
		{
			AssertCorrectLength(EXPECTED_LENGTH, hexEncode.Length * 4, NAME);
			return new(hexEncode.HexStringToBytes());
		}

		protected override string Prefix => PREFIX;

		private MD5Checksum(byte[] digest)
			: base(digest) {}
	}
}
