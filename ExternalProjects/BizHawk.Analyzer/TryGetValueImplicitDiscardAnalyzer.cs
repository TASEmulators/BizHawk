namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryGetValueImplicitDiscardAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagUncheckedTryGetValue = new(
		id: "BHI1200",
		title: "Check result of IDictionary.TryGetValue, or discard it if default(T) is desired",
		messageFormat: "Assign the result of this TryGetValue call to a variable or discard",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagUncheckedTryGetValue);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static initContext =>
		{
			const string STR_TGV = "TryGetValue";
			var rwDictSym = initContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2")!;
			var roDictSym = initContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2")!;
			bool IsBCLTryGetValue(IMethodSymbol calledSym)
				=> calledSym.ContainingType.AllInterfaces.Any(intfSym =>
					{
						var degenericisedSym = intfSym.OriginalDefinition;
						return rwDictSym.Matches(degenericisedSym) || roDictSym.Matches(degenericisedSym);
					});
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (IInvocationOperation) oac.Operation;
					if (operation.Parent?.Kind is not OperationKind.ExpressionStatement) return;
					var calledSym = operation.TargetMethod.ConstructedFrom;
					if (calledSym.Name is STR_TGV) oac.ReportDiagnostic(Diagnostic.Create(
						DiagUncheckedTryGetValue,
						operation.Syntax.GetLocation(),
						IsBCLTryGetValue(calledSym) ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
						additionalLocations: null,
						properties: null,
						messageArgs: null));
				},
				OperationKind.Invocation);
		});
	}
}
