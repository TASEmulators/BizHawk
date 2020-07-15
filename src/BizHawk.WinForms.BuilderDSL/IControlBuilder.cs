namespace BizHawk.WinForms.BuilderDSL
{
	public interface IControlBuilderBase<out TBuilt> where TBuilt : class, IFinalizedBuilder<object>
	{
		/// <remarks>this will always be set immediately after construction and before any other calls</remarks>
		ControlBuilderContext Context { get; set; }

		/// <seealso cref="Docs.OrderingConvention"/>
		TBuilt? BuildOrNull();
	}

	/// <remarks>this interface only exists to make the typeparams shorter</remarks>
	public interface IControlBuilder<out TBuilt> : IControlBuilderBase<IFinalizedBuilder<TBuilt>> where TBuilt : class {}

	public interface IContainerBuilder<out TBuilt, in TChild> : IControlBuilderBase<IFinalizedContainer<TBuilt>> where TBuilt : class
	{
		void AddChild(IFinalizedBuilder<TChild> finalized);
	}
}
