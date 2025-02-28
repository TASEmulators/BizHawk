namespace BizHawk.Analyzers;

using System.Collections.Immutable;

/// <remarks>shoutouts to SimpleFlips</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseSimplerBoolFlipAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_SIMPLE = "Use e.g. `b = !b;` instead of `b ^= true;`";

	private const string ERR_MSG_COMPLEX = $"{ERR_MSG_SIMPLE} (you may want to store part of the expression in a local variable to avoid repeated side-effects or computation)";

	private static readonly DiagnosticDescriptor DiagUseSimplerBoolFlip = new(
		id: "BHI1104",
		title: "Don't use ^= (XOR-assign) for inverting the value of booleans",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(/*HawkSourceAnalyzer.DiagWTF,*/ DiagUseSimplerBoolFlip);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(
			oac =>
			{
				static bool IsZeroWorkLocalOrFieldRef(IOperation trunk)
				{
					while (trunk.Kind is OperationKind.FieldReference)
					{
						if (trunk.ChildOperations.Count is 0) return true; // in the unit test, the node(s) for the implicit `this.` are missing for some reason
						trunk = trunk.ChildOperations.First();
					}
					return trunk.Kind is OperationKind.InstanceReference or OperationKind.LocalReference;
				}
				var operation = (ICompoundAssignmentOperation) oac.Operation;
				if (operation.OperatorKind is not BinaryOperatorKind.ExclusiveOr) return;
				if (operation.Type?.SpecialType is not SpecialType.System_Boolean) return;
				if (operation.Value.Kind is not OperationKind.Literal) return;
				var lhsOp = operation.Target;
				bool lhsIsSimpleExpr;
				switch (lhsOp.Kind)
				{
					case OperationKind.PropertyReference:
						lhsIsSimpleExpr = false;
						break;
					case OperationKind.FieldReference:
						lhsIsSimpleExpr = IsZeroWorkLocalOrFieldRef(lhsOp);
						break;
					case OperationKind.LocalReference:
						lhsIsSimpleExpr = true;
						break;
					case OperationKind.ArrayElementReference:
						lhsIsSimpleExpr = false;
						break;
					default:
						HawkSourceAnalyzer.ReportWTF(operation, oac, message: $"[{nameof(UseSimplerBoolFlipAnalyzer)}] Left-hand side of XOR-assign was of an unexpected kind: {lhsOp.GetType().FullName}");
						return;
				}
				DiagUseSimplerBoolFlip.ReportAt(operation, isErrorSeverity: lhsIsSimpleExpr, oac, lhsIsSimpleExpr ? ERR_MSG_SIMPLE : ERR_MSG_COMPLEX);
			},
			OperationKind.CompoundAssignment);
	}
}
