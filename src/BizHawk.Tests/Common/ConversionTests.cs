using BizHawk.Common.NumberExtensions;

namespace BizHawk.Tests.Common;

[TestClass]
public class ConversionTests
{
	[TestMethod]
	[DataRow(0U, 0)]
	[DataRow(1U, 1.401298E-45F)]
	[DataRow(1109917696U, 42)]
	[DataRow(1123477881U, 123.456F)]
	[DataRow(3212836864U, -1)]
	public void TestReinterpretAsF32(uint input, float expected)
	{
		float converted = NumberExtensions.ReinterpretAsF32(input);

		Assert.AreEqual(expected, converted);

		uint restoredInput = NumberExtensions.ReinterpretAsUInt32(converted);

		Assert.AreEqual(input, restoredInput);
	}
}
