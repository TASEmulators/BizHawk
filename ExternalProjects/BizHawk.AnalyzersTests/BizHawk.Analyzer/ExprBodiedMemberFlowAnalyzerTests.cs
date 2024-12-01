namespace BizHawk.Tests.Analyzers;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.ExprBodiedMemberFlowAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class ExprBodiedMemberFlowAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfExpressionBodies()
		=> Verify.VerifyAnalyzerAsync("""
			public sealed class Cases {
				private int GetOnlyProp
					=> default;
				{|BHI1120:private int BadGetOnlyProp => default;|}
				private int GetSetProp {
					get => default;
					set => _ = value;
				}
				private int BadGetSetProp {
					{|BHI1120:get =>
						default;|}
					{|BHI1120:set
						=> _ = value;|}
				}
				private int GetInitProp {
					get => default;
					init => _ = value;
				}
				private int BadGetInitProp {
					{|BHI1120:get
						=> default;|}
					{|BHI1120:init =>
						_ = value;|}
				}
				private event System.EventHandler Event {
					add => DummyMethod();
					remove => _ = value;
				}
				private event System.EventHandler BadEvent {
					{|BHI1120:add =>
						DummyMethod();|}
					{|BHI1120:remove
						=> _ = value;|}
				}
				{|BHI1120:public Cases() => DummyMethod();|}
				{|BHI1120:~Cases() => DummyMethod();|}
				private int this[char good] {
					get => default;
					set => _ = value;
				}
				private int this[int bad] {
					{|BHI1120:get
						=> default;|}
					{|BHI1120:set =>
						_ = value;|}
				}
				private int ExprBodyMethod()
					=> default;
				{|BHI1120:private int BadExprBodyMethod() => default;|}
				private void DummyMethod() {
					int LocalMethod()
						=> default;
					{|BHI1120:int BadLocalMethod() => default;|}
				}
			}
			public sealed class GoodCtorDtor {
				public GoodCtorDtor()
					=> DummyMethod();
				~GoodCtorDtor()
					=> DummyMethod();
				private void DummyMethod() {}
			}
			namespace System.Runtime.CompilerServices {
				public static class IsExternalInit {} // this sample is compiled for lowest-common-denominator of `netstandard2.0`, so `init` accessor gives an error without this
			}
		""");
}
