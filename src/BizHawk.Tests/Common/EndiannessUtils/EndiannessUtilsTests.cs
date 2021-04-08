using System.Linq;

using BizHawk.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Common
{
	[TestClass]
	public sealed class EndiannessUtilsTests
	{
		private static readonly byte[] expected = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

		[TestMethod]
		public void TestByteSwap16()
		{
			var a = new byte[] { 0x23, 0x01, 0x67, 0x45, 0xAB, 0x89, 0xEF, 0xCD };
			EndiannessUtils.MutatingByteSwap16(a);
			Assert.IsTrue(a.SequenceEqual(expected));
		}

		[TestMethod]
		public void TestByteSwap32()
		{
			var a = new byte[] { 0x67, 0x45, 0x23, 0x01, 0xEF, 0xCD, 0xAB, 0x89 };
			EndiannessUtils.MutatingByteSwap32(a);
			Assert.IsTrue(a.SequenceEqual(expected));
		}
	}
}
