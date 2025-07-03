namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrokenCollectionExpressionAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagBrokenCollectionExpression = new(
		id: "BHI1234",
		title: "don't this",
		messageFormat: "don't this",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(/*HawkSourceAnalyzer.DiagWTF,*/ DiagBrokenCollectionExpression);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var arraySegmentSym = initContext.Compilation.GetTypeByMetadataName("System.ArraySegment`1")!;
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (ICollectionExpressionOperation) oac.Operation;
					if (arraySegmentSym.Matches(operation.Type.OriginalDefinition)) DiagBrokenCollectionExpression.ReportAt(operation, oac);
				},
				OperationKind.CollectionExpression);
		});
	}
}
