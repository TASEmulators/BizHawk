namespace BizHawk.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class RoslynUtils
{
	public static SyntaxNode? EnclosingTypeDeclarationSyntax(this CSharpSyntaxNode node)
	{
		var parent = node.Parent;
		while (parent is not (null or TypeDeclarationSyntax)) parent = parent.Parent;
		return parent;
	}

	private static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ExpressionSyntax exprSyn)
		=> exprSyn is ObjectCreationExpressionSyntax
			? model.GetTypeInfo(exprSyn).Type
			: null; // code reads `throw <something weird>`

	public static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ThrowExpressionSyntax tes)
		=> model.GetThrownExceptionType(tes.Expression);

	public static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ThrowStatementSyntax tss)
		=> model.GetThrownExceptionType(tss.Expression!);

	public static bool Matches(this ISymbol expected, ISymbol? actual)
		=> SymbolEqualityComparer.Default.Equals(expected, actual);

#if false // easier to just `.OriginalDefinition` always
	public static bool Matches(this INamedTypeSymbol expected, INamedTypeSymbol? actual, bool degenericise)
	{
		if (degenericise)
		{
			if (expected.IsGenericType && !expected.IsUnboundGenericType) return expected.OriginalDefinition.Matches(actual, degenericise: true);
			if (actual is not null && actual.IsGenericType && !actual.IsUnboundGenericType) return expected.Matches(actual.OriginalDefinition, degenericise: true);
		}
		return expected.Matches(actual as ISymbol);
	}
#endif
}
