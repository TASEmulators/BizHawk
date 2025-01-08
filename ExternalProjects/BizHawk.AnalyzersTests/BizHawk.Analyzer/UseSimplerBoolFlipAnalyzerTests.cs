namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.UseSimplerBoolFlipAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class UseSimplerBoolFlipAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfXORAssignment()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static int _z = default;
				private static bool _a = default;
				private static void CasesMethod() {
					_z ^= 0xA5A5;
					_a ^= DummyMethod();
					{|BHI1104:_a ^= true|};
					var b = false;
					{|BHI1104:b ^= true|};
					bool c = default;
					{|BHI1104:c ^= false|}; // this is effectively a no-op so there's no reason it would be used in the first place, but it was easier to flag both
					{|BHI1104:AnotherClass.GetInstance().Prop ^= true|}; // care needs to be taken with non-trivial expressions like this; a different message will be given
				}
				private static bool DummyMethod()
					=> default;
			}
			public sealed class AnotherClass {
				public static AnotherClass GetInstance()
					=> new();
				public bool Prop { get; set; }
				private AnotherClass() {}
			}
		""");
}
