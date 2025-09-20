namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.LiteralExpectedAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class LiteralExpectedAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfExprPassedWhenLiteralExpected()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Diagnostics.CodeAnalysis;
			public static class Cases {
				private static void Do([LiteralExpected] long num = 2L) {}
				private static void X()
					=> Do();
				private static void YW([LiteralExpected] int good = 3)
					=> Do(good);
				private static void YX([LiteralExpected] long good = 8L)
					=> Do(good);
				private static void YY([LiteralExpected] int good)
					=> Do(good);
				private static void YZ([LiteralExpected] long good)
					=> Do(good);
				private static void ZY()
					=> Do(5);
				private static void ZZ()
					=> Do(10L);
				private static void AA(long bad)
					=> Do({|BHI3200:bad|});
				private static void AB(int bad)
					=> Do({|BHI3200:bad|});
				private static void AC(long bad = 4L)
					=> Do({|BHI3200:bad|});
				private static void AD(int bad = 4)
					=> Do({|BHI3200:bad|});
				private const long CONST_FIELD_S64 = 4L;
				private static void BA()
					=> Do({|BHI3200:CONST_FIELD_S64|});
				private const int CONST_FIELD_S32 = 4;
				private static void BB()
					=> Do({|BHI3200:CONST_FIELD_S32|});
				private static void CA() {
					const long CONST_LOCAL_S64 = 4L;
					Do({|BHI3200:CONST_LOCAL_S64|});
				}
				private static void CB() {
					const long CONST_LOCAL_S32 = 4L;
					Do({|BHI3200:CONST_LOCAL_S32|});
				}
			}
			namespace System.Diagnostics.CodeAnalysis {
				[AttributeUsage(AttributeTargets.Parameter)]
				public sealed class LiteralExpectedAttribute : Attribute {}
			}
		""");
}
