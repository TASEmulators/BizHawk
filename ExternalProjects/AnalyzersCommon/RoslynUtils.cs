namespace BizHawk.Analyzers;

using System.Collections.Generic;
using System.Linq;

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

	public static string FullNamespace(this ISymbol? sym)
	{
		if (sym is null) return string.Empty;
		var s = sym.Name;
		var ns = sym.ContainingNamespace;
		while (ns is { IsGlobalNamespace: false })
		{
			s = $"{ns.Name}.{s}";
			ns = ns.ContainingNamespace;
		}
		return s;
	}

	/// <param name="sym">required to differentiate <c>record class</c> and <c>record struct</c> (later Roslyn versions will make this redundant)</param>
	/// <returns>
	/// one of:
	/// <list type="bullet">
	/// <item><description><c>{ "abstract", "class" }</c></description></item>
	/// <item><description><c>{ "abstract", "partial", "class" }</c></description></item>
	/// <item><description><c>{ "class" }</c></description></item>
	/// <item><description><c>{ "enum" }</c></description></item>
	/// <item><description><c>{ "interface" }</c></description></item>
	/// <item><description><c>{ "partial", "class" }</c></description></item>
	/// <item><description><c>{ "partial", "interface" }</c></description></item>
	/// <item><description><c>{ "partial", "record", "class" }</c></description></item>
	/// <item><description><c>{ "partial", "record", "struct" }</c></description></item>
	/// <item><description><c>{ "partial", "struct" }</c></description></item>
	/// <item><description><c>{ "record", "class" }</c></description></item>
	/// <item><description><c>{ "record", "struct" }</c></description></item>
	/// <item><description><c>{ "sealed", "class" }</c></description></item>
	/// <item><description><c>{ "sealed", "partial", "class" }</c></description></item>
	/// <item><description><c>{ "static", "class" }</c></description></item>
	/// <item><description><c>{ "static", "partial", "class" }</c></description></item>
	/// <item><description><c>{ "struct" }</c></description></item>
	/// </list>
	/// </returns>
	/// <remarks>this list is correct and complete as of C# 10, despite what the official documentation of these keywords might say (<c>static partial class</c> nowhere in sight)</remarks>
	public static IReadOnlyList<string> GetTypeKeywords(this BaseTypeDeclarationSyntax btds, INamedTypeSymbol sym)
	{
		// maybe it would make more sense to have a [Flags] enum (Abstract | Concrete | Delegate | Enum | Partial | Record | Sealed | ValueType) okay I've overengineered this
		// what about using ONLY cSym? I think that's more correct anyway as it combines partial interfaces
		if (btds is EnumDeclarationSyntax) return new[] { /*eds.EnumKeyword.Text*/"enum" };
		var tds = (TypeDeclarationSyntax) btds;
		List<string> keywords = new() { tds.Keyword.Text };
#if true
		if (tds is RecordDeclarationSyntax) keywords.Add(sym.IsValueType ? "struct" : "class");
#else // requires newer Roslyn
		if (tds is RecordDeclarationSyntax rds)
		{
			var s = rds.ClassOrStructKeyword.Text;
			keywords.Add(s is "" ? "class" : s);
		}
#endif
		var mods = tds.Modifiers.Select(static st => st.Text).ToList();
		if (mods.Contains("partial")) keywords.Insert(0, "partial");
		if (mods.Contains("abstract")) keywords.Insert(0, "abstract");
		else if (mods.Contains("sealed")) keywords.Insert(0, "sealed");
		else if (mods.Contains("static")) keywords.Insert(0, "static");
		return keywords;
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
}
