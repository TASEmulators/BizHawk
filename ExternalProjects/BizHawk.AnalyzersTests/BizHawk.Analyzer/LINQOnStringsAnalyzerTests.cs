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
					=> {|BHI3102:str.Any()|};
				private static IEnumerable<char> AB(string str)
					=> {|BHI3102:str.Append('.')|};
				private static IEnumerable<char> AC(string str)
					=> {|BHI3102:str.Concat("-_-")|};
				private static IEnumerable<char> AD(string str)
					=> {|BHI3102:str.Concat(new[] { '-', '_', '-' })|};
				private static bool AE(string str)
					=> {|BHI3102:Enumerable.Contains(str, '.')|};
				private static int AF(string str)
					=> {|BHI3102:str.Count()|};
				private static IEnumerable<char> AG(string str)
					=> {|BHI3102:str.DefaultIfEmpty()|};
				private static IEnumerable<char> AH(string str)
					=> {|BHI3102:str.DefaultIfEmpty('.')|};
				private static char AI(string str)
					=> {|BHI3102:str.ElementAt(2)|};
				private static char AJ(string str)
					=> {|BHI3102:str.ElementAtOrDefault(2)|};
				private static char AK(string str)
					=> {|BHI3102:str.First()|};
				private static char AL(string str)
					=> {|BHI3102:str.FirstOrDefault()|};
				private static char AM(string str)
					=> {|BHI3102:str.Last()|};
				private static char AN(string str)
					=> {|BHI3102:str.LastOrDefault()|};
				private static long AO(string str)
					=> {|BHI3102:str.LongCount()|};
				private static IEnumerable<char> AP(string str)
					=> {|BHI3102:str.Prepend('.')|};
				private static IEnumerable<char> AQ(string str)
					=> {|BHI3102:str.Reverse()|};
				private static char AR(string str)
					=> {|BHI3102:str.Single()|};
				private static char AS(string str)
					=> {|BHI3102:str.SingleOrDefault()|};
				private static IEnumerable<char> AT(string str)
					=> {|BHI3102:str.Skip(2)|};
				private static IEnumerable<char> AU(string str)
					=> {|BHI3102:str.Take(2)|};
				private static char[] AV(string str)
					=> {|BHI3102:str.ToArray()|};
			}
			namespace BizHawk.Common.StringExtensions {
				public static class StringExtensions {} // Analyzer does more checks if this exists
			}
		""");
}
