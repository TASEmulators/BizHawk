namespace BizHawk.SrcGen.SettingsUtil;

using System.Collections.Immutable;
using System.Linq;
using System.Text;

using BizHawk.Analyzers;

using TestType = (TypeDeclarationSyntax TDS, SemanticModel SemanticModel);

[Generator]
public class DefaultSetterGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classDecls = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax or RecordDeclarationSyntax,
			transform: static (ctx, _) => ((TypeDeclarationSyntax) ctx.Node, ctx.SemanticModel));
		context.RegisterSourceOutput(context.CompilationProvider.Combine(classDecls.Collect()), Execute);
	}

	private static void CreateDefaultSetter(StringBuilder source, ITypeSymbol symbol, bool isCLSCompliant)
	{
		var props = symbol
			.GetMembers()
			.Where(m => m.Kind == SymbolKind.Property)
			.ToImmutableArray();

		source.Append($@"
		{(isCLSCompliant ? string.Empty : "[CLSCompliant(false)] ")}public static void SetDefaultValues({symbol} settings)
		{{");

		foreach (var prop in props)
		{
			var defaultValueAttribute = prop
				.GetAttributes()
				.FirstOrDefault(
					a => a.AttributeClass?.Name == "DefaultValueAttribute");

			var ctorArgs = defaultValueAttribute?.ConstructorArguments;
			if (!ctorArgs.HasValue)
			{
				continue;
			}

			switch (ctorArgs.Value.Length)
			{
				case 1:
					// this single arg is just the value assigned to the default value
					var arg = ctorArgs.Value[0];
					// a bit lame, but it'll work
					// TODO: do we even want to handle arrays? we don't even have arrays in default values...
					var converionStr = arg.Kind == TypedConstantKind.Array
						? $"new {arg.Type}[] " // new T[]
						: ""; // do we need a cast (i.e. (T)) here? probably not?
					source.Append($@"
			settings.{prop.Name} = {converionStr}{arg.ToCSharpString()};");
					break;
				case 2:
					// first arg is the type, the second arg is a string which converts it
					source.Append($@"
			settings.{prop.Name} = ({ctorArgs.Value[0].Value})System.ComponentModel.TypeDescriptor
									.GetConverter({ctorArgs.Value[0].ToCSharpString()})
									.ConvertFromInvariantString({ctorArgs.Value[1].ToCSharpString()});");
					break;
			}
		}

		source.Append(@"
		}
");
	}

	private static void Execute(
		SourceProductionContext context,
		(Compilation Compilation, ImmutableArray<TestType> ClassDeclarations) value)
	{
		var (compilation, classDeclarations) = value;
		var consumerAttrSym = compilation.GetTypeByMetadataName("BizHawk.Emulation.Common.CoreSettingsAttribute");
		if (consumerAttrSym is null) return;
		var clsCompliantAttrSym = compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName!)!;

		// Generated source code
		var source = new StringBuilder(@"
namespace BizHawk.Emulation.Cores
{
	public static partial class SettingsUtil
	{");

		foreach (var symbol in classDeclarations
			.Where(tuple => tuple.TDS.AttributeLists.Matching(
				consumerAttrSym,
				tuple.SemanticModel,
				context.CancellationToken).Any())
			.Select(tuple => tuple.SemanticModel.GetDeclaredSymbol(tuple.TDS, context.CancellationToken))
			.OfType<ITypeSymbol>())
		{
			CreateDefaultSetter(source, symbol, symbol.GetIsCLSCompliant(clsCompliantAttrSym) ?? true);
		}

		source.Append(@"
	}
}");

		// Add the source code to the compilation
		context.AddSource("DefaultSetters.g.cs", source.ToString());
	}
}
