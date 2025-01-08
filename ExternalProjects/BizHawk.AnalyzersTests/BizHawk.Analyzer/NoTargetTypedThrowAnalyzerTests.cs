namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.NoTargetTypedThrowAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class NoTargetTypedThrowAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfTargetTypedNewForExceptions()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			public static class Cases {
				// not present: throwing a local, which is apparently necessary in Win32 interop
				private static void V()
					=> throw ExceptionHelperProp;
				private static void W()
					=> throw ExceptionHelperMethod();
				private static void X() {
					try {
						Z();
					} catch (Exception) {
						throw;
					}
				}
				private static void Y()
					=> throw new Exception();
				private static void Z()
					=> throw new NotImplementedException();
				private static void A()
					=> throw {|BHI1007:new()|};
				private static Exception ExceptionHelperMethod()
					=> new();
				private static Exception ExceptionHelperProp
					=> new();
			}
		""");
}
