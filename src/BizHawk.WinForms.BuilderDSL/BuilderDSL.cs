using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public static partial class BuilderDSL
	{
		public static void AddChildren(this IFinalizedContainer<Control> parent, Blueprint<IContainerBuilder<object, Control>> blueprint)
			=> blueprint(new FinalizedParentedControlBuilder<Control>(parent.Context, parent.GetControlRef().Controls.Add));

		public static UnparentedControlBuilder BuildUnparented(ControlBuilderContext context) => new UnparentedControlBuilder(context);

		public static void HackAddChild(this IFinalizedContainer<Control> parent, Control c)
			=> parent.GetControlRef().Controls.Add(c);

		public static WithContextControlBuilder<TBuilt, TChild> WithContext<TBuilt, TChild>(this IContainerBuilder<TBuilt, TChild> wrapped, ControlBuilderContext newContext)
			where TBuilt : class
			=> new WithContextControlBuilder<TBuilt, TChild>(wrapped, newContext);

		public static WithContextControlBuilder<TBuilt, TChild> WithContext<TBuilt, TChild>(
			this IContainerBuilder<TBuilt, TChild> wrapped,
			bool? isLTR = null,
			bool? autoPosOnly = null,
			bool? autoSizeOnly = null
		) where TBuilt : class
		{
			var ctx = wrapped.Context;
			return wrapped.WithContext(new ControlBuilderContext(
				isLTR: isLTR ?? ctx.IsLTR,
				autoPosOnly: autoPosOnly ?? ctx.AutoPosOnly,
				autoSizeOnly: autoSizeOnly ?? ctx.AutoSizeOnly
			));
		}
	}
}
