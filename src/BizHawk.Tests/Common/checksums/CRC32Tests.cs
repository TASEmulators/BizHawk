using BizHawk.Common;

namespace BizHawk.Tests.Common.checksums
{
	[DoNotParallelize] // I only noticed problems w/ SHA1, but better safe than sorry --yoshi
	[TestClass]
	public sealed class CRC32Tests
	{
		private const uint EXPECTED = 0xDA3BA10AU;

		private const uint EXPECTED_COMBINED = 0xED182BB0U;

		private const uint EXPECTED_EXTRA = 0x29058C73U;

		[TestMethod]
		public void TestCRC32Stability()
		{
			static byte[] InitialiseArray()
			{
				var a = new byte[0x100];
				for (var i = 0; i < 0x100; i++) a[i] = (byte) ~i;
				return a;
			}
			static byte[] InitialiseArrayExtra()
			{
				var a = new byte[0x100];
				for (var i = 0; i < 0x100; i++) a[i] = (byte) i;
				return a;
			}

			var data = InitialiseArray();
			Assert.AreEqual(EXPECTED, CRC32.Calculate(data));

			data = InitialiseArray();
			CRC32 crc32 = new();
			crc32.Add(data);
			Assert.AreEqual(EXPECTED, crc32.Result);

			var dataExtra = InitialiseArrayExtra();
			CRC32 crc32Extra = new();
			crc32Extra.Add(dataExtra);
			Assert.AreEqual(EXPECTED_EXTRA, crc32Extra.Result);
			crc32.Incorporate(crc32Extra.Result, dataExtra.Length);
			Assert.AreEqual(EXPECTED_COMBINED, crc32.Result);
		}
	}
}
