namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseSpanIsEmptyAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_USE_ISEMPTY = "Use IsEmpty instead of checking Length";

	private static readonly DiagnosticDescriptor DiagUseSpanIsEmpty = new(
		id: "BHI3103",
		title: ERR_MSG_USE_ISEMPTY,
		messageFormat: ERR_MSG_USE_ISEMPTY,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(DiagUseSpanIsEmpty);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var roSpanSym = initContext.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
			var spanSym = initContext.Compilation.GetTypeByMetadataName("System.Span`1");
			if (roSpanSym is null || spanSym is null) return;
			var roSpanLengthSym = roSpanSym.GetMembers().OfType<IPropertySymbol>().First(static sym => sym.Name is "Length");
			var spanLengthSym = spanSym.GetMembers().OfType<IPropertySymbol>().First(static sym => sym.Name is "Length");
			initContext.RegisterOperationAction(
				oac =>
				{
					static bool TryGetConstExprValue(IOperation op, out Optional<object?> value)
					{
						switch (op)
						{
							case IFieldReferenceOperation fieldRefOp:
								value = fieldRefOp.ConstantValue;
								return true;
							case ILiteralOperation literalOp:
								value = literalOp.ConstantValue;
								return true;
							default: // property, method call, etc.
								value = default;
								return false;
						}
					}
					var propRefOp = (IPropertyReferenceOperation) oac.Operation;
					var propSym = propRefOp.Property.OriginalDefinition;
					if (!roSpanLengthSym.Matches(propSym) && !spanLengthSym.Matches(propSym)) return;
					var lengthCheckOp = propRefOp.Parent;
					BinaryOperatorKind binaryOpKind;
					Optional<object?> maybeConstExpr;
					switch (lengthCheckOp)
					{
						case IBinaryOperation binaryOp:
							binaryOpKind = binaryOp.OperatorKind;
							if (binaryOpKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals
								or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual
								or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.GreaterThan))
							{
								return;
							}
							var sentinelOp = binaryOp.RightOperand;
							if (sentinelOp == propRefOp) // flipped
							{
								sentinelOp = binaryOp.LeftOperand;
								binaryOpKind = binaryOpKind switch
								{
									BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan,
									BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
									BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
									BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan,
									_ => binaryOpKind,
								};
							}
							if (!TryGetConstExprValue(sentinelOp, out maybeConstExpr)) return;
							break;
						case IIsPatternOperation { Pattern: var pattern } patternOp:
							var negated = false;
							if (pattern is INegatedPatternOperation negPatternOp)
							{
								negated = true;
								pattern = negPatternOp.Pattern;
							}
							switch (pattern)
							{
								case IConstantPatternOperation eqPatternOp:
									if (!TryGetConstExprValue(eqPatternOp.Value, out maybeConstExpr)) return;
									binaryOpKind = BinaryOperatorKind.Equals;
									break;
								case IRelationalPatternOperation cmpPatternOp:
									if (!TryGetConstExprValue(cmpPatternOp.Value, out maybeConstExpr)) return;
									binaryOpKind = cmpPatternOp.OperatorKind;
									break;
								default: // type check or something
									return;
							}
							if (negated) binaryOpKind = binaryOpKind switch
							{
								BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals,
								BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals,
								BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
								BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
								BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
								BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
								_ => binaryOpKind,
							};
							break;
						default: // anything else
							return;
					}
					if ((maybeConstExpr.Value is int i ? i : null)
						== (binaryOpKind is BinaryOperatorKind.LessThan or BinaryOperatorKind.GreaterThanOrEqual ? 1 : 0))
					{
						DiagUseSpanIsEmpty.ReportAt(lengthCheckOp, oac);
					}
				},
				OperationKind.PropertyReference);
		});
	}
}
