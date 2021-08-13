using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BizHawk.SrcGen.PeripheralOption
{
	[Generator]
	public sealed class PeripheralOptionGenerator : ISourceGenerator
	{
		private static readonly DiagnosticDescriptor DiagNoEnum = new(
			id: "BHI3800",
			title: "Apply [PeripheralOptionEnum] to enums used with generator",
			messageFormat: "Matching enum should have [PeripheralOptionEnum] to enable better analysis and codegen",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor DiagMustBeNonNegative = new(
			id: "BHI3801",
			title: "Underlying value of [PeripheralOptionEnum] members cannot be negative (the underlying type may be signed)",
			messageFormat: "One or more members of {0} have an underlying value < 0",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor DiagEnumMemberConflict = new(
			id: "BHI3802",
			title: "Underlying values of [PeripheralOptionEnum] members should be unique",
			messageFormat: "Underlying value of {0}.{1} conflicts with another enum member",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor DiagNoImplForOption = new(
			id: "BHI3803",
			title: "Create a [PeripheralOptionImpl] for each [PeripheralOptionEnum] member",
			messageFormat: "No impl was found for {0}.{1}",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor DiagDuplicateImpl = new(
			id: "BHI3804",
			title: "Create only one [PeripheralOptionImpl] for each [PeripheralOptionEnum] member",
			messageFormat: "Multiple impls found for {0}.{1}",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor DiagUnused = new(
			id: "BHI3805",
			title: "Finish implementation of [PeripheralOptionConsumer]",
			messageFormat: "Couldn't find matching [PeripheralOptionConsumer] for [PeripheralOption{0}] {1}",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		private sealed class PeripheralOptionAnnotatedGroup
		{
			public readonly List<(BaseTypeDeclarationSyntax Decl, INamedTypeSymbol Symbol, string ImplTypeFullName, string DefaultOptionFullName)> Consumers = new();

			public (EnumDeclarationSyntax Decl, INamedTypeSymbol Symbol)? Enum = null;

			public readonly List<(BaseTypeDeclarationSyntax Decl, string TypeFullName, string OptionFullName)> Impls = new();
		}

		private sealed class PeripheralOptionGenSyntaxReceiver : ISyntaxReceiver
		{
			public readonly List<BaseTypeDeclarationSyntax> Candidates = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode is BaseTypeDeclarationSyntax syn && syn.AttributeLists.Count > 0) Candidates.Add(syn);
			}
		}

		public void Initialize(GeneratorInitializationContext context)
			=> context.RegisterForSyntaxNotifications(static () => new PeripheralOptionGenSyntaxReceiver());

		public void Execute(GeneratorExecutionContext context)
		{
			static string RemovePrefix(string str, string prefix)
				=> str.StartsWith(prefix) ? str.Substring(prefix.Length, str.Length - prefix.Length) : str;
			static string SubstringAfterLast(string str, char delimiter)
			{
				var index = str.LastIndexOf(delimiter);
				return index < 0 ? str : str.Substring(index + 1, str.Length - index - 1);
			}
			static string SubstringAfterLastPeriod(string str)
				=> SubstringAfterLast(str, '.');

			// boilerplate to get attrs working
			var attributesSource = SourceText.From(typeof(PeripheralOptionGenerator).Assembly.GetManifestResourceStream("BizHawk.SrcGen.PeripheralOption.PeripheralOptionAttributes.cs")!, Encoding.UTF8, canBeEmbedded: true);
			context.AddSource("PeripheralOptionAttributes", attributesSource);
			if (context.SyntaxReceiver is not PeripheralOptionGenSyntaxReceiver receiver) return;

			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(attributesSource, (CSharpParseOptions) ((CSharpCompilation) context.Compilation).SyntaxTrees[0].Options));
			var consumerAttrSymbol = compilation.GetTypeByMetadataName("BizHawk.SrcGen.PeripheralOption." + nameof(PeripheralOptionConsumerAttribute))!;
			var enumAttrSymbol = compilation.GetTypeByMetadataName("BizHawk.SrcGen.PeripheralOption." + nameof(PeripheralOptionEnumAttribute))!;
			var implAttrSymbol = compilation.GetTypeByMetadataName("BizHawk.SrcGen.PeripheralOption." + nameof(PeripheralOptionImplAttribute))!;

			// group type declarations together
			static AttributeData? FirstOfTypeOrNull(INamedTypeSymbol attrSymbol, IReadOnlyCollection<AttributeData> attrs)
				=> attrs.FirstOrDefault(ad => ad.AttributeClass!.Equals(attrSymbol, SymbolEqualityComparer.Default));
			static string TypeNameFromArg(int paramIndex, AttributeData ad)
				=> ((INamedTypeSymbol) ad.ConstructorArguments[paramIndex].Value!).ToDisplayString();
			Dictionary<string, PeripheralOptionAnnotatedGroup> groups = new();
			PeripheralOptionAnnotatedGroup GroupFor(string typeName)
				=> groups.TryGetValue(typeName, out var g) ? g : (groups[typeName] = new());
			foreach (var tds in receiver.Candidates)
			{
				var typeSym = compilation.GetSemanticModel(tds.SyntaxTree).GetDeclaredSymbol(tds)!;
				var attrs = typeSym.GetAttributes();
				if (FirstOfTypeOrNull(enumAttrSymbol, attrs) is not null)
				{
					if (tds is not EnumDeclarationSyntax eds) continue;
					GroupFor(typeSym.ToDisplayString()).Enum = (eds, typeSym);
				}
				else if (FirstOfTypeOrNull(implAttrSymbol, attrs) is {} implAttr)
				{
					if (implAttr.ConstructorArguments.Length is not 2) continue;
					GroupFor(TypeNameFromArg(0, implAttr)).Impls.Add((
						tds,
						typeSym.ToDisplayString(),
						implAttr.ConstructorArguments[1].ToCSharpString()));
				}
				else if (FirstOfTypeOrNull(consumerAttrSymbol, attrs) is {} consumerAttr)
				{
					if (consumerAttr.ConstructorArguments.Length is not 3) continue;
					GroupFor(TypeNameFromArg(0, consumerAttr)).Consumers.Add((
						tds,
						typeSym,
						TypeNameFromArg(1, consumerAttr),
						consumerAttr.ConstructorArguments[2].ToCSharpString()));
				}
			}

			// check that everything was found, and generate source
			static IReadOnlyList<(string Identifier, ulong RawValue)> ReadEnumMembersMaxFirst(EnumDeclarationSyntax enumDecl)
				=> enumDecl.ChildNodes().Where(static node => node.IsKind(SyntaxKind.EnumMemberDeclaration))
					.Select(static node => (
						Identifier: node.ChildTokens().First().ToString(),
						RawValue: ulong.Parse(node.ChildNodes().First(static node1 => node1.IsKind(SyntaxKind.EqualsValueClause)).ChildNodes().First().ToString())))
					.OrderByDescending(static tuple => tuple.RawValue)
					.ToList();
			foreach (var kvp in groups)
			{
				var group = kvp.Value;
				if (group.Consumers.Count is 0)
				{
					if (group.Enum is not null)
					{
						var (enumDecl, enumSym) = group.Enum.Value;
						context.ReportDiagnostic(Diagnostic.Create(DiagUnused, enumDecl.GetLocation(), "Enum", enumSym.Name));
					}
					foreach (var (implDecl, implTypeName, _) in group.Impls)
					{
						context.ReportDiagnostic(Diagnostic.Create(DiagUnused, implDecl.GetLocation(), "Impl", SubstringAfterLastPeriod(implTypeName)));
					}
					continue;
				}
				(IReadOnlyList<string?> ImplTypeFullNamesIndexed, bool ListContainsNull, string EnumRawTypeName)? advancedProcessing = null;
				if (group.Enum is not null)
				{
					var implsByOption = group.Impls.Select(static impl => (OptionName: SubstringAfterLastPeriod(impl.OptionFullName), Impl: impl)).ToList();
					var (enumDecl, enumSymbol) = group.Enum.Value;
					var enumRawTypeName = enumSymbol.EnumUnderlyingType!.Name; // type name, not keyword, but that's okay because System.* is imported
					IReadOnlyList<(string Identifier, ulong RawValue)> membersMaxFirst;
					try
					{
						membersMaxFirst = ReadEnumMembersMaxFirst(enumDecl);
					}
					catch (SystemException e) when (e is FormatException || e is OverflowException)
					{
						context.ReportDiagnostic(Diagnostic.Create(DiagMustBeNonNegative, enumDecl.GetLocation(), SubstringAfterLastPeriod(kvp.Key)));
						continue;
					}
					var memberNamesIndexed = new string?[membersMaxFirst[0].RawValue + 1UL]; // length of max + 1 means the max, and all lower values, are valid "keys" for the generated array
					foreach (var member in membersMaxFirst)
					{
						if (memberNamesIndexed[member.RawValue] is null) memberNamesIndexed[member.RawValue] = member.Identifier;
						else context.ReportDiagnostic(Diagnostic.Create(DiagEnumMemberConflict, enumDecl.GetLocation(), SubstringAfterLastPeriod(kvp.Key), member.Identifier));
					}
					var listContainsNull = false;
					List<string?> implTypeFullNamesIndexed = new();
					foreach (var memberName in memberNamesIndexed)
					{
						if (memberName is null)
						{
							listContainsNull = true;
							implTypeFullNamesIndexed.Add(null);
							continue;
						}
						var i = implsByOption.FindIndex(tuple => tuple.OptionName == memberName);
						if (i < 0)
						{
							context.ReportDiagnostic(Diagnostic.Create(DiagNoImplForOption, enumDecl.GetLocation(), SubstringAfterLastPeriod(kvp.Key), memberName));
							listContainsNull = true;
							implTypeFullNamesIndexed.Add(null);
							continue;
						}
						var impl = implsByOption[i].Impl;
						implsByOption.RemoveAt(i);
						implTypeFullNamesIndexed.Add(impl.TypeFullName);
					}
					foreach (var tuple in implsByOption)
					{
						context.ReportDiagnostic(Diagnostic.Create(DiagDuplicateImpl, tuple.Impl.Decl.GetLocation(), SubstringAfterLastPeriod(kvp.Key), tuple.OptionName));
					}
					advancedProcessing = (implTypeFullNamesIndexed, listContainsNull, enumRawTypeName);
				}
				foreach (var consumer in group.Consumers)
				{
					var (consumerDecl, consumerSym, supertypeFullName, defaultOptionFullName) = consumer;
					var nSpace = consumerSym.ContainingNamespace.ToDisplayString();
					var classWithKeyword = $"{(consumerSym.IsValueType ? "struct" : "class")} {consumerSym.Name}";
					string RemoveNSPrefix(string str)
						=> RemovePrefix(str, $"{nSpace}.");
					var enumTypeName = RemoveNSPrefix(kvp.Key);
					var supertypeName = RemoveNSPrefix(supertypeFullName);
					var ctorFTypeName = $"Func<int, {supertypeName}>";
					string innerSrc;
					if (advancedProcessing is not null)
					{
						var (implTypeFullNamesIndexed, listContainsNull, enumRawTypeName) = advancedProcessing.Value;
						var ctorArrayEntries = string.Join("\n\t\t\t", implTypeFullNamesIndexed.Select(s => s is null ? "null," : $"static portNum => new {RemoveNSPrefix(s)}(portNum),"));
						innerSrc = $@"private static readonly IReadOnlyList<{ctorFTypeName}{(listContainsNull ? "?" : string.Empty)}> _controllerCtors = new {ctorFTypeName}{(listContainsNull ? "?" : string.Empty)}[]
		{{
			{ctorArrayEntries}
		}};

#pragma warning disable SA1121 // cast to enum base type doesn't use keyword
		private static {ctorFTypeName} CtorFor({enumTypeName} option) => _controllerCtors[({enumRawTypeName}) option]{(listContainsNull ? "!" : string.Empty)};
#pragma warning restore SA1121";
					}
					else
					{
						context.ReportDiagnostic(Diagnostic.Create(DiagNoEnum, consumerDecl.GetLocation()));
						var ctorDictEntries = string.Join("\n\t\t\t",group.Impls.Select(impl => $"[{RemoveNSPrefix(impl.OptionFullName)}] = static portNum => new {RemoveNSPrefix(impl.TypeFullName)}(portNum),"));
						innerSrc = $@"private static readonly IReadOnlyDictionary<{enumTypeName}, {ctorFTypeName}> _controllerCtors = new Dictionary<{enumTypeName}, {ctorFTypeName}>
		{{
			{ctorDictEntries}
		}};

		private static {ctorFTypeName} CtorFor({enumTypeName} option) => _controllerCtors[option];";
					}
					var src = $@"#nullable enable

using System;
using System.Collections.Generic;

namespace {nSpace}
{{
	public partial {classWithKeyword}
	{{
		public const {enumTypeName} DEFAULT_PERIPHERAL_OPTION = {RemoveNSPrefix(defaultOptionFullName)};

		{innerSrc}
	}}
}}
";
					context.AddSource($"{consumerSym.Name}.PeripheralOption.cs", SourceText.From(src, Encoding.UTF8));
				}
			}
		}
	}
}
