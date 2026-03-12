using NE = BizHawk.Common.NumberExtensions.NumberExtensions;

namespace BizHawk.Tests.Common.NumberExtensions
{
	[TestClass]
	public class StringExtensionTests
	{
		[DataRow(int.MinValue, -1)]
		[DataRow(-1, -1)]
		[DataRow(0, -1)]
		[DataRow(1, 0)]
		[DataRow(2, 0)]
		[DataRow(9, 0)]
		[DataRow(10, 1)]
		[DataRow(11, 1)]
		[DataRow(99, 1)]
		[DataRow(100, 2)]
		[DataRow(101, 2)]
		[DataRow(999, 2)]
		[DataRow(1000, 3)]
		[DataRow(1001, 3)]
		[DataRow(9999, 3)]
		[DataRow(10000, 4)]
		[DataRow(10001, 4)]
		[TestMethod]
		public void TestLog10(int input, int expected)
		{
			var actual = NE.Log10(input);
			Assert.AreEqual(expected, actual, "should match docs");
			if (expected >= 0) Assert.AreEqual(unchecked((int) Math.Log10(input)), actual, "should match BCL");
		}
	}
}
