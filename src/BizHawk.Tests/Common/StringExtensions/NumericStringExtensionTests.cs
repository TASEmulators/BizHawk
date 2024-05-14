using Microsoft.VisualStudio.TestTools.UnitTesting;
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

			Assert.AreEqual("", (null as string).CleanHex());
			Assert.AreEqual("", "".CleanHex());
			Assert.AreEqual("", "0x$ABCDEF".CleanHex());
			Assert.AreEqual("", "$0xABCDEF".CleanHex());
			Assert.AreEqual("", "$$ABCDEF".CleanHex());
			Assert.AreEqual("", "ABCDEF$".CleanHex());
			Assert.AreEqual("", "A!B.C(DE)F".CleanHex());
		}
	}
}
