namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.FirstOrDefaultOnStructAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class FirstOrDefaultOnStructAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfFirstOrDefault()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Collections.Generic;
			using System.Linq;
			public static class Cases {
				private static string? Y()
					=> new[] { 0x80.ToString(), 0x20.ToString(), 0x40.ToString() }.FirstOrDefault(static s => s.Length > 2);
				private static string? Z()
					=> new List<int> { 0x80, 0x20, 0x40 }.Select(static n => n.ToString()).FirstOrDefault();
				private static int A()
					=> {|BHI3100:new[] { 0x80, 0x20, 0x40 }.FirstOrDefault()|};
				private static int B()
					=> {|BHI3100:new List<int> { 0x80, 0x20, 0x40 }.FirstOrDefault()|};
				private static int C()
					=> {|BHI3100:new[] { 0x80, 0x20, 0x40 }.FirstOrDefault(static n => n.ToString().Length > 2)|};
				private static int D()
					=> {|BHI3100:new List<int> { 0x80, 0x20, 0x40 }.FirstOrDefault(static n => n.ToString().Length > 2)|};
			}
			namespace BizHawk.Common.CollectionExtensions {
				public static class CollectionExtensions {} // Analyzer short-circuits if this doesn't exist, since that's where the extension lives
			}
		""");
}
