using System;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <remarks>
	/// TODO binary encoding https://multiformats.io/multihash/
	/// </remarks>
	public abstract class Checksum : IComparable<Checksum>, IEquatable<Checksum>
	{
		public static readonly NotAChecksum NotAChecksum = new(Array.Empty<byte>());

		protected static void AssertCorrectLength(int expectedLength, int actualLength, string algorithm)
		{
			if (actualLength != expectedLength) throw new ArgumentException($"wrong digest length, expected {expectedLength} bits for {algorithm}");
		}

		/// <param name="confidence"><c>0</c> indicates full confidence (i.e. an unambiguously-formatted string was given), higher numbers indicate more assumptions were made</param>
		/// <remarks>half-Span-ified</remarks>
		public static Checksum Parse(ReadOnlySpan<char> str, out int confidence)
		{
			static Checksum Fail()
				=> throw new ArgumentException("unknown hashing algorithm", nameof(str));
			var i = str.IndexOf(':');
			if (i is -1)
			{
				string tail;
				if (str.Length % 2 is 0)
				{
					tail = str.ToString();
					confidence = 5;
				}
				else
				{
					tail = $"0{str.ToString()}";
					confidence = 6;
				}
				var bytes = tail.HexStringToBytes();
				return bytes.Length switch
				{
					CRC32Checksum.EXPECTED_LENGTH / 8 => CRC32Checksum.FromDigestBytes(bytes),
					MD5Checksum.EXPECTED_LENGTH / 8 => MD5Checksum.FromDigestBytes(bytes),
					SHA1Checksum.EXPECTED_LENGTH / 8 => SHA1Checksum.FromDigestBytes(bytes),
					SHA256Checksum.EXPECTED_LENGTH / 8 => SHA256Checksum.FromDigestBytes(bytes),
					_ => Fail()
				};
			}
			else
			{
				static Checksum? Inner(string alg, string tail)
					=> alg switch
					{
						CRC32Checksum.PREFIX => CRC32Checksum.FromDigestBytes(tail.HexStringToBytes()),
						MD5Checksum.PREFIX => MD5Checksum.FromDigestBytes(tail.HexStringToBytes()),
						SHA1Checksum.PREFIX => SHA1Checksum.FromDigestBytes(tail.HexStringToBytes()),
						SHA256Checksum.PREFIX => SHA256Checksum.FromDigestBytes(tail.HexStringToBytes()),
						_ => null
					};
				var tail = str.Slice(i + 1).ToString();
				var alg = str.Slice(0, i).ToString();
				var first = Inner(alg, tail);
				if (first is not null)
				{
					confidence = 0;
					return first;
				}
				confidence = 1;
				return Inner(alg.ToUpperInvariant(), tail) ?? Inner(alg.ToLowerInvariant(), tail) ?? Fail();
			}
		}

		/// <inheritdoc cref="Parse(System.ReadOnlySpan{char},out int)"/>
		public static Checksum Parse(string str, out int confidence)
			=> Parse(str.AsSpan(), out confidence);

		public static bool operator ==(Checksum? a, Checksum? b)
			=> (a is null && b is null)
				|| (a is not null && b is not null && a.Equals(b));

		public static bool operator !=(Checksum? a, Checksum? b)
			=> !(a == b);

		protected readonly byte[] _digest;

		private string? _encodedForHumans;

		public ReadOnlySpan<byte> Digest => _digest;

		protected abstract string Prefix { get; }

		public Checksum(byte[] digest)
			=> _digest = digest;

		public int CompareTo(Checksum other)
		{
			var algOrder = Prefix.CompareTo(other.Prefix);
			if (algOrder is not 0) return algOrder;
			return Digest.SequenceCompareTo(other.Digest);
		}

		public string DigestHexEncoded()
			=> _digest.BytesToHexString();

		public bool Equals(Checksum other)
			=> ReferenceEquals(other, this)
				|| (other.Prefix == Prefix && other.Digest.SequenceEqual(Digest));

		public override bool Equals(object? obj)
			=> ReferenceEquals(obj, this)
				|| (obj is Checksum other && other.Prefix == Prefix && other.Digest.SequenceEqual(Digest));

		public override int GetHashCode()
		{
#if false // only in .NET Standard 2.1 or later, there is a backport on Nuget but it doesn't work
			HashCode h = new();
			foreach (var b in Digest) h.Add(b);
			h.Add(Prefix);
			return h.ToHashCode();
#endif
			var i = 0;
			foreach (var b in Digest)
			{
				i ^= b;
				i <<= 5;
			}
			return i ^ Prefix.GetHashCode();
		}

		public override string ToString()
			=> _encodedForHumans ??= $"{Prefix}:{DigestHexEncoded()}";
	}
}
