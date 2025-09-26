namespace BizHawk.SrcGen.CLSCompliance;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using BizHawk.Analyzers;

[Generator]
public sealed class CLSComplianceGenerator : IIncrementalGenerator
{
	private static readonly DiagnosticDescriptor DiagTest = new(
		id: "BHI1337",
		title: "test",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var declSyns0 = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax, // excludes enums because they can't be `partial`; you'll have to add the attribute explicitly
			transform: static (ctx, _) => (TypeDeclarationSyntax) ctx.Node);
		context.RegisterSourceOutput(context.CompilationProvider.Combine(declSyns0.Collect()), Execute);
	}

	private static void Execute(
		SourceProductionContext context,
		(Compilation Compilation, ImmutableArray<TypeDeclarationSyntax> DeclSyns) receiver)
	{
		var clsCompliantAttrSym = receiver.Compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName)!;
		bool? InheritedCLSCompliant(ITypeSymbol typeSym)
			=> typeSym.AllBaseTypes().Select(typeSym1 => typeSym1.GetOwnCLSCompliantValue(clsCompliantAttrSym))
				.FirstOrDefault(static tristate => tristate is not null);
		string? NodesToNamespaceBlock(
			IGrouping<string, (TypeDeclarationSyntax Syn, INamedTypeSymbol Sym, string Namespace)> group)
		{
			StringBuilder sb = new();
			HashSet<string> seen = new(); // `DistinctBy` only exists in .NET Core >:(
			foreach (var (syn, sym, _) in group)
			{
				if (!seen.Add(sym.Name)) continue; // dedup partials
				if (sym.GetIsCLSCompliant(clsCompliantAttrSym) is not null) continue; // already set explicitly
				var accessMod = "internal";
				var hasPartial = false;
				foreach (var mod in syn.Modifiers)
				{
					if (mod.Text is "public" or "internal" or "protected" or "private")
					{
						accessMod = mod.Text;
						if (accessMod is not "public") break;
					}
					else if (mod.Text is "partial") hasPartial = true; // initially set this up for `static class`, `readonly struct`, etc. but it turns out you don't need to repeat them
				}
				if (accessMod is not "public") continue;
				if (!hasPartial)
				{
//					DiagTest.ReportAt(syn, context, "make this type partial");
					continue;
				}
				sb.AppendFormat(
					"\t[CLSCompliant({0})]\n\t{1} partial {2} {3}{4} {{}}\n\n",
					(InheritedCLSCompliant(sym) ?? true).ToString().ToLowerInvariant(),
					accessMod,
					syn switch
					{
						ClassDeclarationSyntax => "class",
						InterfaceDeclarationSyntax => "interface",
						RecordDeclarationSyntax rds => $"record {rds.ClassOrStructKeyword.Text}",
						StructDeclarationSyntax => "struct",
						_ => throw new InvalidOperationException("pretend this is a BHI6660 unexpected node in AST (in this case, a new kind of type declaration was added to C#)"),
					},
					syn.Identifier.Text,
					syn.TypeParameterList?.ToString() ?? string.Empty);
			}
			return sb.Length is 0 ? null : $"\n\nnamespace {group.Key}\n{{\n{sb}}}";
		}

		var compilation = receiver.Compilation;
		var clsCompliantSym = compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName)!;
		List<(TypeDeclarationSyntax Syn, INamedTypeSymbol Sym, string Namespace)> typeDecls = new();
		foreach (var tuple in receiver.DeclSyns
			.Select(tds => (Syn: tds, Sym: compilation.GetSemanticModel(tds.SyntaxTree)!.GetDeclaredSymbol(tds, context.CancellationToken)!))
			.Where(tuple => tuple.Sym.GetIsCLSCompliant(clsCompliantSym) is null))
		{
			var parentSyn = tuple.Syn.Parent;
			var nds = parentSyn as NamespaceDeclarationSyntax;
			if (nds is not null)
			{
				typeDecls.Add((
					tuple.Syn,
					tuple.Sym,
					nds.Name.ToMetadataNameStr() ?? string.Empty));
			}
			else if (parentSyn is not null) // must be a type decl. with this as its inner class/struct
			{
				//TODO warn
			}
		}
		var src = $"#pragma warning disable MA0104{string.Concat(
			typeDecls.GroupBy(static tuple => tuple.Namespace)
				.Select(NodesToNamespaceBlock)
				.Where(static s => s is not null))}";
		context.AddSource("CLSCompliance.cs", SourceText.From(src, Encoding.UTF8));
	}
}
