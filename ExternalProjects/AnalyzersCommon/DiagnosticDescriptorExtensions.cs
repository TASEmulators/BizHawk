namespace BizHawk.Analyzers;

using static System.Runtime.CompilerServices.MethodImplOptions;

using DD = Microsoft.CodeAnalysis.DiagnosticDescriptor;
using MI = System.Runtime.CompilerServices.MethodImplAttribute;
using OAC = Microsoft.CodeAnalysis.Diagnostics.OperationAnalysisContext;
using SNAC = Microsoft.CodeAnalysis.Diagnostics.SyntaxNodeAnalysisContext;

public static class DDExtensions
{
	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		Location location,
		DiagnosticSeverity effectiveSeverity,
		OAC ctx,
		object?[]? messageArgs = null)
			=> ctx.ReportDiagnostic(Diagnostic.Create(
				diag,
				location,
				effectiveSeverity,
				additionalLocations: null,
				properties: null,
				messageArgs: messageArgs));

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		Location location,
		DiagnosticSeverity effectiveSeverity,
		SNAC ctx,
		object?[]? messageArgs = null)
			=> ctx.ReportDiagnostic(Diagnostic.Create(
				diag,
				location,
				effectiveSeverity,
				additionalLocations: null,
				properties: null,
				messageArgs: messageArgs));

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, Location location, OAC ctx, object?[]? messageArgs = null)
		=> ctx.ReportDiagnostic(Diagnostic.Create(diag, location, messageArgs: messageArgs));

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, Location location, SNAC ctx, object?[]? messageArgs = null)
		=> ctx.ReportDiagnostic(Diagnostic.Create(diag, location, messageArgs: messageArgs));

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		SyntaxNode location,
		DiagnosticSeverity effectiveSeverity,
		OAC ctx,
		object?[]? messageArgs = null)
			=> diag.ReportAt(location.GetLocation(), effectiveSeverity, ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		SyntaxNode location,
		DiagnosticSeverity effectiveSeverity,
		SNAC ctx,
		object?[]? messageArgs = null)
			=> diag.ReportAt(location.GetLocation(), effectiveSeverity, ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		SyntaxNode location,
		DiagnosticSeverity effectiveSeverity,
		OAC ctx,
		string message)
			=> diag.ReportAt(location, effectiveSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		SyntaxNode location,
		DiagnosticSeverity effectiveSeverity,
		SNAC ctx,
		string message)
			=> diag.ReportAt(location, effectiveSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, bool isErrorSeverity, OAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(
			location,
			isErrorSeverity ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
			ctx,
			messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, bool isErrorSeverity, SNAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(
			location,
			isErrorSeverity ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
			ctx,
			messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, bool isErrorSeverity, OAC ctx, string message)
		=> diag.ReportAt(location, isErrorSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, bool isErrorSeverity, SNAC ctx, string message)
		=> diag.ReportAt(location, isErrorSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, OAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(location.GetLocation(), ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, SNAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(location.GetLocation(), ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, OAC ctx, string message)
		=> diag.ReportAt(location, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, SyntaxNode location, SNAC ctx, string message)
		=> diag.ReportAt(location, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		IOperation location,
		DiagnosticSeverity effectiveSeverity,
		OAC ctx,
		object?[]? messageArgs = null)
			=> diag.ReportAt(location.Syntax, effectiveSeverity, ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(
		this DD diag,
		IOperation location,
		DiagnosticSeverity effectiveSeverity,
		OAC ctx,
		string message)
			=> diag.ReportAt(location, effectiveSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, IOperation location, bool isErrorSeverity, OAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(
			location,
			isErrorSeverity ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
			ctx,
			messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, IOperation location, bool isErrorSeverity, OAC ctx, string message)
		=> diag.ReportAt(location, isErrorSeverity, ctx, [ message ]);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, IOperation location, OAC ctx, object?[]? messageArgs = null)
		=> diag.ReportAt(location.Syntax, ctx, messageArgs);

	[MI(AggressiveInlining)]
	public static void ReportAt(this DD diag, IOperation location, OAC ctx, string message)
		=> diag.ReportAt(location, ctx, [ message ]);
}
