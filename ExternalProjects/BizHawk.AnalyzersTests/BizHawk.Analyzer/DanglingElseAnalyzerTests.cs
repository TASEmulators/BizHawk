namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.DanglingElseAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class DanglingElseAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfDanglingElse()
		=> Verify.VerifyAnalyzerAsync("""
			using static System.Console;
			public static class Cases {
				private static void Y(bool b, bool b1, bool b2, bool b3) {
					// ugly, but this parses unambiguously; another Analyzer can flag the indentation
					/*0*/if (b)
						/*1*/if (b1)
							WriteLine("Y");
					/*1*/else if (b2)
						WriteLine("X");
					/*1*/else
						WriteLine("Z");
					/*0*/else if (b3)
						WriteLine("Z");
				}
				private static void Z(bool b, bool b1) {
					/*0*/if (b)
						/*1*/if (b1)
							WriteLine("Y");
						/*1*/else
							WriteLine("Z");
					/*0*/else
						WriteLine("X");
				}
				private static void A(bool b, bool b1) {
					/*0*/{|BHI1121:if|} (b)
						/*1*/if (b1)
							WriteLine("Y");
					/*1*/else
						WriteLine("X");
				}
				private static void B(bool b, bool b1, bool b2) {
					/*0*/{|BHI1121:{|BHI1121:if|}|} (b)
						/*1*/if (b1)
							WriteLine("Y");
						/*1*/else if (b2)
							WriteLine("Z");
					/*1*/else
						WriteLine("X");
				}
				private static void C(bool b, bool b1, bool b2) {
					/*0*/{|BHI1121:{|BHI1121:if|}|} (b)
						/*1*/if (b1)
							WriteLine("Y");
					/*1*/else if (b2)
						WriteLine("Z");
					/*1*/else
						WriteLine("X");
				}
				private static void D(bool b, bool b1, bool b2) {
					/*0*/{|BHI1121:if|} (b)
						/*1*/if (b1)
							WriteLine("Y");
					/*1*/else if (b2)
						WriteLine("X");
				}
			}
		""");
}
