namespace BizHawk.SrcGen.InlineArray;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using BizHawk.Analyzers;

using TestType = (INamedTypeSymbol Sym, int Count, string? Namespace, SemanticModel SemanticModel);
using WidthResult = (string? WidthBytesExpr, int WidthBytes);

file static class Helpers
{
	public static IEnumerable<IFieldSymbol> GetInstanceFields(this ITypeSymbol typeSym)
		=> typeSym.GetMembers().Where(static sym => !sym.IsStatic).OfType<IFieldSymbol>();

	public static WidthResult? WidthAsPrimitive(this ITypeSymbol typeSym)
		=> (typeSym is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: var underlyingTypeSym }
			? underlyingTypeSym!
			: typeSym).SpecialType switch
		{
			SpecialType.System_Object => ("/*IntPtr.Size*/sizeof(ulong)", sizeof(ulong)),
			SpecialType.System_Boolean => ("/*sizeof(bool)*/1", 1),
			SpecialType.System_Char => ("/*sizeof(char)*/2", 2),
			SpecialType.System_SByte => ("sizeof(sbyte)", sizeof(sbyte)),
			SpecialType.System_Byte => ("sizeof(byte)", sizeof(byte)),
			SpecialType.System_Int16 => ("sizeof(short)", sizeof(short)),
			SpecialType.System_UInt16 => ("sizeof(ushort)", sizeof(ushort)),
			SpecialType.System_Int32 => ("sizeof(int)", sizeof(int)),
			SpecialType.System_UInt32 => ("sizeof(uint)", sizeof(uint)),
			SpecialType.System_Int64 => ("sizeof(long)", sizeof(long)),
			SpecialType.System_UInt64 => ("sizeof(ulong)", sizeof(ulong)),
			SpecialType.System_Decimal => ("sizeof(decimal)", sizeof(decimal)),
			SpecialType.System_Single => ("sizeof(float)", sizeof(float)),
			SpecialType.System_Double => ("sizeof(float)", sizeof(float)),
			SpecialType.System_IntPtr => ("/*IntPtr.Size*/sizeof(ulong)", sizeof(ulong)),
			SpecialType.System_UIntPtr => ("/*UIntPtr.Size*/sizeof(ulong)", sizeof(ulong)),
			_ => null,
		};
}

/// <remarks>I should not have to write a "keyed list allowing duplicates", Microsoft</remarks>
file sealed class WidthResultCache
{
	private List<(ITypeSymbol Sym, WidthResult Result)> _items = new();

	private List<string> _keys = new();

	public WidthResult? this[ITypeSymbol sym]
	{
		get
		{
			var key = sym.Name;
			var i = _keys.BinarySearch(key);
			if (i < 0) return null;
			while (1 < i && _keys[i - 1] == key) i--;
			do
			{
				var (sym1, result) = _items[i];
				if (sym1.Matches(sym)) return result;
				i++;
			}
			while (_keys[i] == key);
			return null;
		}
	}

	public WidthResult Add(ITypeSymbol sym, WidthResult result)
	{
		var key = sym.Name;
		var i = _keys.BinarySearch(key);
		_keys.Insert(i < 0 ? ~i : i, key);
		_items.Insert(i < 0 ? ~i : i, (sym, result));
		return result;
	}
}

file readonly struct UnmanagedWidthCalculator()
{
	private static readonly WidthResult INVALID = (null, -1);

	private readonly WidthResultCache _cache = new();

	private WidthResult Calc(ITypeSymbol typeSym)
	{
		if (_cache[typeSym] is WidthResult cached) return cached;
		if (!typeSym.IsValueType) return _cache.Add(typeSym, INVALID);
		if (typeSym.WidthAsPrimitive() is WidthResult primitive) return _cache.Add(typeSym, primitive);
		var fields = typeSym.GetInstanceFields().ToArray();
		if (fields.Length <= 1)
		{
			return _cache.Add(
				typeSym,
				fields.Length is 0
					? (typeSym as INamedTypeSymbol)?.GetMetadataNameStr() is "System.Guid" // has 0 fields for some reason
						? (null, /*sizeof(Guid)*/16)
						: INVALID
					: Calc(fields[0].Type)); // recurse on single field
		}
		var totalWidth = 0;
		foreach (var field in fields)
		{
			var result = Calc(field.Type);
			if (result.WidthBytes < 0) return _cache.Add(typeSym, result);
			totalWidth += result.WidthBytes;
		}
		return _cache.Add(typeSym, (null, totalWidth));
	}

	public string For(ITypeSymbol typeSym)
	{
		var (widthBytesExpr, widthBytes) = Calc(typeSym);
		return widthBytesExpr is null ? widthBytes.ToString() : widthBytesExpr;
	}
}

[Generator]
public sealed class InlineArrayGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var structDecls = context.SyntaxProvider.ForAttributeWithMetadataName(
			"System.Runtime.CompilerServices.InlineArray1Attribute", // doesn't work with the "real" one, maybe because it's polyfilled
			predicate: static (node, _) => node is StructDeclarationSyntax,
			transform: static (ctx, cancellationToken) => (
				(INamedTypeSymbol) ctx.TargetSymbol,
				ctx.Attributes[0].ConstructorArguments[0].Value is int elemCount ? elemCount : 0,
				ctx.TargetSymbol.ContainingNamespace?.GetMetadataNameStr(),
				ctx.SemanticModel));
		context.RegisterSourceOutput(
			context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider).Combine(structDecls.Collect()),
			Execute);
	}

	public void Execute(
		SourceProductionContext context,
		((AnalyzerConfigOptionsProvider AnalyzerOptions, Compilation Compilation) More, ImmutableArray<TestType> StructDecls) value)
	{
		var ((config, compilation), structDecls) = value;
		_ = config.GlobalOptions.TryGetValue("build_property.TargetFramework", out var tfm);
		tfm ??= string.Empty;
		if (!tfm.StartsWith("netstandard") && !tfm.StartsWith("net4") && tfm is not ("net5.0" or "net6.0" or "net7.0")) return; // must be .NET 8+ which already have runtime support
		var isRangeTypeAvailable = compilation.GetTypeByMetadataName("System.Range") is not null;
		UnmanagedWidthCalculator sizeCalc = new(/*compilation*/);
		foreach (var group in structDecls.GroupBy(static tuple => tuple.Namespace))
		{
			StringBuilder sb = new("using System;\nusing System.Runtime.CompilerServices;\nusing System.Runtime.InteropServices;\n\n#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference\n");
			var indent = string.Empty;
			var containingNSName = group.Key;
			if (containingNSName is not null)
			{
				sb.Append($"namespace {containingNSName}\n{{");
				indent = "\t";
			}
			sb.Append($"\n{indent}file static class Helper\n{
				indent}{{\n{
				indent}\tpublic static unsafe ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(in TBuffer buffer, int count)\n{
				indent}\t\twhere TBuffer : struct\n{
				indent}\t\t=> new(Unsafe.AsPointer(ref Unsafe.AsRef(in buffer)), count);\n\n{
				indent}\tpublic static unsafe Span<TElement> InlineArrayAsSpan<TBuffer, TElement>(in TBuffer buffer, int count)\n{
				indent}\t\twhere TBuffer : struct\n{
				indent}\t\t=> new(Unsafe.AsPointer(ref Unsafe.AsRef(in buffer)), count);\n\n{
				indent}\tpublic static ref T ThrowAOORE<T>(int index, string message)\n{
				indent}\t\t=> throw new ArgumentOutOfRangeException(paramName: nameof(index), index, message: message);\n");
			if (isRangeTypeAvailable)
			{
				sb.Append($"\n{
					indent}\tpublic static ReadOnlySpan<T> Slice<T>(ReadOnlySpan<T> span, Range range)\n{
					indent}\t\t=> throw new NotImplementedException();\n\n{
					indent}\tpublic static Span<T> Slice<T>(Span<T> span, Range range)\n{
					indent}\t\t=> throw new NotImplementedException();\n");
			}
			sb.Append($"{indent}}}\n");
			foreach (var (sym, elemCount, _, semanticModel) in group)
			{
				if (elemCount <= 0) continue; //TODO warn
				var memberSyms = sym.GetInstanceFields().ToList();
				if (memberSyms.Count is not 1 || memberSyms[0] is not { Name: "_element0" } elem0Sym) continue; //TODO warn
				//TODO warn if `elem0Sym.DeclaredAccessibility` is less than (literally?) `sym.DeclaredAccessibility`
				var containingType = sym.ContainingType;
				if (containingType is not null)
				{
					var containingTypeKind = containingType.IsValueType
						? containingType.IsReadOnly ? "readonly struct" : "struct"
						: containingType.IsRecord
							? "record class"
							: containingType.BaseType is null ? "interface" : "class";
					sb.Append($"{containingType.GetAccessModifierKeyword()} {containingTypeKind} {containingType.Name}\n{{\n");
					indent += '\t';
				}
				var isROStruct = sym.IsReadOnly;
				var structName = sym.Name;
				var elemTypeName = elem0Sym.Type.GetCSharpKeywordOrName();
				sb.Append($"\n{indent}[StructLayout(LayoutKind.Sequential, Size = ELEM_COUNT * {sizeCalc.For(elem0Sym.Type)})]\n{
					indent}[UnsafeValueType]\n{
					indent}{sym.GetAccessModifierKeyword()} {(isROStruct ? "readonly partial struct" : "partial struct")} {structName}\n{
					indent}{{\n");
				indent += '\t';
				sb.Append($"{indent}private const int ELEM_COUNT = {elemCount};\n");
				var spanRORWTypeName = $"ReadOnlySpan<{elemTypeName}>";
				sb.Append($"\n{indent}public static implicit operator /*unchecked*/ {spanRORWTypeName}(in {structName} buffer)\n{
					indent}\t=> Helper.InlineArrayAsReadOnlySpan<{structName}, {elemTypeName}>(in buffer, ELEM_COUNT);\n");
				if (!isROStruct)
				{
					spanRORWTypeName = $"Span<{elemTypeName}>";
					sb.Append($"\n{indent}public static implicit operator /*unchecked*/ {spanRORWTypeName}(in {structName} buffer)\n{
						indent}\t=> Helper.InlineArrayAsSpan<{structName}, {elemTypeName}>(in buffer, ELEM_COUNT);\n");
				}
				var byrefKeywords = isROStruct ? "ref readonly" : "ref";
				if (isRangeTypeAvailable)
				{
					sb.Append($"\n{indent}public {byrefKeywords} {elemTypeName} this[Index index]\n{
						indent}\t=> ref this[index.GetOffset(ELEM_COUNT)];\n");
				}
				sb.Append($"\n{indent}public {byrefKeywords} {elemTypeName} this[int index]\n");
#if true
				var rwElem0RefExpr = isROStruct ? "ref Unsafe.AsRef(in GetPinnableReference())" : "ref GetPinnableReference()";
				sb.Append($"{indent}\t=> ref index < 0 || ELEM_COUNT < index\n{
					indent}\t\t? ref Helper.ThrowAOORE<{elemTypeName}>(index, \"invalid index, must be in 0..<{elemCount}\")\n{
					indent}\t\t: ref Unsafe.Add({rwElem0RefExpr}, index);\n");
#else // didn't work
				sb.Append($"{indent}\t=> ref (({spanRORWTypeName}) this)[index];\n");
#endif
				if (isRangeTypeAvailable)
				{
					sb.Append($"\n{indent}public {spanRORWTypeName} this[Range range]\n{
						indent}\t=> (({spanRORWTypeName}) this).Slice(range);\n");
				}
				sb.Append($"\n{indent}public unsafe {byrefKeywords} {elemTypeName} GetPinnableReference()\n{
					indent}\t=> ref _element0;\n");
				indent = indent.Substring(startIndex: 0, length: indent.Length - 1);
				sb.Append($"{indent}}}\n");
				if (containingType is not null)
				{
					sb.Append("}\n");
					indent = indent.Substring(startIndex: 0, length: indent.Length - 1);
				}
			}
			if (containingNSName is not null) sb.Append("}\n");
			sb.Append("#pragma warning restore CS9084\n");
			context.AddSource($"{containingNSName ?? "global"}.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
		}
	}
}
