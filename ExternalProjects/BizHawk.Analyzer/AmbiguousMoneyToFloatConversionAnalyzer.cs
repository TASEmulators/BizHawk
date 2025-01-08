namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
		context.RegisterCompilationStartAction(initContext =>
		{
			var decimalSym = initContext.Compilation.GetTypeByMetadataName("System.Decimal")!;
			var doubleSym = initContext.Compilation.GetTypeByMetadataName("System.Double")!;
			var floatSym = initContext.Compilation.GetTypeByMetadataName("System.Single")!;
			initContext.RegisterOperationAction(oac =>
				{
					var conversionOp = (IConversionOperation) oac.Operation;
					var typeOutput = conversionOp.Type;
					var typeInput = conversionOp.Operand.Type;
					bool isToDecimal;
					bool isDoublePrecision;
					if (decimalSym.Matches(typeOutput))
					{
						if (doubleSym.Matches(typeInput)) isDoublePrecision = true;
						else if (floatSym.Matches(typeInput)) isDoublePrecision = false;
						else return;
						isToDecimal = true;
					}
					else if (decimalSym.Matches(typeInput))
					{
						if (doubleSym.Matches(typeOutput)) isDoublePrecision = true;
						else if (floatSym.Matches(typeOutput)) isDoublePrecision = false;
						else return;
						isToDecimal = false;
					}
					else
					{
						return;
					}
					var conversionSyn = conversionOp.Syntax;
					//TODO check the suggested methods are accessible (i.e. BizHawk.Common is referenced)
					oac.ReportDiagnostic(Diagnostic.Create(
						DiagAmbiguousMoneyToFloatConversion,
						(conversionSyn.Parent?.Kind() is SyntaxKind.CheckedExpression or SyntaxKind.UncheckedExpression
							? conversionSyn.Parent
							: conversionSyn).GetLocation(),
						conversionOp.IsChecked ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
						additionalLocations: null,
						properties: null,
						messageArgs: isToDecimal
							? [
								$"new decimal({(isDoublePrecision ? "double" : "float")})", // "checked"
								"static NumberExtensions.ConvertToMoneyTruncated", // "unchecked"
							]
							: [
								$"decimal.{(isDoublePrecision ? "ConvertToF64" : "ConvertToF32")} ext. (from NumberExtensions)", // "checked"
								$"static Decimal.{(isDoublePrecision ? "ToDouble" : "ToSingle")}", // "unchecked"
							]));
				},
				OperationKind.Conversion);
		});
	}
}
