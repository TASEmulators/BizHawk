namespace BizHawk.Analyzers;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FeatureNotImplementedAnalyzer : DiagnosticAnalyzer
{
	private const string ERR_MSG_DOES_NOT_THROW = "Throw NotImplementedException in [FeatureNotImplemented] method/prop body (or remove attribute)";

	private const string ERR_MSG_METHOD_THROWS_UNKNOWN = "Indeterminable exception type in [FeatureNotImplemented] method/prop body, should be NotImplementedException";

	private const string ERR_MSG_THROWS_WRONG_TYPE = "Incorrect exception type in [FeatureNotImplemented] method/prop body, should be NotImplementedException";

	private const string ERR_MSG_UNEXPECTED_INCANTATION = "It seems [FeatureNotImplemented] should not be applied to whatever this is";

	private static readonly DiagnosticDescriptor DiagShouldThrowNIE = new(
		id: "BHI3300",
		title: "Throw NotImplementedException from methods/props marked [FeatureNotImplemented]",
		messageFormat: "{0}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagShouldThrowNIE);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(initContext =>
		{
			var featureNotImplementedAttrSym = initContext.Compilation.GetTypeByMetadataName("BizHawk.Emulation.Common.FeatureNotImplementedAttribute");
			if (featureNotImplementedAttrSym is null) return; // project does not have BizHawk.Emulation.Common dependency
			var notImplementedExceptionSym = initContext.Compilation.GetTypeByMetadataName("System.NotImplementedException")!;
			initContext.RegisterSyntaxNodeAction(
				snac =>
				{
					void Wat(Location location)
						=> snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_UNEXPECTED_INCANTATION));
					void MaybeReportFor(ITypeSymbol? thrownExceptionType, Location location)
					{
						if (thrownExceptionType is null) snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_METHOD_THROWS_UNKNOWN));
						else if (!notImplementedExceptionSym.Matches(thrownExceptionType)) snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_THROWS_WRONG_TYPE));
						// else correct usage, do not flag
					}
					bool IncludesFNIAttribute(SyntaxList<AttributeListSyntax> mds)
						=> mds.SelectMany(static als => als.Attributes)
							.Any(aSyn => featureNotImplementedAttrSym.Matches(snac.SemanticModel.GetTypeInfo(aSyn, snac.CancellationToken).Type));
					void CheckBlockBody(BlockSyntax bs, Location location)
					{
						if (bs.Statements.Count is not 1) snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_DOES_NOT_THROW));
						else if (bs.Statements[0] is not ThrowStatementSyntax tss) snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_DOES_NOT_THROW));
						else MaybeReportFor(snac.SemanticModel.GetThrownExceptionType(tss), tss.GetLocation());
					}
					void CheckExprBody(ArrowExpressionClauseSyntax aecs, Location location)
					{
						if (aecs.Expression is not ThrowExpressionSyntax tes) snac.ReportDiagnostic(Diagnostic.Create(DiagShouldThrowNIE, location, ERR_MSG_DOES_NOT_THROW));
						else MaybeReportFor(snac.SemanticModel.GetThrownExceptionType(tes), tes.GetLocation());
					}
					void CheckAccessor(AccessorDeclarationSyntax ads)
					{
						if (!IncludesFNIAttribute(ads.AttributeLists)) return;
						if (ads.ExpressionBody is not null) CheckExprBody(ads.ExpressionBody, ads.GetLocation());
						else if (ads.Body is not null) CheckBlockBody(ads.Body, ads.GetLocation());
						else Wat(ads.GetLocation());
					}
					switch (snac.Node)
					{
						case AccessorDeclarationSyntax ads:
							CheckAccessor(ads);
							break;
						case MethodDeclarationSyntax mds:
							if (!IncludesFNIAttribute(mds.AttributeLists)) return;
							if (mds.ExpressionBody is not null) CheckExprBody(mds.ExpressionBody, mds.GetLocation());
							else if (mds.Body is not null) CheckBlockBody(mds.Body, mds.GetLocation());
							else Wat(mds.GetLocation());
							break;
						case PropertyDeclarationSyntax pds:
							if (pds.ExpressionBody is not null)
							{
								if (IncludesFNIAttribute(pds.AttributeLists)) CheckExprBody(pds.ExpressionBody, pds.GetLocation());
							}
							else
							{
								if (IncludesFNIAttribute(pds.AttributeLists)) Wat(pds.GetLocation());
#if false // accessors will be checked separately
								else foreach (var accessor in pds.AccessorList!.Accessors) CheckAccessor(accessor);
#endif
							}
							break;
					}
				},
				SyntaxKind.GetAccessorDeclaration,
				SyntaxKind.MethodDeclaration,
				SyntaxKind.PropertyDeclaration,
				SyntaxKind.SetAccessorDeclaration);
		});
	}
}
