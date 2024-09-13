namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.TernaryInferredTypeMismatchAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class TernaryInferredTypeMismatchAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfTernariesInInterpolations()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Collections.Generic;
			using System.Diagnostics;
			public static class Cases {
				private static string X(bool cond)
					=> $"{(cond ? 'p'.ToString() : 9.ToString())}";
				private static string Y(bool cond)
					=> $"{(cond ? "p" : 9.ToString())}";
				private static string Z(bool cond)
					=> $"{(cond ? "p" : "q")}";
				private static string A(bool cond)
					=> $"{(cond ? "p" : {|BHI1210:9|})}";
				private static string B(bool cond)
					=> $"{(cond ? {|BHI1210:'p'|} : 9)}";
				private static string C(bool cond, Process p, Queue<int> q)
					=> $"{({|BHI1210:cond ? p : q|})}";
			}
		""");
}
