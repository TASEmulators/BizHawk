using System;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
	public static partial class BuilderDSL
	{
		private static TBuilt BuildAndAddToParent<TBuilder, TBuilt, TControl>(Blueprint<TBuilder> blueprint, IContainerBuilder<object, TControl> parent)
			where TBuilder : IControlBuilderBase<TBuilt>
			where TBuilt : class, IFinalizedBuilder<TControl>
			where TControl : class
		{
			var builder = Activator.CreateInstance<TBuilder>();
			builder.Context = parent.Context;
			blueprint(builder);
			var finalized = builder.BuildOrNull() ?? throw new Exception();
			parent.AddChild(finalized);
			return finalized;
		}

		public static IFinalizedBuilder<Button> AddButton(this IContainerBuilder<object, Control> parent, Blueprint<ButtonBuilder> blueprint)
			=> BuildAndAddToParent<ButtonBuilder, IFinalizedBuilder<Button>, Button>(blueprint, parent);

		public static IFinalizedBuilder<CheckBox> AddCheckBox(this IContainerBuilder<object, Control> parent, Blueprint<CheckBoxBuilder> blueprint)
			=> BuildAndAddToParent<CheckBoxBuilder, IFinalizedBuilder<CheckBox>, CheckBox>(blueprint, parent);

		public static IFinalizedContainer<FlowLayoutPanel> AddFLP(this IContainerBuilder<object, Control> parent, Blueprint<FLPBuilder> blueprint)
			=> BuildAndAddToParent<FLPBuilder, IFinalizedContainer<FlowLayoutPanel>, FlowLayoutPanel>(blueprint, parent);

#if false
		public static IFinalizedContainer<GroupBoxEx> AddGroupBoxEx(this IContainerBuilder<object, Control> parent, Blueprint<GroupBoxExBuilder> blueprint)
			=> BuildAndAddToParent<GroupBoxExBuilder, IFinalizedContainer<GroupBoxEx>, GroupBoxEx>(blueprint, parent);
#endif

		public static IFinalizedBuilder<Label> AddLabel(this IContainerBuilder<object, Control> parent, Blueprint<LabelBuilder> blueprint)
			=> BuildAndAddToParent<LabelBuilder, IFinalizedBuilder<Label>, Label>(blueprint, parent);

		public static IFinalizedBuilder<LinkLabel> AddLinkLabel(this IContainerBuilder<object, Control> parent, Blueprint<LinkLabelBuilder> blueprint)
			=> BuildAndAddToParent<LinkLabelBuilder, IFinalizedBuilder<LinkLabel>, LinkLabel>(blueprint, parent);

		public static IFinalizedBuilder<ListBox> AddListBox(this IContainerBuilder<object, ListBox> parent, Blueprint<ListBoxBuilder> blueprint)
			=> BuildAndAddToParent<ListBoxBuilder, IFinalizedBuilder<ListBox>, ListBox>(blueprint, parent);

		public static IFinalizedBuilder<NumericUpDown> AddNUD(this IContainerBuilder<object, Control> parent, Blueprint<NUDBuilder> blueprint)
			=> BuildAndAddToParent<NUDBuilder, IFinalizedBuilder<NumericUpDown>, NumericUpDown>(blueprint, parent);

		public static IFinalizedBuilder<RadioButtonEx> AddRadioButtonEx(this IContainerBuilder<object, Control> parent, Blueprint<RadioButtonExBuilder> blueprint)
			=> BuildAndAddToParent<RadioButtonExBuilder, IFinalizedBuilder<RadioButtonEx>, RadioButtonEx>(blueprint, parent);

		public static IFinalizedContainer<TabControl> AddTabControl(this IContainerBuilder<object, Control> parent, Blueprint<TabControlBuilder> blueprint)
			=> BuildAndAddToParent<TabControlBuilder, IFinalizedContainer<TabControl>, TabControl>(blueprint, parent);

		public static IFinalizedContainer<TabPage> AddTabPage(this IContainerBuilder<object, TabPage> parent, Blueprint<TabPageBuilder> blueprint)
			=> BuildAndAddToParent<TabPageBuilder, IFinalizedContainer<TabPage>, TabPage>(blueprint, parent);

		public static IFinalizedBuilder<TextBox> AddTextBox(this IContainerBuilder<object, TextBox> parent, Blueprint<TextBoxBuilder> blueprint)
			=> BuildAndAddToParent<TextBoxBuilder, IFinalizedBuilder<TextBox>, TextBox>(blueprint, parent);

		public static IFinalizedBuilder<TrackBar> AddTrackBar(this IContainerBuilder<object, Control> parent, Blueprint<TrackBarBuilder> blueprint)
			=> BuildAndAddToParent<TrackBarBuilder, IFinalizedBuilder<TrackBar>, TrackBar>(blueprint, parent);
	}
}
