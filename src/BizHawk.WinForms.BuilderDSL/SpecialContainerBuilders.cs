using System;

namespace BizHawk.WinForms.BuilderDSL
{
	public class FinalizedParentedControlBuilder<TChild> : IContainerBuilder<object, TChild>
	{
		private readonly Action<TChild> _addChildCallback;

		public ControlBuilderContext Context { get; set; }

		internal FinalizedParentedControlBuilder(in ControlBuilderContext context, Action<TChild> addChildCallback)
		{
			Context = context;
			_addChildCallback = addChildCallback;
		}

		protected FinalizedParentedControlBuilder(in ControlBuilderContext context) : this(context, c => {}) {}

		public void AddChild(IFinalizedBuilder<TChild> finalized) => _addChildCallback(finalized.GetControlRef());

		public IFinalizedContainer<object>? BuildOrNull() => throw new InvalidOperationException();
	}

	public sealed class UnparentedControlBuilder : FinalizedParentedControlBuilder<object>
	{
		public UnparentedControlBuilder(in ControlBuilderContext context) : base(context) {}
	}

	public sealed class WithContextControlBuilder<TBuilt, TChild> : IContainerBuilder<TBuilt, TChild>
		where TBuilt : class
	{
		private readonly IContainerBuilder<TBuilt, TChild> _wrapped;

		public ControlBuilderContext Context { get; set; }

		public WithContextControlBuilder(IContainerBuilder<TBuilt, TChild> wrapped, ControlBuilderContext newContext)
		{
			_wrapped = wrapped;
			Context = newContext;
		}

		public void AddChild(IFinalizedBuilder<TChild> finalized) => _wrapped.AddChild(finalized);

		public IFinalizedContainer<TBuilt>? BuildOrNull() => _wrapped.BuildOrNull();
	}
}
