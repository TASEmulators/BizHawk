namespace BizHawk.WinForms.BuilderDSL
{
	public interface IFinalizedBuilder<out T>
	{
		T GetControlRef();
	}

	internal class FinalizedBuilder<T> : IFinalizedBuilder<T>
	{
		private readonly T _control;

		public FinalizedBuilder(T control) => _control = control;

		public T GetControlRef() => _control;
	}

	public interface IFinalizedContainer<out TParent> : IFinalizedBuilder<TParent>
	{
		ControlBuilderContext Context { get; }
	}

	internal sealed class FinalizedContainer<TParent> : FinalizedBuilder<TParent>, IFinalizedContainer<TParent>
	{
		public ControlBuilderContext Context { get; }

		public FinalizedContainer(TParent control, ControlBuilderContext context) : base(control) => Context = context;
	}
}
