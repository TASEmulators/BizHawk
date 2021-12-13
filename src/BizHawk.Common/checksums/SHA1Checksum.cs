using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA1"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public sealed class SHA1Checksum : Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 160;

		internal const string NAME = "SHA-1";

		internal const string PREFIX = "SHA1";

#if NET6_0
		public static SHA1Checksum Compute(ReadOnlySpan<byte> data)
			=> new(SHA1.HashData(data));

		public static SHA1Checksum ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
		{
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return new(impl.GetHashAndReset());
		}
#else
		private static SHA1? _sha1Impl;

		private static SHA1 SHA1Impl
		{
			get
			{
				if (_sha1Impl == null)
				{
					_sha1Impl = SHA1.Create();
					Debug.Assert(_sha1Impl.CanReuseTransform && _sha1Impl.HashSize is EXPECTED_LENGTH);
				}
				return _sha1Impl;
			}
		}

		public static SHA1Checksum Compute(byte[] data)
			=> new(SHA1Impl.ComputeHash(data));

		public static SHA1Checksum ComputeConcat(byte[] dataA, byte[] dataB)
		{
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return new(impl.GetHashAndReset());
		}

		public static SHA1Checksum Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());

		public static SHA1Checksum ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
			=> ComputeConcat(dataA.ToArray(), dataB.ToArray());
#endif

		public static SHA1Checksum FromDigestBytes(byte[] digest)
		{
			AssertCorrectLength(EXPECTED_LENGTH, digest.Length * 8, NAME);
			return new(digest);
		}

		/// <param name="hexEncode">w/o prefix</param>
		public static SHA1Checksum FromHexEncoding(string hexEncode)
		{
			AssertCorrectLength(EXPECTED_LENGTH, hexEncode.Length * 4, NAME);
			return new(hexEncode.HexStringToBytes());
		}

		protected override string Prefix => PREFIX;

		private SHA1Checksum(byte[] digest)
			: base(digest) {}
	}
}
