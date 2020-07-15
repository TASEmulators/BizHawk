using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
	public static partial class BuilderDSL
	{
		public static IFinalizedBuilder<Button> AddButton(
			this IContainerBuilder<object, Control> parent,
			string? text = null,
			DialogResult? dialogResult = null,
			Blueprint<ButtonBuilder>? blueprint = null
		) => parent.AddButton(btn =>
			{
				if (text != null) btn.SetText(text);
				if (dialogResult != null) btn.SetDialogResult(dialogResult.Value);
				blueprint?.Invoke(btn);
			});

		public static IFinalizedBuilder<Button> AddButton(
			this IContainerBuilder<object, Control> parent,
			string? text = null,
			EventHandler? onClick = null,
			Blueprint<ButtonBuilder>? blueprint = null
		) => parent.AddButton(btn =>
			{
				if (text != null) btn.SetText(text);
				if (onClick != null) btn.SubToClick(onClick);
				blueprint?.Invoke(btn);
			});

		public static IFinalizedBuilder<Button> AddButton(
			this IContainerBuilder<object, Control> parent,
			string? text = null,
			Blueprint<ButtonBuilder>? blueprint = null
		) => parent.AddButton(btn =>
			{
				if (text != null) btn.SetText(text);
				blueprint?.Invoke(btn);
			});

		public static IFinalizedBuilder<CheckBox> AddCheckBox(
			this IContainerBuilder<object, Control> parent,
			string? labelText = null,
			bool initiallyChecked = false,
			Blueprint<CheckBoxBuilder>? blueprint = null
		) => parent.AddCheckBox(cb =>
			{
				if (labelText != null) cb.LabelText(labelText);
				cb.CheckIf(initiallyChecked);
				blueprint?.Invoke(cb);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPColumnsFlowingUpToRight(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowUp();
				flp.WrapToNewRows();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPColumnsToRight(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowDown();
				flp.WrapToNewRows();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPRowsLTRInLTR(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowLTRInLTR();
				flp.WrapToNewRows();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPRowsRTLInLTR(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowRTLInLTR();
				flp.WrapToNewRows();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPSingleColumn(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowDown();
				flp.SingleColumn();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPSingleColumnFlowingUp(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowUp();
				flp.SingleColumn();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPSingleRowLTRInLTR(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowLTRInLTR();
				flp.SingleRow();
				blueprint?.Invoke(flp);
			});

		public static IFinalizedContainer<FlowLayoutPanel> AddFLPSingleRowRTLInLTR(
			this IContainerBuilder<object, Control> parent,
			Blueprint<FLPBuilder>? blueprint = null
		) => parent.AddFLP(flp =>
			{
				flp.FlowRTLInLTR();
				flp.SingleRow();
				blueprint?.Invoke(flp);
			});

#if false
		public static IFinalizedContainer<GroupBoxEx> AddGroupBoxEx(
			this IContainerBuilder<object, Control> parent,
			RadioButtonGroupTracker tracker,
			string? labelText = null,
			Blueprint<GroupBoxExBuilder>? blueprint = null
		) => parent.AddGroupBoxEx(grp =>
			{
				grp.SetTracker(tracker);
				if (labelText != null) grp.LabelText(labelText);
				blueprint?.Invoke(grp);
			});

		public static IFinalizedContainer<GroupBoxEx> AddGroupBoxEx(
			this IContainerBuilder<object, Control> parent,
			RadioButtonGroupTracker tracker,
			Blueprint<GroupBoxExBuilder>? blueprint = null
		) => parent.AddGroupBoxEx(grp =>
			{
				grp.SetTracker(tracker);
				blueprint?.Invoke(grp);
			});

		public static IFinalizedContainer<GroupBoxEx> AddGroupBoxEx(
			this IContainerBuilder<object, Control> parent,
			string? labelText = null,
			Blueprint<GroupBoxExBuilder>? blueprint = null
		) => parent.AddGroupBoxEx(grp =>
			{
				if (labelText != null) grp.LabelText(labelText);
				blueprint?.Invoke(grp);
			});
#endif

		public static IFinalizedBuilder<Label> AddLabel(
			this IContainerBuilder<object, Control> parent,
			string? labelText = null,
			Blueprint<LabelBuilder>? blueprint = null
		) => parent.AddLabel(lbl =>
			{
				if (labelText != null) lbl.LabelText(labelText);
				blueprint?.Invoke(lbl);
			});

		public static IFinalizedBuilder<LinkLabel> AddLinkLabel(
			this IContainerBuilder<object, Control> parent,
			string? labelText = null,
			string? uriString = null,
			Blueprint<LinkLabelBuilder>? blueprint = null
		) => parent.AddLinkLabel(lbl =>
			{
				if (labelText != null) lbl.LabelText(labelText);
				if (uriString != null) lbl.Hyperlink(uriString);
				blueprint?.Invoke(lbl);
			});

		public static IFinalizedBuilder<NumericUpDown> AddNUD(
			this IContainerBuilder<object, Control> parent,
			Range<decimal>? validRange = null,
			decimal? initValue = null,
			Blueprint<NUDBuilder>? blueprint = null
		) => parent.AddNUD(nud =>
			{
				if (validRange != null) nud.SetValidRange(validRange);
				if (initValue != null) nud.SetInitialValue(initValue.Value);
				blueprint?.Invoke(nud);
			});

		public static IFinalizedBuilder<RadioButtonEx> AddRadioButtonEx(
			this IContainerBuilder<object, Control> parent,
			RadioButtonGroupTracker tracker,
			string? labelText = null,
			bool initiallyChecked = false,
			Blueprint<RadioButtonExBuilder>? blueprint = null
		) => parent.AddRadioButtonEx(rb =>
			{
				rb.SetTracker(tracker);
				if (labelText != null) rb.LabelText(labelText);
				rb.CheckIf(initiallyChecked);
				blueprint?.Invoke(rb);
			});

		public static IFinalizedBuilder<RadioButtonEx> AddRadioButtonEx(
			this IContainerBuilder<object, Control> parent,
			string? labelText = null,
			bool initiallyChecked = false,
			Blueprint<RadioButtonExBuilder>? blueprint = null
		) => parent.AddRadioButtonEx(rb =>
			{
				if (labelText != null) rb.LabelText(labelText);
				rb.CheckIf(initiallyChecked);
				blueprint?.Invoke(rb);
			});

		public static IFinalizedBuilder<RadioButtonEx> AddRadioButtonEx(
			this IContainerBuilder<object, Control> parent,
			RadioButtonGroupTracker tracker,
			Blueprint<RadioButtonExBuilder>? blueprint = null
		) => parent.AddRadioButtonEx(rb =>
			{
				rb.SetTracker(tracker);
				blueprint?.Invoke(rb);
			});

#if false
		/// <remarks>by default, the built GroupBox will contain a single-column FLP which in turn contains the RadioButtons</remarks>
		public static (IFinalizedContainer<GroupBoxEx> Built, IReadOnlyList<IFinalizedBuilder<RadioButtonEx>> BuiltButtons) AddRadioButtonGroupFromEnum<T>(
			this IContainerBuilder<object, Control> parent,
			Blueprint<GroupBoxExBuilder>? groupBoxBlueprint,
			Blueprint<FLPBuilder>? flpBlueprint,
			T? initiallyChecked,
			IReadOnlyDictionary<T, Blueprint<RadioButtonExBuilder>?> blueprints
		) where T : struct, Enum
		{
			var tracker = new RadioButtonGroupTracker();
			IReadOnlyList<IFinalizedBuilder<RadioButtonEx>>? builtButtons = null;
			return (
				parent.AddGroupBoxEx(grp =>
				{
					grp.SetTracker(tracker);
					groupBoxBlueprint?.Invoke(grp);
					grp.AddFLPSingleColumn(flp =>
					{
						flpBlueprint?.Invoke(flp);
						builtButtons = blueprints.Select(kvp => flp.AddRadioButtonEx(rb =>
						{
							rb.SetTracker(tracker);
							rb.SetDataTag(kvp.Key);
							rb.CheckIf(kvp.Key.Equals(initiallyChecked));
							kvp.Value?.Invoke(rb);
						})).ToList();
					});
				}),
				builtButtons ?? throw new Exception()
			);
		}
#endif

		public static IFinalizedBuilder<TrackBar> AddTrackBar(
			this IContainerBuilder<object, Control> parent,
			bool isVertical = false,
			Range<int>? validRange = null,
			int? initValue = null,
			int? markEvery = null,
			int? bigStep = null,
			Blueprint<TrackBarBuilder>? blueprint = null
		) => parent.AddTrackBar(tb =>
			{
				if (isVertical) tb.OrientVertically();
				if (validRange != null) tb.SetValidRange(validRange);
				if (initValue != null) tb.SetInitialValue(initValue.Value);
				if (markEvery != null) tb.SetTickFrequency(markEvery.Value);
				if (bigStep != null) tb.SetBigStep(bigStep.Value);
				blueprint?.Invoke(tb);
			});
	}
}