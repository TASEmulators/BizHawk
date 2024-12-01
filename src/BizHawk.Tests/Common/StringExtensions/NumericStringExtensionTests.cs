using BizHawk.Common.StringExtensions;

namespace BizHawk.Tests.Common.StringExtensions
{
	[TestClass]
	public class NumericStringExtensionTests
	{
		[TestMethod]
		public void TesCleanHex()
		{
			Assert.AreEqual("0123456789ABCDEFABCDEF", "0123456789ABCDEFabcdef".CleanHex());
			Assert.AreEqual("ABCDEF", "0xABCDEF".CleanHex());
			Assert.AreEqual("ABCDEF", "$ABCDEF".CleanHex());
			Assert.AreEqual("ABCDEF", " AB CD\nEF ".CleanHex());
			Assert.AreEqual("ABCDEF", " 0xABCDEF ".CleanHex());

			Assert.AreEqual(string.Empty, (null as string).CleanHex());
			Assert.AreEqual(string.Empty, string.Empty.CleanHex());
			Assert.AreEqual(string.Empty, "0x$ABCDEF".CleanHex());
			Assert.AreEqual(string.Empty, "$0xABCDEF".CleanHex());
			Assert.AreEqual(string.Empty, "$$ABCDEF".CleanHex());
			Assert.AreEqual(string.Empty, "ABCDEF$".CleanHex());
			Assert.AreEqual(string.Empty, "A!B.C(DE)F".CleanHex());
		}
	}
}
