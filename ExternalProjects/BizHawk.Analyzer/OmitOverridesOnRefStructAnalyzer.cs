namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OmitOverridesOnRefStructAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagOmitEqualsOrHashCode = new(
		id: "BHI1009",
		title: "ref struct should not override ValueType.{Equals,GetHashCode}",
		messageFormat: "Omit override for {0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(DiagOmitEqualsOrHashCode);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(
			snac =>
			{
				var declSyn = (BaseMethodDeclarationSyntax) snac.Node;
				if (snac.SemanticModel.GetDeclaredSymbol(declSyn, snac.CancellationToken) is {
					ContainingType.IsRefLikeType: true,
					OverriddenMethod.ContainingType.SpecialType: SpecialType.System_ValueType,
					Name: var methodName,
				} && methodName is nameof(ValueType.Equals) or nameof(ValueType.GetHashCode))
				{
					DiagOmitEqualsOrHashCode.ReportAt(declSyn, snac, methodName);
				}
			},
			SyntaxKind.MethodDeclaration);
	}
}
