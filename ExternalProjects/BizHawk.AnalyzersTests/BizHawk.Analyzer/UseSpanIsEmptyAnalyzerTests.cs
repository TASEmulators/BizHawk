namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.UseSpanIsEmptyAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class UseSpanIsEmptyAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfUseSpanLength()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			public static class Cases {
				private const int ZERO = 0;
				private static bool YZ(SpanPair<int, bool> pair)
					=> pair is { First.Length: 0, Second: [ .., true ] }; // allowed as part of a larger pattern matching exprssion (`IsEmpty: true` works, but is longer and IMO worse)
				private static bool ZX(ReadOnlySpan<int> span)
					=> span.Length is 3;
				private static bool ZY(Span<int> span)
					=> span.Length is >= 2;
				private static bool ZZ(ReadOnlySpan<int> span)
					=> span.Length > 1;
				private static bool AA(Span<int> span)
					=> {|BHI3103:span.Length is 0|};
				private static bool AB(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length == 0|};
				private static bool AC(Span<int> span)
					=> {|BHI3103:0 == span.Length|};
				private static bool AD(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length is ZERO|};
				private static bool AE(Span<int> span)
					=> {|BHI3103:span.Length == ZERO|};
				private static bool AF(ReadOnlySpan<int> span)
					=> {|BHI3103:ZERO == span.Length|};
				private static bool BA(Span<int> span)
					=> {|BHI3103:span.Length is not 0|};
				private static bool BB(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length != 0|};
				private static bool BC(Span<int> span)
					=> {|BHI3103:0 != span.Length|};
				private static bool CA(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length is > 0|};
				private static bool CB(Span<int> span)
					=> {|BHI3103:span.Length > 0|};
				private static bool CC(ReadOnlySpan<int> span)
					=> {|BHI3103:0 < span.Length|};
				private static bool CZ(Span<int> span)
					=> {|BHI3103:span.Length is not <= 0|}; // not going to check every variation of this; another Analyzer can flag this dumb construction
				private static bool DA(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length is >= 1|};
				private static bool DB(Span<int> span)
					=> {|BHI3103:span.Length >= 1|};
				private static bool DC(ReadOnlySpan<int> span)
					=> {|BHI3103:1 <= span.Length|};
				private static bool EA(Span<int> span)
					=> {|BHI3103:span.Length is <= 0|};
				private static bool EB(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length <= 0|};
				private static bool EC(Span<int> span)
					=> {|BHI3103:0 >= span.Length|};
				private static bool FA(ReadOnlySpan<int> span)
					=> {|BHI3103:span.Length is < 1|};
				private static bool FB(Span<int> span)
					=> {|BHI3103:span.Length < 1|};
				private static bool FC(ReadOnlySpan<int> span)
					=> {|BHI3103:1 > span.Length|};
			}
			public ref struct SpanPair<TFirst, TSecond> {
				public readonly Span<TFirst> First;
				public readonly Span<TSecond> Second;
				public SpanPair(Span<TFirst> first, Span<TSecond> second) {
					First = first;
					Second = second;
				}
			}
		""");
}
