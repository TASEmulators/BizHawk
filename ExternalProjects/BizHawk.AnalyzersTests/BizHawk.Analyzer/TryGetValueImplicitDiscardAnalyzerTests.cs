namespace BizHawk.Tests.Analyzers;

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
					=> MakeDict(){|BHI1200:.TryGetValue("a", out _)|};
				private static void B()
					=> ((IDictionary<string, int>) MakeDict()){|BHI1200:.TryGetValue("b", out _)|};
				private static void C()
					=> ((IReadOnlyDictionary<string, int>) MakeDict()){|BHI1200:.TryGetValue("c", out _)|};
				private static void D()
					=> new CustomDict<string, int>(){|BHI1200:.TryGetValue("d", out _)|};
			}
		""");
}
