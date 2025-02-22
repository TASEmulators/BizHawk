namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoTargetTypedThrowAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagNoTargetTypedThrow = new(
		id: "BHI1007",
		title: "Don't use target-typed new for throw expressions",
		messageFormat: "Specify `Exception` (or a more precise type) explicitly",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(/*HawkSourceAnalyzer.DiagWTF,*/ DiagNoTargetTypedThrow);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(
			oac =>
			{
				var exceptionOp = ((IThrowOperation) oac.Operation).Exception;
				if (exceptionOp is null) return; // re-`throw;`
				switch (exceptionOp.Kind)
				{
					case OperationKind.ObjectCreation:
					case OperationKind.Invocation:
					case OperationKind.PropertyReference:
					case OperationKind.LocalReference:
						return;
					case OperationKind.Conversion:
						if (((IConversionOperation) exceptionOp).Operand.Syntax
							.IsKind(SyntaxKind.ImplicitObjectCreationExpression))
						{
							break;
						}
						return;
					default:
						HawkSourceAnalyzer.ReportWTF(exceptionOp, oac, message: $"[{nameof(NoTargetTypedThrowAnalyzer)}] Argument to throw expression was of an unexpected kind: {exceptionOp.GetType().FullName}");
						return;
				}
				DiagNoTargetTypedThrow.ReportAt(exceptionOp, oac);
			},
			OperationKind.Throw);
	}
}
