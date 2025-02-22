namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OrderBySelfAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagUseOrderBySelfExt = new(
		id: "BHI3101",
		title: "Use .Order()/.OrderDescending() shorthand",
		messageFormat: "Replace .OrderBy{0}(e => e) with .Order{0}()",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagUseOrderBySelfExt);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			if (initContext.Compilation.GetTypeByMetadataName("BizHawk.Common.CollectionExtensions.CollectionExtensions") is null) return; // project does not have BizHawk.Common dependency
			var linqExtClassSym = initContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")!;
			var orderByAscSym = linqExtClassSym.GetMembers("OrderBy").Cast<IMethodSymbol>().First(sym => sym.Parameters.Length is 2);
			var orderByDescSym = linqExtClassSym.GetMembers("OrderByDescending").Cast<IMethodSymbol>().First(sym => sym.Parameters.Length is 2);
			initContext.RegisterOperationAction(
				oac =>
				{
					static bool IsSelfReturnLambda(AnonymousFunctionExpressionSyntax afes)
					{
						ParameterSyntax paramSyn;
						switch (afes)
						{
							case AnonymousMethodExpressionSyntax ames: // banned in BizHawk but included for completeness
								if ((ames.ParameterList?.Parameters)?.Count is not 1) return false;
								paramSyn = ames.ParameterList.Parameters[0];
								break;
							case ParenthesizedLambdaExpressionSyntax ples:
								if (ples.ParameterList.Parameters.Count is not 1) return false;
								paramSyn = ples.ParameterList.Parameters[0];
								break;
							case SimpleLambdaExpressionSyntax sles:
								paramSyn = sles.Parameter;
								break;
							default:
								return false;
						}
						bool Matches(IdentifierNameSyntax ins)
							=> ins.Identifier.ValueText == paramSyn.Identifier.ValueText;
						if (afes.ExpressionBody is not null) return afes.ExpressionBody is IdentifierNameSyntax ins && Matches(ins);
						return afes.Block!.Statements.Count is 1 && afes.Block.Statements[0] is ReturnStatementSyntax { Expression: IdentifierNameSyntax ins1 } && Matches(ins1);
					}
					var operation = (IInvocationOperation) oac.Operation;
					var calledSym = operation.TargetMethod.ConstructedFrom;
					if (!(orderByAscSym!.Matches(calledSym) || orderByDescSym!.Matches(calledSym))) return;
					if (((ArgumentSyntax) operation.Arguments[1].Syntax).Expression is not AnonymousFunctionExpressionSyntax afes) return;
					if (IsSelfReturnLambda(afes)) DiagUseOrderBySelfExt.ReportAt(afes, oac, orderByDescSym.Matches(calledSym) ? "Descending" : string.Empty);
				},
				OperationKind.Invocation);
		});
	}
}
