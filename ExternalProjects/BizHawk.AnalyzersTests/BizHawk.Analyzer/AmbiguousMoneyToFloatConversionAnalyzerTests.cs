namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.AmbiguousMoneyToFloatConversionAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class AmbiguousMoneyToFloatConversionAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfDecimalExplicitCastOperators()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static float Y(decimal m)
					=> decimal.ToSingle(m);
				private static decimal Z(double d)
					=> new(d);
				private static float A(decimal m)
					=> {|BHI1105:unchecked((float) m)|};
				private static decimal B(double d)
					=> {|BHI1105:checked((decimal) d)|};
				private static decimal C(float d)
					=> {|BHI1105:unchecked((decimal) d)|};
				private static double D(decimal m)
					=> {|BHI1105:checked((double) m)|};
			}
		""");
}
