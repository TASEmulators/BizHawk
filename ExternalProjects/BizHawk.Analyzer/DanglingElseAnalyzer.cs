namespace BizHawk.Analyzers;

using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DanglingElseAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagDanglingElse = new(
		id: "BHI1121",
		title: "Dangling else",
		messageFormat: "Dangling `{0}`! Add braces to clarify intent",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(DiagDanglingElse);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(
			static snac =>
			{
				var targetElse = (ElseClauseSyntax) snac.Node;
				var firstIf = targetElse.Parent!;
				while (firstIf.Parent is ElseClauseSyntax outerElse) firstIf = outerElse.Parent!;
				if (firstIf.Parent is not IfStatementSyntax { Else: null } outer) return;
				DiagDanglingElse.ReportAt(outer.IfKeyword, snac, targetElse.Statement is IfStatementSyntax ? "else if" : "else");
			},
			SyntaxKind.ElseClause);
	}
}
