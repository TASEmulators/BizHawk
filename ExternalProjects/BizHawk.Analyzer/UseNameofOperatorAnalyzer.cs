namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseNameofOperatorAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagNoToStringOnType = new(
		id: "BHI1103",
		title: "Don't call typeof(T).ToString(), use nameof operator or typeof(T).FullName",
		messageFormat: "Replace typeof({0}){1} with either nameof({0}) or typeof({0}).FullName",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagUseNameof = new(
		id: "BHI1102",
		title: "Don't call typeof(T).Name, use nameof operator",
		messageFormat: "Replace typeof({0}).Name with nameof({0})",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagNoToStringOnType, DiagUseNameof);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		ISymbol? memberInfoDotNameSym = null;
		ISymbol? typeDotToStringSym = null;
		context.RegisterSyntaxNodeAction(
			snac =>
			{
				memberInfoDotNameSym ??= snac.Compilation.GetTypeByMetadataName("System.Reflection.MemberInfo")!.GetMembers("Name")[0];
				typeDotToStringSym ??= snac.Compilation.GetTypeByMetadataName("System.Type")!.GetMembers("ToString")[0];
				var toes = (TypeOfExpressionSyntax) snac.Node;
				switch (toes.Parent)
				{
					case BinaryExpressionSyntax bes:
						if ((ReferenceEquals(toes, bes.Left) ? bes.Right : bes.Left) is LiteralExpressionSyntax { Token.RawKind: (int) SyntaxKind.StringLiteralToken })
						{
							snac.ReportDiagnostic(Diagnostic.Create(DiagNoToStringOnType, toes.GetLocation(), toes.Type.GetText(), " in string concatenation"));
						}
						break;
					case InterpolationSyntax:
						snac.ReportDiagnostic(Diagnostic.Create(DiagNoToStringOnType, toes.GetLocation(), toes.Type.GetText(), " in string interpolation"));
						break;
					case MemberAccessExpressionSyntax maes1:
						var accessed = snac.SemanticModel.GetSymbolInfo(maes1.Name, snac.CancellationToken).Symbol;
						if (memberInfoDotNameSym.Matches(accessed))
						{
							snac.ReportDiagnostic(Diagnostic.Create(DiagUseNameof, maes1.GetLocation(), toes.Type.GetText()));
						}
						else if (typeDotToStringSym.Matches(accessed))
						{
							snac.ReportDiagnostic(Diagnostic.Create(DiagNoToStringOnType, maes1.GetLocation(), toes.Type.GetText(), ".ToString()"));
						}
						break;
				}
			},
			SyntaxKind.TypeOfExpression);
	}
}
