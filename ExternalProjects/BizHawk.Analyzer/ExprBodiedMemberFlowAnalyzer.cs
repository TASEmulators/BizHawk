namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagExprBodiedMemberFlow);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var ARROW_ONE_LINE = (' ', ' ');
//			var ARROW_POST_SIG = (' ', '\n');
			var ARROW_PRE_BODY = ('\n', ' ');
			initContext.RegisterSyntaxNodeAction(
				snac =>
				{
					var aecs = (ArrowExpressionClauseSyntax) snac.Node;
					(char Before, char After) expectedWhitespace;
					string kind;
					var parent = aecs.Parent;
					if (parent is null)
					{
						snac.ReportDiagnostic(Diagnostic.Create(DiagExprBodiedMemberFlow, aecs.GetLocation(), "Syntax node for expression-bodied member was orphaned?"));
						return;
					}
					void Flag(string message)
						=> snac.ReportDiagnostic(Diagnostic.Create(DiagExprBodiedMemberFlow, parent.GetLocation(), message));
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
									Flag($"Expression-bodied accessor was of an unexpected kind: {ads.Parent!.Parent!.GetType().FullName}");
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
							Flag($"Expression-bodied member was of an unexpected kind: {parent.GetType().FullName}");
							return;
					}
					static string EscapeChar(char c)
						=> c is '\n' ? "\\n" : c.ToString();
					void Fail()
						=> Flag($"Whitespace around {kind} arrow syntax should be `{EscapeChar(expectedWhitespace.Before)}=>{EscapeChar(expectedWhitespace.After)}`");
					if ((aecs.ArrowToken.HasLeadingTrivia ? '\n' : ' ') != expectedWhitespace.Before)
					{
						Fail();
						return;
					}
					var hasLineBreakAfterArrow = aecs.ArrowToken.HasTrailingTrivia && aecs.ArrowToken.TrailingTrivia.ToFullString().Contains('\n');
					if ((hasLineBreakAfterArrow ? '\n' : ' ') != expectedWhitespace.After) Fail();
				},
				SyntaxKind.ArrowExpressionClause);
		});
	}
}
