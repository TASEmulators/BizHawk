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
				bool HasCheckedCounterpart<T>(SyntaxNode node, string checkedName, Func<T, string> getMethodName)
					=> ((TypeDeclarationSyntax) node.Parent!).Members.OfType<T>() //TODO don't think this accounts for `partial` types
						.Any(syn => checkedName.Equals(getMethodName(syn), StringComparison.Ordinal));
				void SuggestDeprecation(SyntaxNode node)
					=> DiagDefineCheckedOp.ReportAt(node, DiagnosticSeverity.Warning, snac, ERR_MSG_DEPRECATE);
				switch (snac.Node)
				{
					case ConversionOperatorDeclarationSyntax cods:
						if (UncheckedOps.TryGetValue(cods.GetMethodName(), out var checkedName))
						{
							var hasCheckedCounterpart = HasCheckedCounterpart<ConversionOperatorDeclarationSyntax>(
								cods,
								checkedName,
								RoslynUtils.GetMethodName);
							if (!hasCheckedCounterpart) DiagDefineCheckedOp.ReportAt(cods, snac, ERR_MSG_MAKE_CHECKED);
							else if (!cods.AttributeLists.Matching(obsoleteAttrSym, snac).Any()) SuggestDeprecation(cods);
							// else you did it good job
						}
						break;
					case OperatorDeclarationSyntax ods:
						if (UncheckedOps.TryGetValue(ods.GetMethodName(), out var checkedName1))
						{
							var hasCheckedCounterpart = HasCheckedCounterpart<OperatorDeclarationSyntax>(
								ods,
								checkedName1,
								RoslynUtils.GetMethodName);
							if (!hasCheckedCounterpart) DiagDefineCheckedOp.ReportAt(ods, snac, ERR_MSG_MAKE_CHECKED);
							else if (!ods.AttributeLists.Matching(obsoleteAttrSym, snac).Any()) SuggestDeprecation(ods);
							// else you did it good job
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
