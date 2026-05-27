#pragma warning disable IDE0240 // just making sure NRTs are set up right for this file
#pragma warning disable IDE0241 // ditto
#nullable enable

using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Tests.Common.ReflectionExtensions
{
	[TestClass]
	public sealed class ReflectionExtensionTests
	{
		private static class Dummy
		{
#if false
#nullable disable
			public static readonly string Field_RefT_Ambig = default!;
#nullable restore

			public static readonly string Field_RefT_NonNull = default!;

			public static readonly string? Field_RefT_Nullable = default;

			public static readonly int Field_ValT_NonNull = default;

			public static readonly int? Field_ValT_Nullable = default;
#endif

#nullable disable
			public static string Method_RefT_Ambig(string arg)
				=> arg;
#nullable restore

			public static string Method_RefT_NonNull(string arg)
				=> arg;

			public static string? Method_RefT_Nullable(string? arg)
				=> arg;

			public static int Method_ValT_NonNull(int arg)
				=> arg;

			public static int? Method_ValT_Nullable(int? arg)
				=> arg;
		}

		private static readonly Type DummyClass = typeof(Dummy);

#if false
		[DataRow("RefT_Ambig", "null")]
		[DataRow("RefT_NonNull", "false")]
		[DataRow("RefT_Nullable", "true")]
		[DataRow("ValT_NonNull", "false")]
		[DataRow("ValT_Nullable", "true")]
		[TestMethod]
		public void TestFieldIsNRT(string label, string expectedStr)
		{
			bool? expected = bool.TryParse(expectedStr, out var b) ? b : null;
			Assert.AreEqual(expected, DummyClass.GetField($"Field_{label}").IsNRT());
		}
#endif

		[TestMethod]
		public void TestIsNullableT()
		{
			Assert.IsFalse(typeof(object).IsNullableT(), "object");
			Assert.IsFalse(typeof(string).IsNullableT(), "string");
			Assert.IsFalse(typeof(int[]).IsNullableT(), "int[]");
			Assert.IsFalse(typeof(ReflectionExtensionTests).IsNullableT(), "class ReflectionExtensionTests");
			Assert.IsFalse(DummyClass.IsNullableT(), "static class Dummy");
			Assert.IsFalse(typeof(ValueType).IsNullableT(), "ValueType");
			Assert.IsFalse(typeof(int).IsNullableT(), "int");
			Assert.IsFalse(typeof(Nullable<>).IsNullableT(), "unparameterised Nullable<>");
			Assert.IsTrue(typeof(int?).IsNullableT(), "Nullable<int>");
			Assert.IsTrue(typeof(Guid?).IsNullableT(), "Nullable<Guid>");
		}

		[DataRow("RefT_Ambig", "null")]
		[DataRow("RefT_NonNull", "false")]
		[DataRow("RefT_Nullable", "true")]
		[DataRow("ValT_NonNull", "false")]
		[DataRow("ValT_Nullable", "true")]
		[TestMethod]
		public void TestParamIsNRT(string label, string expectedStr)
		{
			bool? expected = bool.TryParse(expectedStr, out var b) ? b : null;
			Assert.AreEqual(expected, DummyClass.GetMethod($"Method_{label}").GetParameters()[0].IsNRTOrNullableT());
		}

#if false
		[DataRow("RefT_Ambig", "null")]
		[DataRow("RefT_NonNull", "false")]
		[DataRow("RefT_Nullable", "true")]
		[DataRow("ValT_NonNull", "false")]
		[DataRow("ValT_Nullable", "true")]
		[TestMethod]
		public void TestReturnIsNRT(string label, string expectedStr)
		{
			bool? expected = bool.TryParse(expectedStr, out var b) ? b : null;
			Assert.AreEqual(expected, DummyClass.GetMethod($"Method_{label}").ReturnTypeIsNRT());
		}
#endif
	}
}
