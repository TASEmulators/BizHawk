using BizHawk.Analyzers;

namespace BizHawk.SrcGen.ServiceProps;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class ServicePropsGenerator : ISourceGenerator
{
	private sealed class ServicePropsGenSyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<TypeDeclarationSyntax> Candidates = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is TypeDeclarationSyntax { AttributeLists.Count: > 0 } syn) Candidates.Add(syn);
		}
	}

	private record struct AttrData(string TypeIdent, string PropName, bool IsOptional);

	public void Initialize(GeneratorInitializationContext context)
		=> context.RegisterForSyntaxNotifications(static () => new ServicePropsGenSyntaxReceiver());

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxReceiver is not ServicePropsGenSyntaxReceiver receiver) return;
		var entryAttrSymbol = context.Compilation.GetTypeByMetadataName("BizHawk.Emulation.Common.GenEmuServicePropAttribute");
		if (entryAttrSymbol is null) return; // project does not have BizHawk.Emulation.Common dependency
		foreach (var cSym in receiver.Candidates.Select(tds => context.Compilation.GetSemanticModel(tds.SyntaxTree).GetDeclaredSymbol(tds)!)
			.Where(static cSym => SymbolEqualityComparer.Default.Equals(cSym.ContainingSymbol, cSym.ContainingNamespace))) // only top-level types can be partial
		{
			var entries = cSym.GetAttributes().Where(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, entryAttrSymbol))
				.Select(static entry =>
				{
					const string PFX_EMU_COMMON = "BizHawk.Emulation.Common.";
					var typeIdent = ((INamedTypeSymbol) entry.ConstructorArguments[0].Value!).FullNamespace();
					return new AttrData(
						TypeIdent: typeIdent.RemovePrefix(PFX_EMU_COMMON),
						PropName: (string) entry.ConstructorArguments[1].Value!,
						IsOptional: (entry.NamedArguments.FirstOrNull(static kvp => kvp.Key is "IsOptional")?.Value)?.Value is true);
				})
				.ToList();
			if (entries.Count is 0) continue;
			var nSpace = cSym.ContainingNamespace.ToDisplayString();
			var classWithKeyword = $"{(cSym.IsValueType ? "struct" : "class")} {cSym.Name}";
			Dictionary<string, string> innerText = new();
			foreach (var ad in entries)
			{
				if (ad.IsOptional)
				{
					innerText[ad.PropName] = $@"[OptionalService]
		public {ad.TypeIdent}? {ad.PropName}
		{{
			get;
			[Obsolete(GenEmuServicePropAttribute.SETTER_DEPR_MSG)] private set;
		}} = null;";
				}
				else
				{
					var backingIdent = $"_maybe{ad.PropName}";
					innerText[backingIdent] = $@"[RequiredService]
		public {ad.TypeIdent}? {backingIdent}
		{{
			[Obsolete(""use "" + nameof({ad.PropName}))] get;
			[Obsolete(GenEmuServicePropAttribute.SETTER_DEPR_MSG)] private set;
		}} = null;";
					innerText[ad.PropName] = $"private {ad.TypeIdent} {ad.PropName}\n\t\t\t=> {backingIdent}!;";
				}
			}
			context.AddSource(
				$"{cSym.Name}.ServiceProps.cs",
				SourceText.From($@"#nullable enable
#pragma warning disable CS0618

using System;

using BizHawk.Emulation.Common;

namespace {nSpace}
{{
	partial {classWithKeyword}
	{{
		{string.Join("\n\n\t\t", innerText.OrderBy(static kvp => kvp.Key).Select(static kvp => kvp.Value))}
	}}
}}
",
				Encoding.UTF8));
		}
	}
}
