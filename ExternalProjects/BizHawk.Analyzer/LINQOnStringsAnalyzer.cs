namespace BizHawk.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LINQOnStringsAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor DiagLINQOnStrings = new(
		id: "BHI3102",
		title: "Prefer specialised methods over LINQ on string receivers",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagLINQOnStrings);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			const string CHAR_ZERO_WARNING = " (but did you intend to get `default(char)`?)";
			static string DisambigNameFor(IMethodSymbol sym)
				=> $"{sym.Name}#{sym.Parameters.Length}";
//			var hasHawkStringExtensions = initContext.Compilation.GetTypeByMetadataName("BizHawk.Common.StringExtensions.StringExtensions") is not null;
			var hasStringContainsChar = initContext.Compilation.GetSpecialType(SpecialType.System_String)
				.GetMembers("Contains").Length > 1;
			var linqMethodSyms = initContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")!.GetMembers();
			Dictionary<string, (IMethodSymbol SymToMatch, DiagnosticSeverity Level, string MsgFmtStr)> banned = new();
			foreach (var (identifier, msgFmtStr) in new[]
			{
				("Append", "Use concatenation or interpolation"),
				("Concat", "Use concatenation operator"), // we have special handling for when the "other" string is only `IEnumerable<char>`
				("ElementAt", "Use `({0})[i]`"),
				("ElementAtOrDefault", "Use `i < str.Length ? str[i] : default`" + CHAR_ZERO_WARNING),
				("Prepend", "Use concatenation or interpolation"),
				("Skip", "Use `({0}).Substring(offset)` (or `.AsSpan(offset)`)"),
				("Take", "Use `({0}).Substring(0, count)` (or `.AsSpan(0, count)`)"),
				("ToArray", "Use `({0}).ToCharArray()`"),
			})
			{
				// only one overload, simply add to list
				var foundSym = linqMethodSyms.OfType<IMethodSymbol>().FirstOrDefault(sym => sym.Name == identifier);
				if (foundSym is not null) banned[DisambigNameFor(foundSym)] = (foundSym, DiagnosticSeverity.Warning, msgFmtStr);
			}
			foreach (var (identifier, msgFmtStr) in new[]
			{
				("Any", "Use `({0}).Length is not 0`"),
				("Count", "Use `({0}).Length`"),
				("First", "Use `({0})[0]`"),
				("FirstOrDefault", "Use `str.Length is 0 ? default : str[0]`" + CHAR_ZERO_WARNING),
				("Last", "Use `({0})[^1]`"),
				("LastOrDefault", "Use `str.Length is 0 ? default : str[^1]`" + CHAR_ZERO_WARNING),
				("LongCount", "Use `(long) ({0}).Length` (`String` cannot exceed `int.MaxValue` chars)"),
				("Single", "Use `str[0]`, asserting `str.Length is 1` beforehand if desired"),
				("SingleOrDefault", "Use `str.Length is 1 ? str[0] : default`" + CHAR_ZERO_WARNING),
			})
			{
				// for these, the overload with a delegate param (combined `Where`) is allowed, and the overload without is banned
				var foundSym = linqMethodSyms.OfType<IMethodSymbol>()
					.FirstOrDefault(sym => sym.Parameters.Length is 1 && sym.Name == identifier);
				if (foundSym is not null) banned[DisambigNameFor(foundSym)] = (foundSym, DiagnosticSeverity.Error, msgFmtStr);
			}
			var linqBinaryContainsSym = linqMethodSyms.OfType<IMethodSymbol>()
				.FirstOrDefault(static sym => sym.Parameters.Length is 2 && sym.Name is "Contains");
			if (linqBinaryContainsSym is not null)
			{
				banned[DisambigNameFor(linqBinaryContainsSym)] = (
					linqBinaryContainsSym,
					DiagnosticSeverity.Error,
					"Call the instance method from `String` (why would you do this)");
			}
			foreach (var sym in linqMethodSyms.OfType<IMethodSymbol>().Where(static sym => sym.Name is "DefaultIfEmpty"))
			{
				banned[DisambigNameFor(sym)] = sym.Parameters.Length is 2
					? (sym, DiagnosticSeverity.Warning, "Use `str.Length is 0 ? [ defaultValue ] : str`")
					: (sym, DiagnosticSeverity.Error, "Use `str.Length is 0 ? [ default(char) ] : str`" + CHAR_ZERO_WARNING);
			}
			var linqReverseSym = linqMethodSyms.OfType<IMethodSymbol>().FirstOrDefault(static sym => sym.Name is "Reverse");
			if (linqReverseSym is not null)
			{
				banned[DisambigNameFor(linqReverseSym)] = (
					linqReverseSym,
					DiagnosticSeverity.Error,
					"This will reverse the order of `char`s, which are NOT Unicode graphemes or even codepoints, and thus the result may be malformed;"
						+ " to reverse text, use `str.EnumerateRunes().Reverse()` (.NET Core) or `Strings.StrReverse(str)` (from VB.NET helpers),"
						+ " and to reverse a list of 16-bit values, use `Array.Reverse` on a `char[]`/`ushort[]`");
			}
			initContext.RegisterOperationAction(
				oac =>
				{
					var operation = (IInvocationOperation) oac.Operation;
					var calledSym = operation.TargetMethod.ConstructedFrom;
					var disambigName = DisambigNameFor(calledSym);
					if (!banned.TryGetValue(disambigName, out var tuple)) return;
					var (symToMatch, level, msgFmtStr) = tuple;
					if (!symToMatch.Matches(calledSym)) return;
					var receiverExpr = operation.Arguments[0];
					var receiverExprType = operation.SemanticModel!.GetTypeInfo(receiverExpr, oac.CancellationToken).Type!;
					if (receiverExprType.SpecialType is not SpecialType.System_String) return;
					switch (disambigName)
					{
						case "Concat#2" /*when operation.SemanticModel!
								.GetTypeInfo(operation.Arguments[1], oac.CancellationToken)
								.Type!.SpecialType is not SpecialType.System_String*/:
							// other string is only `IEnumerable<char>`
							level = DiagnosticSeverity.Warning;
							msgFmtStr = "Use concatenation if both are strings and the value is being enumerated to a new string";
							break;
						case "Contains#1" when !hasStringContainsChar:
							level = DiagnosticSeverity.Warning;
							var arg = operation.Arguments[1].ConstantValue;
							msgFmtStr = arg.HasValue
								? $"Use `({{0}}).Contains(\"{arg.Value}\")`"
								: "Use `str.Contains(c.ToString())`";
							break;
					}
					DiagLINQOnStrings.ReportAt(operation, level, oac, string.Format(msgFmtStr, receiverExpr.Syntax));
				},
				OperationKind.Invocation);
		});
	}
}
