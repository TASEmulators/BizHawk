namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.TryGetValueImplicitDiscardAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class TryGetValueImplicitDiscardAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfTryGetValue()
		=> Verify.VerifyAnalyzerAsync("""
			using System.Collections.Generic;
			public static class Cases {
				private static Dictionary<string, int> MakeDict()
					=> new();
				private sealed class CustomDict<K, V> {
					public bool TryGetValue(K key, out V val) {
						val = default;
						return false;
					}
				}
				private static void Z()
					=> _ = MakeDict().TryGetValue("z", out _);
				private static void A()
					=> {|BHI1200:MakeDict().TryGetValue("a", out _)|};
				private static void B()
					=> {|BHI1200:((IDictionary<string, int>) MakeDict()).TryGetValue("b", out _)|};
				private static void C()
					=> {|BHI1200:((IReadOnlyDictionary<string, int>) MakeDict()).TryGetValue("c", out _)|};
				private static void D()
					=> {|BHI1200:new CustomDict<string, int>().TryGetValue("d", out _)|};
			}
		""");
}
