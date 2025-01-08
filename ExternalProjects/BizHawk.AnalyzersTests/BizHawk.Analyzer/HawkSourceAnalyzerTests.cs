namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.HawkSourceAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class HawkSourceAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfAnonymousClasses()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Linq;
			public static class Cases {
				private static int Z()
					=> new[] { 0x80, 0x20, 0x40 }
						.Select(static n => (N: n, StrLen: n.ToString().Length))
						.Where(static pair => pair.StrLen < 3)
						.Sum(static pair => pair.N - pair.StrLen);
				private static int A()
					=> new[] { 0x80, 0x20, 0x40 }
						.Select(static n => {|BHI1002:new { N = n, StrLen = n.ToString().Length }|})
						.Where(static pair => pair.StrLen < 3)
						.Sum(static pair => pair.N - pair.StrLen);
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfAnonymousDelegates()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Linq;
			public static class Cases {
				private static int Z()
					=> new[] { 0x80, 0x20, 0x40 }
						.Where(static n => n.ToString().Length < 3)
						.Sum();
				private static int A()
					=> new[] { 0x80, 0x20, 0x40 }
						.Where({|BHI1001:static delegate(int n) { return n.ToString().Length < 3; }|})
						.Sum();
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfDefaultSwitchBranches()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			public static class Cases {
				private static int Y(string s) {
					switch (s) {
						case "zero":
							return 0;
						case "one":
							return 1;
						default:
							throw new InvalidOperationException();
					}
				}
				private static int Z(string s)
					=> s switch {
						"zero" => 0,
						"one" => 1,
						_ => throw new InvalidOperationException()
					};
				private static int A(string s) {
					switch (s) {
						case "zero":
							return 0;
						case "one":
							return 1;
						default:
							throw new NotImplementedException(); //TODO checking switch blocks was never implemented in the Analyzer
					}
				}
				private static int B(string s)
					=> s switch {
						"zero" => 0,
						"one" => 1,
						_ => {|BHI1005:throw new NotImplementedException()|}
					};
				private static int C(string s)
					=> s switch {
						"zero" => 0,
						"one" => 1,
						_ => {|BHI1005:throw (new NotImplementedException())|} // same code but different message, since only the simplest of expected syntaxes is checked for
					};
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfDiscards()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static void Z() {
					_ = string.Empty;
				}
				private static void A() {
					var s = string.Empty;
					{|BHI1006:_ = s|};
				}
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfInterpolatedString()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static readonly int Z = $@"{0x100}".Length;
				private static readonly int A = {|BHI1004:@$"{0x100}"|}.Length;
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfListSyntaxes()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static readonly int[] V = [ ];
				private static readonly bool W = V is [ ];
				private static readonly int[] X = W ? [ ] : V;
				private static readonly int[] Y = [ 0x80, 0x20, 0x40 ];
				private static readonly bool Z = Y is [ _, > 20, .. ];
				private static readonly int[] A = {|BHI1110:[0x80, 0x20, 0x40 ]|};
				private static readonly bool B = A is {|BHI1110:[ _, > 20, ..]|};
				private static readonly bool C = A is {|BHI1110:[_, > 20, ..]|};
				private static readonly int[] D = {|BHI1110:[]|};
				private static readonly bool E = D is {|BHI1110:[]|};
				private static readonly int[] F = E ? {|BHI1110:[]|} : D;
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfRecordDeclKeywords()
		=> Verify.VerifyAnalyzerAsync("""
			internal record struct Y {}
			internal record class Z {}
			{|BHI1130:internal record A {}|}
		""");

	[TestMethod]
	public Task CheckMisuseOfQuerySyntax()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Linq;
			using L = System.Collections.Generic.IEnumerable<(int N, int StrLen)>;
			public static class Cases {
				private static L Z()
					=> new[] { 0x80, 0x20, 0x40 }
						.Select(static n => (N: n, StrLen: n.ToString().Length))
						.OrderBy(static pair => pair.StrLen);
				private static L A()
					=> {|BHI1003:from n in new[] { 0x80, 0x20, 0x40 }
						let pair = (N: n, StrLen: n.ToString().Length)
						orderby pair.StrLen
						select pair|};
			}
		""");
}
