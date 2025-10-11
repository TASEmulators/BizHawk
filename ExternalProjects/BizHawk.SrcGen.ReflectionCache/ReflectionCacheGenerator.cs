namespace BizHawk.SrcGen.ReflectionCache;

using System.Text;

[Generator]
public sealed class ReflectionCacheGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> context.RegisterSourceOutput(
			context.AnalyzerConfigOptionsProvider.Combine(context.ParseOptionsProvider).Combine(context.CompilationProvider),
			Execute);

	public void Execute(
		SourceProductionContext context,
		((AnalyzerConfigOptionsProvider Config, ParseOptions ParseOptions) More, Compilation Compilation) providers)
	{
		if (!providers.More.Config.GlobalOptions.TryGetValue("build_property.RootNamespace", out var nSpace)
			|| string.IsNullOrWhiteSpace(nSpace)
			|| providers.Compilation.GetTypeByMetadataName("BizHawk.Common.StringExtensions.StringExtensions") is null) // project does not have BizHawk.Common dependency TODO revisit w/ codegen'd kitchen sink
		{
			return;
		}
		var className = "ReflectionCache";
		var langVersion = (providers.More.ParseOptions as CSharpParseOptions)?.LanguageVersion ?? LanguageVersion.Default;
		if (langVersion >= LanguageVersion.CSharp10)
		{
			_ = providers.More.Config.GlobalOptions.TryGetValue("build_property.MSBuildProjectName", out var projName);
			projName ??= nSpace;
			foreach (var chunk in projName.Split('.'))
			{
				className += $"_{(chunk.Length <= 3 ? chunk : chunk.Substring(0, 3))}";
			}
		}
		var src = $@"#nullable enable
{(langVersion >= LanguageVersion.CSharp10 ? $"\nglobal using ReflectionCache = {nSpace}.{className};\n" : string.Empty)}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
{(nSpace == "BizHawk.Common" ? string.Empty : "\nusing BizHawk.Common;")}
using BizHawk.Common.StringExtensions;

namespace {nSpace}
{{
	public static class {className}
	{{
		private const string EMBED_PREFIX = ""{nSpace}."";

		private static Type[]? _types = null;

		private static readonly Assembly Asm = typeof({nSpace}.{className}).Assembly;

		public static readonly Version AsmVersion = Asm.GetName().Version!;

		public static Type[] Types => _types ??= Asm.GetTypesWithoutLoadErrors().ToArray();

		public static IEnumerable<string> EmbeddedResourceList(string extraPrefix)
		{{
			var fullPrefix = EMBED_PREFIX + extraPrefix;
			return Asm.GetManifestResourceNames().Where(s => s.StartsWithOrdinal(fullPrefix)) // seems redundant with `RemovePrefix`, but we only want these in the final list
				.Select(s => s.RemovePrefix(fullPrefix));
		}}

		public static IEnumerable<string> EmbeddedResourceList()
			=> EmbeddedResourceList(string.Empty); // can't be simplified to `Asm.GetManifestResourceNames` call

		/// <exception cref=""ArgumentException"">not found</exception>
		public static Stream EmbeddedResourceStream(string embedPath)
		{{
			var fullPath = EMBED_PREFIX + embedPath;
			return Asm.GetManifestResourceStream(fullPath)
				?? throw new ArgumentException(paramName: nameof(embedPath), message: $""resource at {{fullPath}} not found"");
		}}
	}}
}}
";
		context.AddSource("ReflectionCache.cs", SourceText.From(src, Encoding.UTF8));
	}
}
