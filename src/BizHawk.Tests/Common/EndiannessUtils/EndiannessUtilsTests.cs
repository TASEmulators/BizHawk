using BizHawk.Common;

namespace BizHawk.Tests.Common
{
	[TestClass]
	public sealed class EndiannessUtilsTests
	{
		private static readonly byte[] expected = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

		[TestMethod]
		public void TestByteSwap16()
		{
			var b = new byte[] { 0x23, 0x01, 0x67, 0x45, 0xAB, 0x89, 0xEF, 0xCD }.AsSpan();
			var a = b.ToArray().AsSpan();
			EndiannessUtils.MutatingByteSwap16(a);
			Assert.IsTrue(a.SequenceEqual(expected));
			EndiannessUtils.MutatingByteSwap16(a);
			Assert.IsTrue(a.SequenceEqual(b));
		}

		[TestMethod]
		public void TestByteSwap32()
		{
			var b = new byte[] { 0x67, 0x45, 0x23, 0x01, 0xEF, 0xCD, 0xAB, 0x89 }.AsSpan();
			var a = b.ToArray().AsSpan();
			EndiannessUtils.MutatingByteSwap32(a);
			Assert.IsTrue(a.SequenceEqual(expected));
			EndiannessUtils.MutatingByteSwap32(a);
			Assert.IsTrue(a.SequenceEqual(b));
		}

		[TestMethod]
		public void TestShortSwap32()
		{
			var b = new byte[] { 0x45, 0x67, 0x01, 0x23, 0xCD, 0xEF, 0x89, 0xAB }.AsSpan();
			var a = b.ToArray().AsSpan();
			EndiannessUtils.MutatingShortSwap32(a);
			Assert.IsTrue(a.SequenceEqual(expected));
			EndiannessUtils.MutatingShortSwap32(a);
			Assert.IsTrue(a.SequenceEqual(b));
		}
	}
}
