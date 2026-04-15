using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Tests.Common.ReflectionExtensions
{
	[TestClass]
	public sealed class ReflectionExtensionTests
	{
		[TestMethod]
		public void TestIsNullableT()
		{
			Assert.IsFalse(typeof(object).IsNullableT(), "object");
			Assert.IsFalse(typeof(string).IsNullableT(), "string");
			Assert.IsFalse(typeof(int[]).IsNullableT(), "int[]");
			Assert.IsFalse(typeof(ReflectionExtensionTests).IsNullableT(), "class ReflectionExtensionTests");
			Assert.IsFalse(typeof(ValueType).IsNullableT(), "ValueType");
			Assert.IsFalse(typeof(int).IsNullableT(), "int");
			Assert.IsFalse(typeof(Nullable<>).IsNullableT(), "unparameterised Nullable<>");
			Assert.IsTrue(typeof(int?).IsNullableT(), "Nullable<int>");
			Assert.IsTrue(typeof(Guid?).IsNullableT(), "Nullable<Guid>");
		}
	}
}
