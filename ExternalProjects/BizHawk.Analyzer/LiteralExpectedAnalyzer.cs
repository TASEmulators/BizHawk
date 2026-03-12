namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LiteralExpectedAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagLiteralExpected = new(
		id: "BHI3200",
		title: "An inline literal value is expected for the parameter",
		messageFormat: "Pass a literal value for {0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(HawkSourceAnalyzer.DiagWTF, DiagLiteralExpected);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var literalExpectedAttrSym = initContext.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.LiteralExpectedAttribute");
			if (literalExpectedAttrSym is null) return;
			bool ExpectsLiteral(IParameterSymbol paramSym)
				=> paramSym.GetAttributes().Any(ad => literalExpectedAttrSym.Matches(ad.AttributeClass));
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (IArgumentOperation) oac.Operation;
					if (operation.ArgumentKind is ArgumentKind.DefaultValue) return;
					var paramSym = operation.Parameter;
					if (paramSym is null) return;
					if (!ExpectsLiteral(paramSym)) return;
					var innerOp = operation.Value;
					if (innerOp is IConversionOperation convOp) innerOp = convOp.Operand;
					DiagnosticSeverity severity;
					if (innerOp.ConstantValue.HasValue) switch (innerOp)
					{
						case IFieldReferenceOperation: // `const` field, still bad
							severity = DiagnosticSeverity.Error;
							break;
						case ILiteralOperation: // good
							return;
						case ILocalReferenceOperation: // `const` local, less bad
							severity = DiagnosticSeverity.Warning;
							break;
						default:
							HawkSourceAnalyzer.ReportWTF(operation, oac, $"[{nameof(LiteralExpectedAnalyzer)}] Method argument was of an unexpected kind: {innerOp.GetType().FullName}");
							return;
					}
					else if (innerOp is IParameterReferenceOperation pro && ExpectsLiteral(pro.Parameter)) return; // transitively good
					else severity = DiagnosticSeverity.Error; // reference to non-`[LiteralExpected]` param, or to local, field, etc.
					DiagLiteralExpected.ReportAt(operation, severity, oac, paramSym.Name);
				},
				OperationKind.Argument);
		});
	}
}
