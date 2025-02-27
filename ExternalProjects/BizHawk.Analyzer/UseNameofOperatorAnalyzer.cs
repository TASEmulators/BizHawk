namespace BizHawk.Analyzers;

using System.Collections.Immutable;

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
		context.RegisterCompilationStartAction(initContext =>
		{
			var memberInfoDotNameSym = initContext.Compilation.GetTypeByMetadataName("System.Reflection.MemberInfo")!
				.GetMembers("Name")[0];
			var typeDotToStringSym = initContext.Compilation.GetTypeByMetadataName("System.Type")!
				.GetMembers(WellKnownMemberNames.ObjectToString)[0];
			initContext.RegisterSyntaxNodeAction(
				snac =>
				{
					var toes = (TypeOfExpressionSyntax) snac.Node;
					switch (toes.Parent)
					{
						case BinaryExpressionSyntax bes:
							if ((ReferenceEquals(toes, bes.Left) ? bes.Right : bes.Left) is LiteralExpressionSyntax { Token.RawKind: (int) SyntaxKind.StringLiteralToken })
							{
								DiagNoToStringOnType.ReportAt(toes, snac, [ toes.Type.GetText(), " in string concatenation" ]);
							}
							break;
						case InterpolationSyntax:
							DiagNoToStringOnType.ReportAt(toes, snac, [ toes.Type.GetText(), " in string interpolation" ]);
							break;
						case MemberAccessExpressionSyntax maes1:
							var accessed = snac.SemanticModel.GetSymbolInfo(maes1.Name, snac.CancellationToken).Symbol;
							if (memberInfoDotNameSym.Matches(accessed)) DiagUseNameof.ReportAt(maes1, snac, [ toes.Type.GetText() ]);
							else if (typeDotToStringSym.Matches(accessed)) DiagNoToStringOnType.ReportAt(maes1, snac, [ toes.Type.GetText(), ".ToString()" ]);
							break;
					}
				},
				SyntaxKind.TypeOfExpression);
		});
	}
}
