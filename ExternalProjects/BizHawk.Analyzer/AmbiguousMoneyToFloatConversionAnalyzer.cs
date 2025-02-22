namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AmbiguousMoneyToFloatConversionAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagAmbiguousMoneyToFloatConversion = new(
		id: "BHI1105",
		title: "Use unambiguous decimal<=>float/double conversion methods",
		messageFormat: "use {0} for checked conversion, or {1} for unchecked",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagAmbiguousMoneyToFloatConversion);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(
			oac =>
			{
				var conversionOp = (IConversionOperation) oac.Operation;
				var typeOutput = conversionOp.Type?.SpecialType ?? SpecialType.None;
				var typeInput = conversionOp.Operand.Type?.SpecialType ?? SpecialType.None;
				bool isToDecimal;
				bool isDoublePrecision;
				if (typeOutput is SpecialType.System_Decimal)
				{
					if (typeInput is SpecialType.System_Double) isDoublePrecision = true;
					else if (typeInput is SpecialType.System_Single) isDoublePrecision = false;
					else return;
					isToDecimal = true;
				}
				else if (typeInput is SpecialType.System_Decimal)
				{
					if (typeOutput is SpecialType.System_Double) isDoublePrecision = true;
					else if (typeOutput is SpecialType.System_Single) isDoublePrecision = false;
					else return;
					isToDecimal = false;
				}
				else
				{
					return;
				}
				var conversionSyn = conversionOp.Syntax;
				//TODO check the suggested methods are accessible (i.e. BizHawk.Common is referenced)
				DiagAmbiguousMoneyToFloatConversion.ReportAt(
					conversionSyn.Parent?.Kind() is SyntaxKind.CheckedExpression or SyntaxKind.UncheckedExpression
						? conversionSyn.Parent
						: conversionSyn,
					isErrorSeverity: conversionOp.IsChecked,
					oac,
					messageArgs: isToDecimal
						? [
							$"new decimal({(isDoublePrecision ? "double" : "float")})", // "checked"
							"static NumberExtensions.ConvertToMoneyTruncated", // "unchecked"
						]
						: [
							$"decimal.{(isDoublePrecision ? "ConvertToF64" : "ConvertToF32")} ext. (from NumberExtensions)", // "checked"
							$"static Decimal.{(isDoublePrecision ? "ToDouble" : "ToSingle")}", // "unchecked"
						]);
			},
			OperationKind.Conversion);
	}
}
