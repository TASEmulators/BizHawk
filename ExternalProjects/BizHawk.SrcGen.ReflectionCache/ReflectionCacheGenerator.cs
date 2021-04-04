using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BizHawk.SrcGen.ReflectionCache
{
	[Generator]
	public sealed class ReflectionCacheGenerator : ISourceGenerator
	{
		private sealed class ReflectionCacheGenSyntaxReceiver : ISyntaxReceiver
		{
			/// <remarks>
			/// I may have just added RNG to the build process...
			/// Increase this sample size to decrease chance of random failure.
			/// Alternatively, if you can come up with a better way of getting the project name in <see cref="ISourceGenerator.Execute"/> (I tried like 5 different things), do that instead.
			/// --yoshi
			/// </remarks>
			private const int SAMPLE_SIZE = 20;

			private string? _namespace;

			private readonly List<string> _namespaces = new();

			public string Namespace => _namespace ??= CalcNamespace();

			private string CalcNamespace()
			{
				// black magic wizardry to find common prefix https://stackoverflow.com/a/35081977
				var ns = new string(_namespaces.First()
					.Substring(0, _namespaces.Min(s => s.Length))
					.TakeWhile((c, i) => _namespaces.All(s => s[i] == c))
					.ToArray());
				return ns[ns.Length - 1] == '.' ? ns.Substring(0, ns.Length - 1) : ns; // trim trailing '.' (can't use BizHawk.Common from Source Generators)
			}

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				static string Ser(NameSyntax nameSyn) => nameSyn switch
				{
					SimpleNameSyntax simple => simple.Identifier.ValueText,
					QualifiedNameSyntax qual => $"{Ser(qual.Left)}.{Ser(qual.Right)}",
					_ => throw new Exception()
				};
				if (_namespace != null || syntaxNode is not NamespaceDeclarationSyntax syn) return;
				var newNS = Ser(syn.Name);
				if (!newNS.StartsWith("BizHawk.")) return;
				_namespaces.Add(newNS);
				if (_namespaces.Count == SAMPLE_SIZE) _namespace = CalcNamespace();
			}
		}

		public void Initialize(GeneratorInitializationContext context)
			=> context.RegisterForSyntaxNotifications(() => new ReflectionCacheGenSyntaxReceiver());

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is not ReflectionCacheGenSyntaxReceiver receiver) return;
			var nSpace = receiver.Namespace;
			if (nSpace == null) return;
			var extraImports = nSpace == "BizHawk.Common" ? string.Empty : "\nusing BizHawk.Common;";
			var src = $@"#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
{extraImports}

namespace {nSpace}
{{
	public static class ReflectionCache
	{{
		private static readonly Assembly Asm = typeof({nSpace}.ReflectionCache).Assembly;

		public static readonly Version AsmVersion = Asm.GetName().Version!;

		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypesWithoutLoadErrors().ToArray());

		public static Type[] Types => _types.Value;

		/// <exception cref=""ArgumentException"">not found</exception>
		public static Stream EmbeddedResourceStream(string embedPath)
		{{
			var fullPath = $""{nSpace}.{{embedPath}}"";
			return Asm.GetManifestResourceStream(fullPath) ?? throw new ArgumentException(""resource at {{fullPath}} not found"", nameof(embedPath));
		}}
	}}
}}
";
			context.AddSource("ReflectionCache.cs", SourceText.From(src, Encoding.UTF8));
		}
	}
}
