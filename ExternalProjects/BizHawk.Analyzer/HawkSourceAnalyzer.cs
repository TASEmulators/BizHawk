namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HawkSourceAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_LIST_EXPR_EMPTY = "Empty collection expression should be `[ ]`";

	private const string ERR_MSG_LIST_EXPR_END = "Collection expression should end with ` ]`";

	private const string ERR_MSG_LIST_EXPR_START = "Collection expression should start with `[ `";

	private const string ERR_MSG_SWITCH_THROWS_UNKNOWN = "Indeterminable exception type in default switch branch, should be InvalidOperationException/SwitchExpressionException";

	private const string ERR_MSG_SWITCH_THROWS_WRONG_TYPE = "Incorrect exception type in default switch branch, should be InvalidOperationException/SwitchExpressionException";

	private static readonly DiagnosticDescriptor DiagInterpStringIsDollarAt = new(
		id: "BHI1004",
		title: "Verbatim interpolated strings should begin $@, not @$",
		messageFormat: "Swap @ and $ on interpolated string",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagListExprSpacing = new(
		id: "BHI1110",
		title: "Brackets of collection expression should be separated with spaces",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagNoAnonClasses = new(
		id: "BHI1002",
		title: "Do not use anonymous types (classes)",
		messageFormat: "Replace anonymous class with tuple or explicit type",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagNoAnonDelegates = new(
		id: "BHI1001",
		title: "Do not use anonymous delegates",
		messageFormat: "Replace anonymous delegate with lambda or local method",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagNoDiscardingLocals = new(
		id: "BHI1006",
		title: "Do not discard local variables",
		messageFormat: "RHS is a local, so this discard has no effect, and is at best unhelpful for humans",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagNoQueryExpression = new(
		id: "BHI1003",
		title: "Do not use query expression syntax",
		messageFormat: "Use method chain for LINQ instead of query expression syntax",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagRecordImplicitlyRefType = new(
		id: "BHI1130",
		title: "Record type declaration missing class (or struct) keyword",
		messageFormat: "Add class (or struct) keyword",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagSwitchShouldThrowIOE = new(
		id: "BHI1005",
		title: "Default branch of switch expression should throw InvalidOperationException/SwitchExpressionException or not throw",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DiagWTF = new(
		id: "BHI6660",
		title: "BizHawk.Analyzer ran into syntax which it doesn't understand/support",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

#if true
	public static OperationCanceledException ReportWTF(IOperation location, OperationAnalysisContext ctx, string message)
	{
		DiagWTF.ReportAt(location, ctx, message);
		return new(ctx.CancellationToken);
	}

	public static OperationCanceledException ReportWTF(SyntaxNode location, OperationAnalysisContext ctx, string message)
	{
		DiagWTF.ReportAt(location, ctx, message);
		return new(ctx.CancellationToken);
	}

	public static OperationCanceledException ReportWTF(SyntaxNode location, SyntaxNodeAnalysisContext ctx, string message)
	{
		DiagWTF.ReportAt(location, ctx, message);
		return new(ctx.CancellationToken);
	}
#else // maybe move to something like this?
	public static OperationCanceledException ReportWTF(SyntaxNode alien, string analyzerName, string disambig, SyntaxNodeAnalysisContext ctx)
	{
		DiagWTF.ReportAt(alien, ctx, $"[{analyzerName}{disambig}] AST/model contained {alien.GetType().FullName} unexpectedly; Analyzer needs updating");
		return new(ctx.CancellationToken);
	}
#endif

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		DiagInterpStringIsDollarAt,
		DiagListExprSpacing,
		DiagNoAnonClasses,
		DiagNoAnonDelegates,
		DiagNoDiscardingLocals,
		DiagNoQueryExpression,
		DiagRecordImplicitlyRefType,
		DiagSwitchShouldThrowIOE,
		DiagWTF);

	public override void Initialize(AnalysisContext context)
	{
		static string? CheckSpacingInList<T>(
			SeparatedSyntaxList<T> listContents,
			SyntaxToken openBracketToken,
			Func<string> serialiseOuter)
				where T : SyntaxNode
		{
			if (listContents.Count is 0) return serialiseOuter() is "[ ]" ? null : ERR_MSG_LIST_EXPR_EMPTY;
			var contentsWithTrivia = listContents.ToFullString();
			if (contentsWithTrivia.Contains("\n")) return null; // don't need to police spaces for multi-line expressions
			if (contentsWithTrivia.Length > 1
				? (contentsWithTrivia[contentsWithTrivia.Length - 1] is not ' '
					|| contentsWithTrivia[contentsWithTrivia.Length - 2] is ' ' or '\t')
				: contentsWithTrivia[0] is not ' ')
			{
				return ERR_MSG_LIST_EXPR_END;
			}
			return openBracketToken.TrailingTrivia.ToFullString() is " " ? null : ERR_MSG_LIST_EXPR_START;
		}
		static bool IsDiscard(AssignmentExpressionSyntax aes)
			=> aes.OperatorToken.RawKind is (int) SyntaxKind.EqualsToken && aes.Left is IdentifierNameSyntax { Identifier.Text: "_" };
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var invalidOperationExceptionSym = initContext.Compilation.GetTypeByMetadataName("System.InvalidOperationException")!;
			var switchExpressionExceptionSym = initContext.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.SwitchExpressionException");
			initContext.RegisterSyntaxNodeAction(
				snac =>
				{
					switch (snac.Node)
					{
						case AnonymousMethodExpressionSyntax:
							DiagNoAnonDelegates.ReportAt(snac.Node, snac);
							break;
						case AnonymousObjectCreationExpressionSyntax:
							DiagNoAnonClasses.ReportAt(snac.Node, snac);
							break;
						case AssignmentExpressionSyntax aes:
							if (!IsDiscard(aes)) break;
							if (snac.SemanticModel.GetSymbolInfo(aes.Right, snac.CancellationToken).Symbol?.Kind is not SymbolKind.Local) break;
							DiagNoDiscardingLocals.ReportAt(snac.Node, snac);
							break;
						case CollectionExpressionSyntax ces:
							var cesError = CheckSpacingInList(ces.Elements, ces.OpenBracketToken, ces.ToString);
							if (cesError is not null) DiagListExprSpacing.ReportAt(ces, snac, cesError);
							break;
						case InterpolatedStringExpressionSyntax ises:
							if (ises.StringStartToken.Text[0] is '@') DiagInterpStringIsDollarAt.ReportAt(ises, snac);
							break;
						case ListPatternSyntax lps:
							var lpsError = CheckSpacingInList(lps.Patterns, lps.OpenBracketToken, lps.ToString);
							if (lpsError is not null) DiagListExprSpacing.ReportAt(lps, snac, lpsError);
							break;
						case QueryExpressionSyntax:
							DiagNoQueryExpression.ReportAt(snac.Node, snac);
							break;
						case RecordDeclarationSyntax rds when rds.ClassOrStructKeyword.ToString() is not "class": // `record struct`s don't use this kind
							DiagRecordImplicitlyRefType.ReportAt(rds, snac);
							break;
						case SwitchExpressionArmSyntax { WhenClause: null, Pattern: DiscardPatternSyntax, Expression: ThrowExpressionSyntax tes }:
							var thrownExceptionType = snac.SemanticModel.GetThrownExceptionType(tes);
							if (thrownExceptionType is null)
							{
								DiagSwitchShouldThrowIOE.ReportAt(tes, DiagnosticSeverity.Warning, snac, ERR_MSG_SWITCH_THROWS_UNKNOWN);
							}
							else if (!invalidOperationExceptionSym.Matches(thrownExceptionType) && switchExpressionExceptionSym?.Matches(thrownExceptionType) != true)
							{
								DiagSwitchShouldThrowIOE.ReportAt(tes, snac, ERR_MSG_SWITCH_THROWS_WRONG_TYPE);
							}
							// else correct usage, do not flag
							break;
					}
				},
				SyntaxKind.AnonymousObjectCreationExpression,
				SyntaxKind.AnonymousMethodExpression,
				SyntaxKind.CollectionExpression,
				SyntaxKind.InterpolatedStringExpression,
				SyntaxKind.ListPattern,
				SyntaxKind.QueryExpression,
				SyntaxKind.RecordDeclaration,
				SyntaxKind.SimpleAssignmentExpression,
				SyntaxKind.SwitchExpressionArm);
		});
	}
}
