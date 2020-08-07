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
	}
}
