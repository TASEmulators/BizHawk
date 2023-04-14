namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FirstOrDefaultOnStructAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagUseFirstOrNull = new(
		id: "BHI3100",
		title: "Call to FirstOrDefault when elements are of a value type; FirstOrNull may have been intended",
		messageFormat: "Call to FirstOrDefault when elements are of a value type; did you mean FirstOrNull?",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagUseFirstOrNull);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			if (initContext.Compilation.GetTypeByMetadataName("BizHawk.Common.CollectionExtensions.CollectionExtensions") is null) return; // project does not have BizHawk.Common dependency
			var linqExtClassSym = initContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")!;
			IMethodSymbol? firstOrDefaultNoPredSym = null;
			IMethodSymbol? firstOrDefaultWithPredSym = null;
			foreach (var sym in linqExtClassSym.GetMembers("FirstOrDefault").Cast<IMethodSymbol>())
			{
				if (sym.Parameters.Length is 2) firstOrDefaultWithPredSym = sym;
				else firstOrDefaultNoPredSym = sym;
			}
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (IInvocationOperation) oac.Operation;
					var calledSym = operation.TargetMethod.ConstructedFrom;
					if (!(firstOrDefaultWithPredSym!.Matches(calledSym) || firstOrDefaultNoPredSym!.Matches(calledSym))) return;
					var receiverExprType = (INamedTypeSymbol) operation.SemanticModel!.GetTypeInfo((CSharpSyntaxNode) operation.Arguments[0].Syntax)!.ConvertedType!;
					if (receiverExprType.TypeArguments[0].IsValueType) oac.ReportDiagnostic(Diagnostic.Create(DiagUseFirstOrNull, operation.Syntax.GetLocation()));
				},
				OperationKind.Invocation);
		});
	}
}
