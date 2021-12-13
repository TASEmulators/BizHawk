using System;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Common.checksums
{
	[TestClass]
	public sealed class ChecksumParsingTests
	{
		private static void DoTest<T>(string withGoodPrefix, out Checksum hashFromStrWithGoodPrefix, out int confidenceFromStrWithoutPrefix)
			where T : Checksum
		{
			hashFromStrWithGoodPrefix = Checksum.Parse(withGoodPrefix, out var confidenceFromStrWithGoodPrefix);
			Assert.That.IsInstanceOfType<T>(hashFromStrWithGoodPrefix);
			Assert.AreEqual(withGoodPrefix, hashFromStrWithGoodPrefix.ToString());

			var hashFromStrLowercased = Checksum.Parse(withGoodPrefix.ToLowerInvariant(), out var confidenceFromStrLowercased);
			Assert.AreEqual(hashFromStrWithGoodPrefix, hashFromStrLowercased);
			Assert.IsTrue(confidenceFromStrWithGoodPrefix <= confidenceFromStrLowercased); // lower numbers indicate more confidence

			var withoutPrefix = withGoodPrefix.SubstringAfter(':');
			var hashFromStrWithoutPrefix = Checksum.Parse(withoutPrefix, out confidenceFromStrWithoutPrefix);
			Assert.AreEqual(hashFromStrWithGoodPrefix, hashFromStrWithoutPrefix);
			Assert.IsTrue(confidenceFromStrWithGoodPrefix < confidenceFromStrWithoutPrefix);

			var hashFromStrWithoutPrefixLowercased = Checksum.Parse(withoutPrefix.ToLowerInvariant(), out var confidenceFromStrWithoutPrefixLowercased);
			Assert.AreEqual(hashFromStrWithGoodPrefix, hashFromStrWithoutPrefixLowercased);
			Assert.IsTrue(confidenceFromStrWithoutPrefix <= confidenceFromStrWithoutPrefixLowercased);
		}

		[TestMethod]
		public void TestCRC32()
		{
			const string withGoodPrefix = "CRC32:0123CDEF";
			DoTest<CRC32Checksum>(withGoodPrefix, out var hashFromStrWithGoodPrefix, out var confidenceFromStrWithoutPrefix);

			var hashFromStrWithoutLeadingZeroOrPrefix = Checksum.Parse("123CDEF", out var confidenceFromStrWithoutLeadingZeroOrPrefix);
			Assert.AreEqual(hashFromStrWithGoodPrefix, hashFromStrWithoutLeadingZeroOrPrefix);
			Assert.IsTrue(confidenceFromStrWithoutPrefix < confidenceFromStrWithoutLeadingZeroOrPrefix); // lower numbers indicate more confidence

			Assert.ThrowsException<ArgumentException>(() => Checksum.Parse("CRC32:123CDEF", out _));
		}

		[TestMethod]
		public void TestMD5()
			=> DoTest<MD5Checksum>("MD5:0123456789ABCDEF0123456789ABCDEF", out _, out _);

		[TestMethod]
		public void TestSHA1()
			=> DoTest<SHA1Checksum>("SHA1:0123456789ABCDEF0123456789ABCDEF01234567", out _, out _);

		[TestMethod]
		public void TestSHA256()
			=> DoTest<SHA256Checksum>("SHA256:0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF", out _, out _);
	}
}
