namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.UseNameofOperatorAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class UseNameofOperatorAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfNameofOperator()
		=> Verify.VerifyAnalyzerAsync("""
			public static class Cases {
				private static readonly int Z = typeof(Cases).FullName.Length;
				private static readonly int A = {|BHI1102:typeof(Cases).Name|}.Length;
				private static readonly int B = {|BHI1103:typeof(Cases).ToString|}().Length; // the diagnostic is added to the method group part (MemberAccessExpressionSyntax) and not the invoke part; won't matter in practice
				private static readonly int C = $"{{|BHI1103:typeof(Cases)|}}".Length;
				private static readonly int D = (">" + {|BHI1103:typeof(Cases)|}).Length;
				private static readonly int E = ({|BHI1103:typeof(Cases)|} + "<").Length;
			}
		""");
}
