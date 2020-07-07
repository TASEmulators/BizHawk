namespace BizHawk.WinForms.BuilderDSL
{
	public delegate void Blueprint<in TBuilder>(TBuilder builder) where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>;
}
