namespace BizHawk.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefineCheckedOpAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_DEPRECATE = "consider marking this unchecked operator as [Obsolete]";

	private const string ERR_MSG_MAKE_CHECKED = "define a checked version of this operator";

	private static readonly DiagnosticDescriptor DiagDefineCheckedOp = new(
		id: "BHI1300",
		title: "Declare checked operators",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly Dictionary<string, string> UncheckedOps = new()
	{
		[WellKnownMemberNames.AdditionOperatorName] = WellKnownMemberNames.CheckedAdditionOperatorName,
		[WellKnownMemberNames.DecrementOperatorName] = WellKnownMemberNames.CheckedDecrementOperatorName,
		[WellKnownMemberNames.DivisionOperatorName] = WellKnownMemberNames.CheckedDivisionOperatorName,
		[WellKnownMemberNames.ExplicitConversionName] = WellKnownMemberNames.CheckedExplicitConversionName,
		[WellKnownMemberNames.IncrementOperatorName] = WellKnownMemberNames.CheckedIncrementOperatorName,
		[WellKnownMemberNames.MultiplyOperatorName] = WellKnownMemberNames.CheckedMultiplyOperatorName,
		[WellKnownMemberNames.SubtractionOperatorName] = WellKnownMemberNames.CheckedSubtractionOperatorName,
		[WellKnownMemberNames.UnaryNegationOperatorName] = WellKnownMemberNames.CheckedUnaryNegationOperatorName,
	};

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(/*HawkSourceAnalyzer.DiagWTF,*/ DiagDefineCheckedOp);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		INamedTypeSymbol? obsoleteAttrSym = null;
		context.RegisterSyntaxNodeAction(
			snac =>
			{
				obsoleteAttrSym ??= snac.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute")!;
				void FindCounterpartAndMaybeReport<T>(
					T declSyn,
					SyntaxToken operatorTkn,
					SyntaxList<AttributeListSyntax> attrLists,
					string checkedName,
					Func<T, string> getMethodName)
						where T : SyntaxNode
				{
					var hasCheckedCounterpart = ((TypeDeclarationSyntax) declSyn.Parent!).Members.OfType<T>() //TODO don't think this accounts for `partial` types
						.Any(syn => checkedName.Equals(getMethodName(syn), StringComparison.Ordinal));
					if (!hasCheckedCounterpart)
					{
						DiagDefineCheckedOp.ReportAt(operatorTkn, snac, ERR_MSG_MAKE_CHECKED);
					}
					else if (!attrLists.Matching(obsoleteAttrSym, snac).Any())
					{
						DiagDefineCheckedOp.ReportAt(operatorTkn, DiagnosticSeverity.Warning, snac, ERR_MSG_DEPRECATE);
					}
					// else usage is correct
				}
				switch (snac.Node)
				{
					case ConversionOperatorDeclarationSyntax cods:
						if (UncheckedOps.TryGetValue(cods.GetMethodName(), out var checkedName))
						{
							FindCounterpartAndMaybeReport(
								cods,
								cods.OperatorKeyword,
								cods.AttributeLists,
								checkedName,
								RoslynUtils.GetMethodName);
						}
						break;
					case OperatorDeclarationSyntax ods:
						if (UncheckedOps.TryGetValue(ods.GetMethodName(), out var checkedName1))
						{
							FindCounterpartAndMaybeReport(
								ods,
								ods.OperatorKeyword,
								ods.AttributeLists,
								checkedName1,
								RoslynUtils.GetMethodName);
						}
						break;
					default:
						HawkSourceAnalyzer.ReportWTF(snac.Node, snac, message: $"[{nameof(DefineCheckedOpAnalyzer)}] unexpected node kind {snac.Node.GetType().FullName}");
						break;
				}
			},
			SyntaxKind.ConversionOperatorDeclaration,
			SyntaxKind.OperatorDeclaration);
	}
}
