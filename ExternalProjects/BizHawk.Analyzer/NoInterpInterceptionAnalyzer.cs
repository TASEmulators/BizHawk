namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoInterpInterceptionAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagNoInterpInterception = new(
		id: "BHI1112",
		title: "Don't use string interpolation which desugars to `DefaultInterpolatedStringHandler` where that's known to fail at runtime (`Program..cctor`)",
		messageFormat: "String interpolation is not available here",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(HawkSourceAnalyzer.DiagWTF, DiagNoInterpInterception);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var noInterpInterceptionAttrSym = initContext.Compilation.GetTypeByMetadataName("BizHawk.Common.NoInterpInterceptionAttribute");
			if (noInterpInterceptionAttrSym is null) return; // project does not have BizHawk.Common dependency
			initContext.RegisterOperationAction(
				oac =>
				{
					var op = oac.Operation;
					if (op.Syntax is not CSharpSyntaxNode interpSyn) return;
					var methodAttrs = interpSyn.NearestAncestorOfType<LocalFunctionStatementSyntax>()?.AttributeLists
						?? interpSyn.NearestAncestorOfType<BaseMethodDeclarationSyntax>()?.AttributeLists; // ugly
					if (methodAttrs is null || !methodAttrs.Value.Matching(noInterpInterceptionAttrSym, oac).Any()) return;
					DiagNoInterpInterception.ReportAt(op, oac);
				},
				OperationKind.InterpolatedString);
		});
	}
}
