namespace BizHawk.SrcGen.VIM;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Analyzers;
using BizHawk.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using ImplNotesList = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<BizHawk.SrcGen.VIM.VIMGenerator.ImplNotes>>;

[Generator]
public sealed class VIMGenerator : ISourceGenerator
{
	internal readonly struct ImplNotes
	{
		public readonly string AccessorKeyword;

		public readonly string BaseImplNamePrefix;

		public readonly string InvokeCall;

		public readonly bool IsSetOrRemove
			=> MethodSym.MethodKind is MethodKind.PropertySet or MethodKind.EventRemove;

		public readonly string MemberFullNameArgs;

		public readonly IMethodSymbol MethodSym;

		public readonly string ReturnType;

		public ImplNotes(IMethodSymbol methodSym, string memberFullNameArgs, string baseImplNamePrefix)
		{
			BaseImplNamePrefix = baseImplNamePrefix;
			MemberFullNameArgs = memberFullNameArgs;
			MethodSym = methodSym;
			switch (methodSym.MethodKind)
			{
				case MethodKind.Ordinary:
					AccessorKeyword = string.Empty;
					InvokeCall = $"(this{string.Concat(methodSym.Parameters.Select(static pSym => $", {pSym.Name}"))})";
					MemberFullNameArgs += $"({string.Join(", ", methodSym.Parameters.Select(static pSym => $"{pSym.Type.ToDisplayString()} {pSym.Name}"))})";
					ReturnType = methodSym.ReturnType.ToDisplayString();
					break;
				case MethodKind.PropertyGet:
					AccessorKeyword = "get";
					InvokeCall = "(this)";
					ReturnType = methodSym.ReturnType.ToDisplayString();
					break;
				case MethodKind.PropertySet:
					AccessorKeyword = "set";
					InvokeCall = "(this, value)";
					ReturnType = ((IPropertySymbol) methodSym.AssociatedSymbol!).Type.ToDisplayString(); // only used for set-only props
					break;
				case MethodKind.EventAdd:
					AccessorKeyword = "add";
					InvokeCall = "(this, value)";
					ReturnType = $"event {((IEventSymbol) methodSym.AssociatedSymbol!).Type.ToDisplayString()}";
					break;
				case MethodKind.EventRemove:
					AccessorKeyword = "remove";
					InvokeCall = "(this, value)";
					ReturnType = string.Empty; // unused
					break;
				default:
					throw new InvalidOperationException();
			}
			if (!string.IsNullOrEmpty(AccessorKeyword)) BaseImplNamePrefix += $"_{AccessorKeyword}";
		}
	}

	private sealed class VIMGenSyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<TypeDeclarationSyntax> Candidates = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is TypeDeclarationSyntax syn) Candidates.Add(syn);
		}
	}

	private static readonly DiagnosticDescriptor DiagCantMakeVirtual = new(
		id: "BHI2000",
		title: "Only apply [VirtualMethod] to (abstract) methods and property/event accessors",
		messageFormat: "Can't apply [VirtualMethod] to this kind of member, only methods and property/event accessors",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

#if false
	private static readonly DiagnosticDescriptor DiagDebug = new(
		id: "BHI2099",
		title: "debug",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);
#endif

	//TODO warning for attr used on member of class/struct/record?

	//TODO warning for only one of get/set/add/remove pair has attr?

	//TODO warning for unused base implementation (i.e. impl/override exists in every direct implementor)? ofc the attribute can be pointing to any static method, so the base implementation itself shouldn't be marked unused

	public void Initialize(GeneratorInitializationContext context)
		=> context.RegisterForSyntaxNotifications(static () => new VIMGenSyntaxReceiver());

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxReceiver is not VIMGenSyntaxReceiver receiver) return;

		// boilerplate to get attr working
		var compilation = context.Compilation;
		var vimAttrSymbol = compilation.GetTypeByMetadataName("BizHawk.Common." + nameof(VirtualMethodAttribute));
		if (vimAttrSymbol is null)
		{
			var attributesSource = SourceText.From(typeof(VIMGenerator).Assembly.GetManifestResourceStream("BizHawk.SrcGen.VIM.VirtualMethodAttribute.cs")!, Encoding.UTF8, canBeEmbedded: true);
			context.AddSource("VirtualMethodAttribute.cs", attributesSource);
			compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(attributesSource, (CSharpParseOptions) ((CSharpCompilation) context.Compilation).SyntaxTrees[0].Options));
			vimAttrSymbol = compilation.GetTypeByMetadataName("BizHawk.Common." + nameof(VirtualMethodAttribute))!;
		}

		Dictionary<string, ImplNotesList> vimDict = new();
		ImplNotesList Lookup(INamedTypeSymbol intfSym)
		{
			var fqn = intfSym.FullNamespace();
			if (vimDict.TryGetValue(fqn, out var implNotes)) return implNotes;
			// else cache miss
			ImplNotesList implNotes1 = new();
			static (string? ImplsClassFullName, string? BaseImplMethodName) ParseVIMAttr(AttributeData vimAttr)
			{
				string? baseImplMethodName = null;
				string? implsClassFullName = null;
				foreach (var kvp in vimAttr.NamedArguments) switch (kvp.Key)
				{
					case nameof(VirtualMethodAttribute.BaseImplMethodName):
						baseImplMethodName = kvp.Value.Value?.ToString();
						break;
					case nameof(VirtualMethodAttribute.ImplsClassFullName):
						implsClassFullName = kvp.Value.Value?.ToString();
						break;
				}
				return (implsClassFullName, baseImplMethodName);
			}
			void AddMethodNotes(IMethodSymbol methodSym, (string? ImplsClassFullName, string? BaseImplMethodName) attrProps)
			{
				var memberName = methodSym.MethodKind is MethodKind.Ordinary ? methodSym.Name : methodSym.AssociatedSymbol!.Name;
				var memberFullNameArgs = $"{intfSym.FullNamespace()}.{memberName}";
				var baseImplNamePrefix = $"{(attrProps.ImplsClassFullName ?? $"{intfSym.FullNamespace()}.MethodDefaultImpls")}.{attrProps.BaseImplMethodName ?? memberName}";
				if (!implNotes1.TryGetValue(memberFullNameArgs, out var parts)) parts = implNotes1[memberFullNameArgs] = new();
				parts.Add(new(methodSym, memberFullNameArgs: memberFullNameArgs, baseImplNamePrefix: baseImplNamePrefix));
			}
			foreach (var memberSym in intfSym.GetMembers())
			{
				var vimAttr = memberSym.GetAttributes().FirstOrDefault(ad => vimAttrSymbol.Matches(ad.AttributeClass));
				switch (memberSym)
				{
					case IMethodSymbol methodSym: // methods and prop accessors (accessors in interface events are an error without DIM)
						if (vimAttr is null) continue;
						if (methodSym.MethodKind is not (MethodKind.Ordinary or MethodKind.PropertyGet or MethodKind.PropertySet))
						{
							// no idea what would actually trigger this
							context.ReportDiagnostic(Diagnostic.Create(DiagCantMakeVirtual, vimAttr.ApplicationSyntaxReference!.GetSyntax().GetLocation()));
							continue;
						}
						AddMethodNotes(methodSym, ParseVIMAttr(vimAttr));
						continue;
					case IPropertySymbol propSym: // props
						if (vimAttr is null) continue;
						var parsed = ParseVIMAttr(vimAttr);
						if (propSym.GetMethod is {} getter) AddMethodNotes(getter, parsed);
						if (propSym.SetMethod is {} setter) AddMethodNotes(setter, parsed);
						continue;
					case IEventSymbol eventSym: // events
						if (vimAttr is null) continue;
						var parsed1 = ParseVIMAttr(vimAttr);
						AddMethodNotes(eventSym.AddMethod!, parsed1);
						AddMethodNotes(eventSym.RemoveMethod!, parsed1);
						continue;
				}
			}

			return vimDict[fqn] = implNotes1;
		}

		List<INamedTypeSymbol> seen = new();
		foreach (var tds in receiver.Candidates)
		{
			var cSym = compilation.GetSemanticModel(tds.SyntaxTree).GetDeclaredSymbol(tds)!;
			if (seen.Contains(cSym)) continue; // dedup partial classes
			seen.Add(cSym);
			var typeKeywords = tds.GetTypeKeywords(cSym);
			if (typeKeywords.Contains("enum") || typeKeywords.Contains("interface") || typeKeywords.Contains("static")) continue;

			var nSpace = cSym.ContainingNamespace.ToDisplayString();
			var nSpaceDot = $"{nSpace}.";
			List<string> innerText = new();
			var intfsToImplement = cSym.BaseType is not null
				? cSym.AllInterfaces.Except(cSym.BaseType.AllInterfaces) // superclass (or its superclass, etc.) already has the delegated base implementations of these interfaces' virtual methods
				: cSym.AllInterfaces;
			//TODO let an interface override a superinterface's virtual method -- may need to order intfsToImplement somehow
			foreach (var methodParts in intfsToImplement.SelectMany(intfSym => Lookup(intfSym).Values))
			{
				var methodSym = methodParts[0].MethodSym;
				if (cSym.FindImplementationForInterfaceMember(methodSym) is not null) continue; // overridden
				var memberImplText = $"{methodParts[0].ReturnType} {methodParts[0].MemberFullNameArgs.RemovePrefix(nSpaceDot)}";
				if (methodSym.MethodKind is MethodKind.Ordinary)
				{
					memberImplText += $"\n\t\t\t=> {methodParts[0].BaseImplNamePrefix.RemovePrefix(nSpaceDot)}{methodParts[0].InvokeCall};";
				}
				else
				{
					if (methodParts[0].IsSetOrRemove) methodParts.Reverse();
					memberImplText += $"\n\t\t{{{string.Concat(methodParts.Select(methodNotes => $"\n\t\t\t{methodNotes.AccessorKeyword} => {methodNotes.BaseImplNamePrefix.RemovePrefix(nSpaceDot)}{methodNotes.InvokeCall};"))}\n\t\t}}";
				}
				innerText.Add(memberImplText);
			}
			if (innerText.Count is not 0) context.AddSource(
				source: $@"#nullable enable

namespace {nSpace}
{{
	public {string.Join(" ", typeKeywords)} {cSym.Name}
	{{
		{string.Join("\n\n\t\t", innerText)}
	}}
}}
",
				hintName: $"{cSym.Name}.VIMDelegation.cs");
		}
	}
}
