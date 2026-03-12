namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnforceMessageInTestAssertCallAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagEnforceMessageInTestAssertCall = new(
		id: "BHI1600",
		title: "Provide message text in Assert.* call",
		messageFormat: "Pass message to {0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly string[] AssertClassNames = { "Assert", "CollectionAssert", "StringAssert" }; //TODO make configurable

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(DiagEnforceMessageInTestAssertCall);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var compilation = initContext.Compilation;
			var assertClassSyms = AssertClassNames.Select(className => compilation.GetTypeByMetadataName($"Microsoft.VisualStudio.TestTools.UnitTesting.{className}"))
				.Where(static sym => sym is not null).ToList();
			if (assertClassSyms.Count is 0) return; // project does not have MSTest dependency
			var inconclusive0Sym = assertClassSyms.First(static sym => sym!.Name is "Assert")!
				.GetMembers("Inconclusive").First();
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (IInvocationOperation) oac.Operation;
					if (operation.Arguments.FirstOrDefault(static arg => arg.Parameter!.Name is "message")
						is { ArgumentKind: not ArgumentKind.DefaultValue })
					{
						return; // already provided
					}
					var calledSym = operation.TargetMethod.ConstructedFrom;
					if (inconclusive0Sym.Matches(calledSym)) return; // don't need message when skipping
					ITypeSymbol receiverExprType = calledSym.ContainingType;
					if (operation.Arguments.FirstOrDefault() is IArgumentOperation receiverExpr)
					{
						receiverExprType = operation.SemanticModel!.GetTypeInfo(receiverExpr, oac.CancellationToken).Type!;
					}
					if (!assertClassSyms.Exists(receiverExprType.Matches)) return;
					// naive check for if there are other calls to `calledSym` in this test method
					var siblings = operation.EnclosingCodeBlockOperation()!.Operations.OfType<IExpressionStatementOperation>()
						.Select(static statementOp => statementOp.Operation).OfType<IInvocationOperation>()
						.Where(invocationOp => calledSym.Matches(invocationOp.TargetMethod.ConstructedFrom))
						.Except([ operation ]);
					DiagEnforceMessageInTestAssertCall.ReportAt(operation, isErrorSeverity: siblings.Any(), oac, calledSym.Name);
				},
				OperationKind.Invocation);
		});
	}
}
