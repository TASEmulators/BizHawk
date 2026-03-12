namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.UseTypeofOperatorAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class UseTypeofOperatorAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfTypeofOperator()
		=> Verify.VerifyAnalyzerAsync("""
			public class Parent {
				private string Z()
					=> 3.GetType().FullName;
				private string A()
					=> {|BHI1101:this.GetType()|}.FullName;
			}
			public sealed class Child: Parent {
				private string B()
					=> {|BHI1100:this.GetType()|}.FullName;
			}
			public readonly struct Struct {
				private string C()
					=> {|BHI1100:this.GetType()|}.FullName;
			}
		""");
}
