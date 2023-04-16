using System;
using System.Diagnostics;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA1"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public static class SHA1Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 160;

		internal const string PREFIX = "SHA1";

		public /*static readonly*/const string Dummy = "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";

		public /*static readonly*/const string EmptyFile = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

		public /*static readonly*/const string Zero = "0000000000000000000000000000000000000000";

#if NET6_0
		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> SHA1.HashData(data);

		public static byte[] ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
		{
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return impl.GetHashAndReset();
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

		public static byte[] Compute(byte[] data)
			=> SHA1Impl.ComputeHash(data);

		public static byte[] ComputeConcat(byte[] dataA, byte[] dataB)
		{
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return impl.GetHashAndReset();
		}

		public static string ComputeDigestHex(byte[] data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(byte[] data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";

		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());

		public static byte[] ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
			=> ComputeConcat(dataA.ToArray(), dataB.ToArray());
#endif

		public static string ComputeDigestHex(ReadOnlySpan<byte> data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(ReadOnlySpan<byte> data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";
	}
}
