namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HawkSourceAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_SWITCH_THROWS_UNKNOWN = "Indeterminable exception type in default switch branch, should be InvalidOperationException/SwitchExpressionException";

	private const string ERR_MSG_SWITCH_THROWS_WRONG_TYPE = "Incorrect exception type in default switch branch, should be InvalidOperationException/SwitchExpressionException";

	private static readonly DiagnosticDescriptor DiagInterpStringIsDollarAt = new(
		id: "BHI1004",
		title: "Verbatim interpolated strings should begin $@, not @$",
		messageFormat: "Swap @ and $ on interpolated string",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
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

	private static readonly DiagnosticDescriptor DiagSwitchShouldThrowIOE = new(
		id: "BHI1005",
		title: "Default branch of switch expression should throw InvalidOperationException/SwitchExpressionException or not throw",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		DiagInterpStringIsDollarAt,
		DiagNoAnonClasses,
		DiagNoAnonDelegates,
		DiagNoDiscardingLocals,
		DiagNoQueryExpression,
		DiagSwitchShouldThrowIOE);

	public override void Initialize(AnalysisContext context)
	{
		static bool IsDiscard(AssignmentExpressionSyntax aes)
			=> aes.OperatorToken.RawKind is (int) SyntaxKind.EqualsToken && aes.Left is IdentifierNameSyntax { Identifier.Text: "_" };
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		INamedTypeSymbol? invalidOperationExceptionSym = null;
		INamedTypeSymbol? switchExpressionExceptionSym = null;
		context.RegisterSyntaxNodeAction(
			snac =>
			{
				if (invalidOperationExceptionSym is null)
				{
					invalidOperationExceptionSym = snac.Compilation.GetTypeByMetadataName("System.InvalidOperationException")!;
					switchExpressionExceptionSym = snac.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.SwitchExpressionException");
				}
				switch (snac.Node)
				{
					case AnonymousMethodExpressionSyntax:
						snac.ReportDiagnostic(Diagnostic.Create(DiagNoAnonDelegates, snac.Node.GetLocation()));
						break;
					case AnonymousObjectCreationExpressionSyntax:
						snac.ReportDiagnostic(Diagnostic.Create(DiagNoAnonClasses, snac.Node.GetLocation()));
						break;
					case AssignmentExpressionSyntax aes when IsDiscard(aes) && snac.SemanticModel.GetSymbolInfo(aes.Right).Symbol?.Kind is SymbolKind.Local:
						snac.ReportDiagnostic(Diagnostic.Create(DiagNoDiscardingLocals, snac.Node.GetLocation()));
						break;
					case InterpolatedStringExpressionSyntax ises:
						if (ises.StringStartToken.Text[0] is '@') snac.ReportDiagnostic(Diagnostic.Create(DiagInterpStringIsDollarAt, ises.GetLocation()));
						break;
					case QueryExpressionSyntax:
						snac.ReportDiagnostic(Diagnostic.Create(DiagNoQueryExpression, snac.Node.GetLocation()));
						break;
					case SwitchExpressionArmSyntax { WhenClause: null, Pattern: DiscardPatternSyntax, Expression: ThrowExpressionSyntax tes }:
						var thrownExceptionType = snac.SemanticModel.GetThrownExceptionType(tes);
						if (thrownExceptionType is null)
						{
							snac.ReportDiagnostic(Diagnostic.Create(
								DiagSwitchShouldThrowIOE,
								tes.GetLocation(),
								DiagnosticSeverity.Warning,
								additionalLocations: null,
								properties: null,
								ERR_MSG_SWITCH_THROWS_UNKNOWN));
						}
						else if (!invalidOperationExceptionSym.Matches(thrownExceptionType) && switchExpressionExceptionSym?.Matches(thrownExceptionType) != true)
						{
							snac.ReportDiagnostic(Diagnostic.Create(DiagSwitchShouldThrowIOE, tes.GetLocation(), ERR_MSG_SWITCH_THROWS_WRONG_TYPE));
						}
						// else correct usage, do not flag
						break;
				}
			},
			SyntaxKind.AnonymousObjectCreationExpression,
			SyntaxKind.AnonymousMethodExpression,
			SyntaxKind.InterpolatedStringExpression,
			SyntaxKind.QueryExpression,
			SyntaxKind.SimpleAssignmentExpression,
			SyntaxKind.SwitchExpressionArm);
	}
}
