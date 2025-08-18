namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.FeatureNotImplementedAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class FeatureNotImplementedAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfFeatureNotImplementedAttr()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			using BizHawk.Emulation.Common;
			public static class Cases {
				[FeatureNotImplemented] private static int X => throw new NotImplementedException();
				private static int Y {
					[FeatureNotImplemented] get => throw new NotImplementedException();
					[FeatureNotImplemented] set => throw new NotImplementedException();
				}
				[FeatureNotImplemented] private static int Z()
					=> throw new NotImplementedException();
				[FeatureNotImplemented] private static int A {|BHI3300:=> default|};
				private static int B {
					[FeatureNotImplemented] get {|BHI3300:=> default|};
					[FeatureNotImplemented] set {|BHI3300:=> _ = value|};
				}
				[FeatureNotImplemented] private static int C()
					{|BHI3300:=> default|};
				// wrong exception type, same code but different message:
				[FeatureNotImplemented] private static int D => {|BHI3300:throw new InvalidOperationException()|};
				private static int E {
					[FeatureNotImplemented] get => {|BHI3300:throw new InvalidOperationException()|};
					[FeatureNotImplemented] set => {|BHI3300:throw new InvalidOperationException()|};
				}
				[FeatureNotImplemented] private static int F()
					=> {|BHI3300:throw new InvalidOperationException()|};
				// same code but different message, since only the simplest of expected syntaxes is checked for:
				[FeatureNotImplemented] private static int G => {|BHI3300:throw (new NotImplementedException())|};
				private static int H {
					[FeatureNotImplemented] get => {|BHI3300:throw (new NotImplementedException())|};
					[FeatureNotImplemented] set => {|BHI3300:throw (new NotImplementedException())|};
				}
				[FeatureNotImplemented] private static int I()
					=> {|BHI3300:throw (new NotImplementedException())|};
				// the "wat" cases (at least the ones that are reachable in practice)
				{|BHI6660:[FeatureNotImplemented] private static int K {
					get => default;
					set => _ = value;
				}|}
			}
			namespace BizHawk.Emulation.Common {
				[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
				public sealed class FeatureNotImplementedAttribute: Attribute {}
			}
		""");
}
