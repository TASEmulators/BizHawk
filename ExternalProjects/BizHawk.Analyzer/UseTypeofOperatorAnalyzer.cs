namespace BizHawk.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTypeofOperatorAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagNoGetTypeOnThis = new(
		id: "BHI1101",
		title: "Don't call this.GetType(), use typeof operator (or replace subtype check with better encapsulation)",
		messageFormat: "Replace this.GetType() with typeof({0}) (or replace subtype check with better encapsulation)",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor DiagNoGetTypeOnThisSealed = new(
		id: "BHI1100",
		title: "Don't call this.GetType() in sealed type, use typeof operator",
		messageFormat: "Replace this.GetType() with typeof({0})",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagNoGetTypeOnThisSealed, DiagNoGetTypeOnThis);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		ISymbol? objectDotGetTypeSym = null;
		context.RegisterOperationAction(
			oac =>
			{
				var operation = (IInvocationOperation) oac.Operation;
				if (operation.IsImplicit || operation.Instance is null) return;
				objectDotGetTypeSym ??= oac.Compilation.GetTypeByMetadataName("System.Object")!.GetMembers("GetType")[0];
				if (!objectDotGetTypeSym.Matches(operation.TargetMethod)) return;
				if (operation.Instance.Syntax is not ThisExpressionSyntax and not IdentifierNameSyntax { Identifier.Text: "GetType" }) return; // called on something that isn't `this`
				var enclosingType = operation.SemanticModel!.GetDeclaredSymbol(
					((CSharpSyntaxNode) operation.Syntax).EnclosingTypeDeclarationSyntax()!,
					oac.CancellationToken)!;
				oac.ReportDiagnostic(Diagnostic.Create(enclosingType.IsSealed ? DiagNoGetTypeOnThisSealed : DiagNoGetTypeOnThis, operation.Syntax.GetLocation(), enclosingType.Name));
			},
			OperationKind.Invocation);
	}
}
