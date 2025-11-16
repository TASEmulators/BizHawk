namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoNullEventArgsAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_NO_NULL_EVENTARGS = "Use EventArgs.Empty, not null";

	private static readonly DiagnosticDescriptor DiagNoNullEventArgs = new(
		id: "BHI1090",
		title: ERR_MSG_NO_NULL_EVENTARGS,
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(DiagNoNullEventArgs);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var eventArgsClassSym = initContext.Compilation.GetTypeByMetadataName(typeof(EventArgs).FullName!)!;
			initContext.RegisterOperationAction(
				oac =>
				{
					var conversionOp = (IConversionOperation) oac.Operation;
					if (conversionOp.ConstantValue is not { HasValue: true, Value: null }) return;
					var outputType = conversionOp.Type;
					if (eventArgsClassSym.Matches(outputType)) DiagNoNullEventArgs.ReportAt(conversionOp, isErrorSeverity: true, oac, ERR_MSG_NO_NULL_EVENTARGS);
					else if (eventArgsClassSym.IsAssignableFrom(outputType)) DiagNoNullEventArgs.ReportAt(conversionOp, isErrorSeverity: false, oac, "Use {outputType.Name}.Empty (if it exists) or construct a new instance");
				},
				OperationKind.Conversion);
		});
	}
}
