namespace BizHawk.Analyzers;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

public static class RoslynUtils
{
	/// <summary>the chain of all classes which <paramref name="typeSym"/> inherits from, excluding itself</summary>
	/// <remarks>if <paramref name="typeSym"/> is not <see cref="TypeKind.Class"/>, the returned lazy collection will be empty</remarks>
	public static IEnumerable<ITypeSymbol> AllBaseTypes(this ITypeSymbol? typeSym)
	{
		if (typeSym?.TypeKind is not TypeKind.Class) yield break;
		while (true)
		{
			typeSym = typeSym.BaseType;
			if (typeSym is null) yield break;
			yield return typeSym;
		}
	}

	/// <summary>the chain of all types which <paramref name="typeSym"/> is contained within, excluding itself</summary>
	public static IEnumerable<ITypeSymbol> AllEnclosingTypes(this ITypeSymbol? typeSym)
	{
		var containing = typeSym?.ContainingType;
		while (containing is not null)
		{
			yield return containing;
			containing = containing.ContainingType;
		}
	}

	public static TypeDeclarationSyntax? EnclosingTypeDeclarationSyntax(this CSharpSyntaxNode node)
		=> node.NearestAncestorOfType<TypeDeclarationSyntax>();

	public static IBlockOperation? EnclosingCodeBlockOperation(this IOperation op)
	{
		var toReturn = op.FirstAncestorOrSelf<IBlockOperation>();
		while (toReturn?.Parent is IBlockOperation blockOp) toReturn = blockOp;
		return toReturn;
	}

	public static TNode? FirstAncestorOrSelf<TNode>(this IOperation? node)
		where TNode : class, IOperation
	{
		for (; node is not null; node = node.Parent) if (node is TNode tnode) return tnode;
		return null;
	}

	public static bool? GetIsCLSCompliant(this ITypeSymbol typeSym, ISymbol clsCompliantAttrSym)
		=> typeSym.AllEnclosingTypes().Prepend(typeSym)
			.Select(typeSym1 => typeSym1.GetOwnCLSCompliantValue(clsCompliantAttrSym))
			.FirstOrDefault(static tristate => tristate is not null);

	public static string GetMethodName(this ConversionOperatorDeclarationSyntax cods)
		=> cods.ImplicitOrExplicitKeyword.ToString() is "implicit"
			? WellKnownMemberNames.ImplicitConversionName
			: cods.CheckedKeyword.IsEmpty()
				? WellKnownMemberNames.ExplicitConversionName
				: WellKnownMemberNames.CheckedExplicitConversionName;

	public static string GetMethodName(this OperatorDeclarationSyntax ods)
		=> ods.OperatorToken.ToString() switch
		{
			"!" => WellKnownMemberNames.LogicalNotOperatorName,
			"!=" => WellKnownMemberNames.InequalityOperatorName,
			"%" => WellKnownMemberNames.ModulusOperatorName,
			"&" => WellKnownMemberNames.BitwiseAndOperatorName,
			"&&" => WellKnownMemberNames.LogicalAndOperatorName,
			"*" => ods.CheckedKeyword.IsEmpty()
				? WellKnownMemberNames.MultiplyOperatorName
				: WellKnownMemberNames.CheckedMultiplyOperatorName,
			"+" => ods.ParameterList.Parameters.Count is 1
				? WellKnownMemberNames.UnaryPlusOperatorName
				: ods.CheckedKeyword.IsEmpty()
					? WellKnownMemberNames.AdditionOperatorName
					: WellKnownMemberNames.CheckedAdditionOperatorName,
			"++" => ods.CheckedKeyword.IsEmpty()
				? WellKnownMemberNames.IncrementOperatorName
				: WellKnownMemberNames.CheckedIncrementOperatorName,
			"-" => ods.ParameterList.Parameters.Count is 1
				? ods.CheckedKeyword.IsEmpty()
					? WellKnownMemberNames.UnaryNegationOperatorName
					: WellKnownMemberNames.CheckedUnaryNegationOperatorName
				: ods.CheckedKeyword.IsEmpty()
					? WellKnownMemberNames.SubtractionOperatorName
					: WellKnownMemberNames.CheckedSubtractionOperatorName,
			"--" => ods.CheckedKeyword.IsEmpty()
				? WellKnownMemberNames.DecrementOperatorName
				: WellKnownMemberNames.CheckedDecrementOperatorName,
			"/" => ods.CheckedKeyword.IsEmpty()
				? WellKnownMemberNames.DivisionOperatorName
				: WellKnownMemberNames.CheckedDivisionOperatorName,
			"<" => WellKnownMemberNames.LessThanOperatorName,
			"<<" => WellKnownMemberNames.LeftShiftOperatorName,
			"<=" => WellKnownMemberNames.LessThanOrEqualOperatorName,
			"==" => WellKnownMemberNames.EqualityOperatorName,
			">" => WellKnownMemberNames.GreaterThanOperatorName,
			">=" => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
			">>" => WellKnownMemberNames.RightShiftOperatorName,
			">>>" => WellKnownMemberNames.UnsignedRightShiftOperatorName,
			"^" => WellKnownMemberNames.ExclusiveOrOperatorName,
			"false" => WellKnownMemberNames.FalseOperatorName,
			"true" => WellKnownMemberNames.TrueOperatorName,
			"|" => WellKnownMemberNames.BitwiseOrOperatorName,
			"||" => WellKnownMemberNames.LogicalOrOperatorName,
			"~" => WellKnownMemberNames.OnesComplementOperatorName,
			// ...and some operators only exist in VB.NET (dw, you're not missing anything)
			_ => throw new ArgumentException(paramName: nameof(ods), message: "pretend this is a BHI6660 unexpected token in AST (in this case, a new kind of operator was added to C#)"),
		};

	public static bool? GetOwnCLSCompliantValue(this ITypeSymbol typeSym, ISymbol clsCompliantAttrSym)
		=> typeSym.GetAttributes().FirstOrDefault(ad => clsCompliantAttrSym.Matches(ad.AttributeClass))
			?.ConstructorArguments[0].Value as bool?;

	private static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ExpressionSyntax exprSyn)
		=> exprSyn is ObjectCreationExpressionSyntax
			? model.GetTypeInfo(exprSyn).Type
			: null; // code reads `throw <something weird>`

	public static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ThrowExpressionSyntax tes)
		=> model.GetThrownExceptionType(tes.Expression);

	public static ITypeSymbol? GetThrownExceptionType(this SemanticModel model, ThrowStatementSyntax tss)
		=> model.GetThrownExceptionType(tss.Expression!);

	public static TypeInfo GetTypeInfo(this SemanticModel model, IArgumentOperation argOp, CancellationToken cancellationToken)
	{
		var syn = argOp.Syntax;
		return model.GetTypeInfo(syn is ArgumentSyntax argSyn ? argSyn.Expression : syn, cancellationToken);
	}

	public static bool IsAssignableFrom(this ITypeSymbol supertype, [NotNullWhen(true)] ITypeSymbol? subtype)
	{
		if (subtype is null) return false;
		bool MatchesPerTypeParamVariance(ITypeSymbol sym)
			=> supertype.Matches(sym); //TODO actually check type params
		return supertype.TypeKind switch
		{
			TypeKind.Class => subtype.AllBaseTypes().Prepend(subtype).Any(MatchesPerTypeParamVariance),
			TypeKind.Interface => subtype.AllInterfaces.Prepend(subtype).Any(MatchesPerTypeParamVariance),
			TypeKind.Enum or TypeKind.Pointer or TypeKind.Struct => MatchesPerTypeParamVariance(subtype),
			_ => throw new ArgumentException(paramName: nameof(supertype), message: "pretend this is a BHI6660 unexpected type kind (neither class/interface nor struct)"),
		};
	}

	public static bool IsEmpty(this SyntaxToken token)
		=> token.ToString().Length is 0;

	public static Location LocWithoutReceiver(this InvocationExpressionSyntax ies)
	{
		var location = ies.GetLocation();
		if (ies.Expression is MemberAccessExpressionSyntax maes)
		{
			location = Location.Create(location.SourceTree!, location.SourceSpan.Slice(maes.Expression.Span.Length));
		}
		return location;
	}

	public static Location LocWithoutReceiver(this IInvocationOperation operation)
		=> ((InvocationExpressionSyntax) operation.Syntax).LocWithoutReceiver();

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

	public static IEnumerable<AttributeSyntax> Matching(
		this SyntaxList<AttributeListSyntax> list,
		INamedTypeSymbol targetAttrSym,
		SemanticModel semanticModel,
		CancellationToken cancellationToken)
			=> list.SelectMany(static als => als.Attributes)
				.Where(aSyn => targetAttrSym.Matches(semanticModel.GetTypeInfo(aSyn, cancellationToken).Type));

	public static IEnumerable<AttributeSyntax> Matching(
		this SyntaxList<AttributeListSyntax> list,
		INamedTypeSymbol targetAttrSym,
		SyntaxNodeAnalysisContext snac)
			=> list.Matching(targetAttrSym, snac.SemanticModel, snac.CancellationToken);

	public static T? NearestAncestorOfType<T>(this CSharpSyntaxNode node)
		where T : CSharpSyntaxNode
		=> node.Parent?.FirstAncestorOrSelf<T>();

	public static T? NearestAncestorOfType<T>(this IOperation op)
		where T : class, IOperation
		=> op.Parent?.FirstAncestorOrSelf<T>();

	public static TextSpan Slice(this TextSpan span, int start)
		=> TextSpan.FromBounds(start: span.Start + start, end: span.End);

	public static TextSpan Slice(this TextSpan span, int start, int length)
		=> new(start: span.Start + start, length: length);

	public static string ToMetadataNameStr(this NameSyntax nameSyn)
		=> nameSyn switch
		{
			QualifiedNameSyntax qual => $"{qual.Left.ToMetadataNameStr()}.{qual.Right.ToMetadataNameStr()}",
			SimpleNameSyntax simple => simple.Identifier.ValueText,
			_ => throw new InvalidOperationException(),
		};
}
