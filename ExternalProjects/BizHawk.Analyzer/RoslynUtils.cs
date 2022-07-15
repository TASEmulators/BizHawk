namespace BizHawk.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RoslynUtils
{
	private static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ExpressionSyntax exprSyn)
		=> exprSyn is ObjectCreationExpressionSyntax oces
			? model.GetTypeInfo(exprSyn).Type
			: null; // code reads `throw <something weird>`

	public static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ThrowExpressionSyntax tes)
		=> model.GetThrownExceptionType(tes.Expression);

	public static bool Matches(this ITypeSymbol expected, ITypeSymbol? actual)
		=> SymbolEqualityComparer.Default.Equals(expected, actual);
}
