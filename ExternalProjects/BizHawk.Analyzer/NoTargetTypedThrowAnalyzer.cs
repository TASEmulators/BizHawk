namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoTargetTypedThrowAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_IMPLICIT = "Specify `Exception` (or a more precise type) explicitly";

	private static readonly DiagnosticDescriptor DiagNoTargetTypedThrow = new(
		id: "BHI1007",
		title: "Don't use target-typed new for throw expressions",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagNoTargetTypedThrow);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(
			oac =>
			{
				var exceptionOp = ((IThrowOperation) oac.Operation).Exception;
				if (exceptionOp is null) return; // re-`throw;`
				void Fail(string message)
					=> oac.ReportDiagnostic(Diagnostic.Create(DiagNoTargetTypedThrow, exceptionOp.Syntax.GetLocation(), message));
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
						Fail($"Argument to throw expression was of an unexpected kind: {exceptionOp.GetType().FullName}");
						return;
				}
				Fail(ERR_MSG_IMPLICIT);
			},
			OperationKind.Throw);
	}
}
