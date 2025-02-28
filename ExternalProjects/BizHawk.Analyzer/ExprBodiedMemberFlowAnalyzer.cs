namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExprBodiedMemberFlowAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagExprBodiedMemberFlow = new(
		id: "BHI1120",
		title: "Expression-bodied member should be flowed to next line correctly",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(/*HawkSourceAnalyzer.DiagWTF,*/ DiagExprBodiedMemberFlow);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		var ARROW_ONE_LINE = (' ', ' ');
//		var ARROW_POST_SIG = (' ', '\n');
		var ARROW_PRE_BODY = ('\n', ' ');
		context.RegisterSyntaxNodeAction(
			snac =>
			{
				var aecs = (ArrowExpressionClauseSyntax) snac.Node;
				(char Before, char After) expectedWhitespace;
				string kind;
				var parent = aecs.Parent;
				if (parent is null)
				{
					HawkSourceAnalyzer.ReportWTF(aecs, snac, message: $"[{nameof(ExprBodiedMemberFlowAnalyzer)}] Syntax node for expression-bodied member was orphaned?");
					return;
				}
				switch (parent)
				{
					case MethodDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "method";
						break;
					case PropertyDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "get-only prop";
						break;
					case AccessorDeclarationSyntax ads:
						expectedWhitespace = ARROW_ONE_LINE;
						switch (ads.Keyword.Text)
						{
							case "get":
								kind = ads.Parent?.Parent is IndexerDeclarationSyntax ? "get-indexer" : "getter";
								break;
							case "set":
								kind = ads.Parent?.Parent is IndexerDeclarationSyntax ? "set-indexer" : "setter";
								break;
							case "init":
								kind = "setter";
								break;
							case "add":
								kind = "event sub";
								break;
							case "remove":
								kind = "event unsub";
								break;
							default:
								HawkSourceAnalyzer.ReportWTF(parent, snac, message: $"[{nameof(ExprBodiedMemberFlowAnalyzer)}] Expression-bodied accessor was of an unexpected kind: {ads.Parent!.Parent!.GetType().FullName}");
								return;
						}
						break;
					case ConstructorDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "constructor";
						break;
					case LocalFunctionStatementSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "local method";
						break;
					case IndexerDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "get-only indexer";
						break;
					case OperatorDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "overloaded operator";
						break;
					case ConversionOperatorDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "overloaded cast operator";
						break;
					case DestructorDeclarationSyntax:
						expectedWhitespace = ARROW_PRE_BODY;
						kind = "finalizer";
						break;
					default:
						HawkSourceAnalyzer.ReportWTF(parent, snac, message: $"[{nameof(ExprBodiedMemberFlowAnalyzer)}] Expression-bodied member was of an unexpected kind: {parent.GetType().FullName}");
						return;
				}
				static string EscapeChar(char c)
					=> c is '\n' ? "\\n" : c.ToString();
				void Fail()
					=> DiagExprBodiedMemberFlow.ReportAt(parent, snac, $"Whitespace around {kind} arrow syntax should be `{EscapeChar(expectedWhitespace.Before)}=>{EscapeChar(expectedWhitespace.After)}`");
				if ((aecs.ArrowToken.HasLeadingTrivia ? '\n' : ' ') != expectedWhitespace.Before)
				{
					Fail();
					return;
				}
#pragma warning disable BHI3102 // LINQ `Contains(char)` is fine here
				var hasLineBreakAfterArrow = aecs.ArrowToken.HasTrailingTrivia && aecs.ArrowToken.TrailingTrivia.ToFullString().Contains('\n');
#pragma warning restore BHI3102
				if ((hasLineBreakAfterArrow ? '\n' : ' ') != expectedWhitespace.After) Fail();
			},
			SyntaxKind.ArrowExpressionClause);
	}
}
