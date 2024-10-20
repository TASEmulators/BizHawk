using System.Text;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Tests.Common.checksums
{
	[DoNotParallelize]
	[TestClass]
	public sealed class SHA1Tests
	{
		[TestMethod]
		public void TestSHA1Empty()
		{
			byte[] data = [ ]; // empty data
			byte[] expectedSha = [ 0xDA, 0x39, 0xA3, 0xEE, 0x5E, 0x6B, 0x4B, 0x0D, 0x32, 0x55, 0xBF, 0xEF, 0x95, 0x60, 0x18, 0x90, 0xAF, 0xD8, 0x07, 0x09 ];

			Assert.IsTrue(expectedSha.SequenceEqual(SHA1Checksum.Compute(data)));
		}

		[TestMethod]
		public void TestSHA1Simple()
		{
			byte[] data = "hash"u8.ToArray(); // random short data
			byte[] expectedSha = [ 0x23, 0x46, 0xAD, 0x27, 0xD7, 0x56, 0x8B, 0xA9, 0x89, 0x6F, 0x1B, 0x7D, 0xA6, 0xB5, 0x99, 0x12, 0x51, 0xDE, 0xBD, 0xF2 ];

			Assert.IsTrue(expectedSha.SequenceEqual(SHA1Checksum.Compute(data)));
			Assert.IsTrue(expectedSha.SequenceEqual(SHA1Checksum.ComputeConcat(Array.Empty<byte>(), data)));
			Assert.IsTrue(expectedSha.SequenceEqual(SHA1Checksum.ComputeConcat(data, Array.Empty<byte>())));

			data = "ha"u8.ToArray();
			byte[] data2 = "sh"u8.ToArray();

			Assert.IsTrue(expectedSha.SequenceEqual(SHA1Checksum.ComputeConcat(data, data2)));
		}

		[TestMethod]
		public void TestSHA1LessSimple()
		{
			const string testString = "The quick brown fox jumps over the lazy dog.";
			byte[] data = Encoding.ASCII.GetBytes(testString);
			byte[] expectedSha1 = [ 0x40, 0x8D, 0x94, 0x38, 0x42, 0x16, 0xF8, 0x90, 0xFF, 0x7A, 0x0C, 0x35, 0x28, 0xE8, 0xBE, 0xD1, 0xE0, 0xB0, 0x16, 0x21 ];

			Assert.IsTrue(expectedSha1.SequenceEqual(SHA1Checksum.Compute(data)));

			data = new byte[65];
			Encoding.ASCII.GetBytes(testString).CopyTo(data, 0);

			byte[] expectedSha2 = [ 0x65, 0x87, 0x84, 0xE2, 0x68, 0xBF, 0xB1, 0x67, 0x94, 0x7B, 0xB7, 0xF3, 0xFB, 0x76, 0x69, 0x62, 0x79, 0x3E, 0x8C, 0x46 ];
			Assert.IsTrue(expectedSha2.SequenceEqual(SHA1Checksum.Compute(data.AsSpan(0, 64))));

			byte[] expectedSha3 = [ 0x34, 0xF3, 0xA2, 0x57, 0xBD, 0x12, 0x5E, 0x6E, 0x0E, 0x28, 0xD0, 0xE5, 0xDA, 0xBE, 0x22, 0x28, 0x97, 0xFA, 0x69, 0x55 ];
			Assert.IsTrue(expectedSha3.SequenceEqual(SHA1Checksum.Compute(data)));
		}
	}
}
