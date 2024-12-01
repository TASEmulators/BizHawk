namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TernaryInferredTypeMismatchAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagTernaryInferredTypeMismatch = new(
		id: "BHI1210",
		title: "Inferred type of branches of ternary expression in interpolation don't match",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagTernaryInferredTypeMismatch);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var objectSym = initContext.Compilation.GetTypeByMetadataName("System.Object")!;
			var stringSym = initContext.Compilation.GetTypeByMetadataName("System.String")!;
			initContext.RegisterOperationAction(oac =>
				{
					var ifelseOrTernaryOp = (IConditionalOperation) oac.Operation;
					if (ifelseOrTernaryOp.WhenFalse is null) return;
					var parent = ifelseOrTernaryOp.Parent!;
					if (parent.Kind is OperationKind.Conversion) parent = parent.Parent!;
					if (parent.Kind is not OperationKind.Interpolation) return;
					var ternaryOp = ifelseOrTernaryOp;
					var typeTernary = ternaryOp.Type!;
#if false // never hit; either both branches are string and there are no conversions, or conversions are necessary
					if (stringSym.Matches(typeTernary)) return;
#endif
					var lhs = ternaryOp.WhenTrue;
					var rhs = ternaryOp.WhenFalse;

					static IOperation TrimImplicitCast(IOperation op)
						=> op is IConversionOperation { Conversion.IsImplicit: true } implCastOp ? implCastOp.Operand : op;
					var typeLHS = TrimImplicitCast(lhs).Type!;
					var typeRHS = TrimImplicitCast(rhs).Type!;
					if (typeLHS.Matches(typeRHS)) return; // unnecessary conversion operators on each branch? seen with `? this : this`

					const string ERR_MSG_OBJECT = "missing ToString means ternary branches are upcast to object";
					var fatal = false;
					IOperation flaggedOp = ternaryOp;
					string message;
					if (stringSym.Matches(typeLHS))
					{
						flaggedOp = rhs;
						message = ERR_MSG_OBJECT;
					}
					else if (stringSym.Matches(typeRHS))
					{
						flaggedOp = lhs;
						message = ERR_MSG_OBJECT;
					}
					else if (objectSym.Matches(typeTernary))
					{
						fatal = true;
						message = "ternary branches are upcast to object! add ToString calls, or convert one to the other's type";
					}
					else
					{
						// if one's already an e.g. int literal, flag the e.g. char literal
						if (typeTernary.Matches(typeLHS)) flaggedOp = rhs;
						else if (typeTernary.Matches(typeRHS)) flaggedOp = lhs;
						message = $"ternary branches are converted to {typeTernary} before serialisation, possibly unintended";
					}
					oac.ReportDiagnostic(Diagnostic.Create(
						DiagTernaryInferredTypeMismatch,
						flaggedOp.Syntax.GetLocation(),
						fatal ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
						additionalLocations: null,
						properties: null,
						messageArgs: message));
				},
				OperationKind.Conditional);
		});
	}
}
