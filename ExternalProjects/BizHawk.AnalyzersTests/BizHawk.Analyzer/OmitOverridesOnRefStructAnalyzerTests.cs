namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.OmitOverridesOnRefStructAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class OmitOverridesOnRefStructAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfOverridesOnRefStruct()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			public readonly ref struct TestStruct2 { // you should definitely not be doing this, but that's a matter for another Analyzer
				public readonly new bool Equals(object? other)
					=> false;
				public readonly new int GetHashCode()
					=> throw new NotImplementedException();
			}
			public readonly ref struct TestStruct {
				public readonly bool Equals(TestStruct other)
					=> throw new NotImplementedException();
				public readonly override string ToString()
					=> throw new NotImplementedException();
				public readonly void DoNothing() {}

				{|BHI1009:public readonly override bool Equals(object? other)
					=> false;|}
				{|BHI1009:public readonly override int GetHashCode()
					=> throw new NotImplementedException();|}
			}
		""");
}
