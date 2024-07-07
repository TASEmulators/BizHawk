namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.OrderBySelfAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class OrderBySelfAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfOrderBy()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Linq;
			using L = System.Collections.Generic.IEnumerable<int>;
			public static class Cases {
				private static readonly int[] Numbers = [ 0x80, 0x20, 0x40 ];
				private static L Y()
					=> Numbers.OrderBy(static delegate(int n) { return n.ToString().Length; });
				private static L Z()
					=> Numbers.OrderByDescending(static n => n.ToString().Length);
				private static L A()
					=> Numbers.OrderBy({|BHI3101:static delegate(int n) { return n; }|});
				private static L B()
					=> Numbers.OrderByDescending({|BHI3101:static delegate(int n) { return n; }|});
				private static L C()
					=> Numbers.OrderBy({|BHI3101:static (n) => n|});
				private static L D()
					=> Numbers.OrderByDescending({|BHI3101:static (n) => n|});
				private static L E()
					=> Numbers.OrderBy({|BHI3101:static n => n|});
				private static L F()
					=> Numbers.OrderByDescending({|BHI3101:static n => n|});
			}
			namespace BizHawk.Common.CollectionExtensions {
				public static class CollectionExtensions {} // Analyzer short-circuits if this doesn't exist, since that's where our backport lives
			}
		""");
}
