using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Tests.Common.StringExtensions
{
	[TestClass]
	public class StringExtensionTests
	{
		[TestMethod]
		public void In_CaseInsensitive()
		{
			var strArray = new[]
			{
				"Hello World"
			};

			var actual = "hello world".In(strArray);
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void TestRemovePrefix()
		{
			const string abcdef = "abcdef";
			const string qrs = "qrs";

			Assert.AreEqual("bcdef", abcdef.RemovePrefix('a', qrs));
			Assert.AreEqual(string.Empty, "a".RemovePrefix('a', qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix('c', qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix('x', qrs));
			Assert.AreEqual(qrs, string.Empty.RemovePrefix('a', qrs));
		}
	}
}
