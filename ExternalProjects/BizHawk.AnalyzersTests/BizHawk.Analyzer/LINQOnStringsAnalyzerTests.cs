namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.LINQOnStringsAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class LINQOnStringsAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfLINQOnStrings()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Collections.Generic;
			using System.Linq;
			public static class Cases {
				private static bool DummyPredicate(char c)
					=> c is >= 'a' and <= 'z';
				private static char ZQ(string str)
					=> str.SingleOrDefault(DummyPredicate);
				private static char ZR(string str)
					=> str.Single(DummyPredicate);
				private static long ZS(string str)
					=> str.LongCount(DummyPredicate);
				private static char ZT(string str)
					=> str.LastOrDefault(DummyPredicate);
				private static char ZU(string str)
					=> str.Last(DummyPredicate);
				private static char ZV(string str)
					=> str.FirstOrDefault(DummyPredicate);
				private static char ZW(string str)
					=> str.First(DummyPredicate);
				private static int ZX(string str)
					=> str.Count(DummyPredicate);
				private static bool ZY(string str)
					=> str.Any(DummyPredicate);
				private static bool ZZ(string str)
					=> str.All(DummyPredicate);
				private static bool AA(string str)
					=> str{|BHI3102:.Any()|};
				private static IEnumerable<char> AB(string str)
					=> str{|BHI3102:.Append('.')|};
				private static IEnumerable<char> AC(string str)
					=> str{|BHI3102:.Concat("-_-")|};
				private static IEnumerable<char> AD(string str)
					=> str{|BHI3102:.Concat(new[] { '-', '_', '-' })|};
				private static bool AE(string str)
					=> Enumerable{|BHI3102:.Contains(str, '.')|};
				private static int AF(string str)
					=> str{|BHI3102:.Count()|};
				private static IEnumerable<char> AG(string str)
					=> str{|BHI3102:.DefaultIfEmpty()|};
				private static IEnumerable<char> AH(string str)
					=> str{|BHI3102:.DefaultIfEmpty('.')|};
				private static char AI(string str)
					=> str{|BHI3102:.ElementAt(2)|};
				private static char AJ(string str)
					=> str{|BHI3102:.ElementAtOrDefault(2)|};
				private static char AK(string str)
					=> str{|BHI3102:.First()|};
				private static char AL(string str)
					=> str{|BHI3102:.FirstOrDefault()|};
				private static char AM(string str)
					=> str{|BHI3102:.Last()|};
				private static char AN(string str)
					=> str{|BHI3102:.LastOrDefault()|};
				private static long AO(string str)
					=> str{|BHI3102:.LongCount()|};
				private static IEnumerable<char> AP(string str)
					=> str{|BHI3102:.Prepend('.')|};
				private static IEnumerable<char> AQ(string str)
					=> str{|BHI3102:.Reverse()|};
				private static char AR(string str)
					=> str{|BHI3102:.Single()|};
				private static char AS(string str)
					=> str{|BHI3102:.SingleOrDefault()|};
				private static IEnumerable<char> AT(string str)
					=> str{|BHI3102:.Skip(2)|};
				private static IEnumerable<char> AU(string str)
					=> str{|BHI3102:.Take(2)|};
				private static char[] AV(string str)
					=> str{|BHI3102:.ToArray()|};
			}
			namespace BizHawk.Common.StringExtensions {
				public static class StringExtensions {} // Analyzer does more checks if this exists
			}
		""");
}
