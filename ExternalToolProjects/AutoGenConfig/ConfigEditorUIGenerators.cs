using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BizHawk.Experiment.AutoGenConfig
{
	public static class ConfigEditorUIGenerators
	{
		public static readonly IDictionary<Type, IConfigPropEditorUIGen<Control>> FallbackGenerators = new Dictionary<Type, IConfigPropEditorUIGen<Control>> {
			[typeof(bool)] = new CheckBoxForBoolEditorUIGen(),
			[typeof(int)] = new NumericUpDownForInt32EditorUIGen(),
			[typeof(string)] = new TextBoxForStringEditorUIGen()
		};

		public static readonly IConfigPropEditorUIGen<GroupBox> FinalFallbackGenerator = new UnrepresentablePropEditorUIGen();

		private static Color GetComparisonColorRefT<T>(string nestedName, T? currentValue, AutoGenConfigForm parentForm, Func<T?, T?, bool> equalityFunc)
			where T : class
			=> equalityFunc(currentValue, parentForm.BaselineValues[nestedName] as T)
				? GetInitComparisonColorRefT(nestedName, currentValue, equalityFunc)
				: equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[nestedName] as T)
					? AutoGenConfigForm.ComparisonColors.ChangedUnset
					: AutoGenConfigForm.ComparisonColors.Changed;

		private static Color GetComparisonColorValT<T>(string nestedName, T? currentValue, AutoGenConfigForm parentForm, Func<T?, T?, bool> equalityFunc)
			where T : struct
			=> equalityFunc(currentValue, parentForm.BaselineValues[nestedName]?.Let(it => (T) it))
				? GetInitComparisonColorValT(nestedName, currentValue, equalityFunc)
				: equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[nestedName]?.Let(it => (T) it))
					? AutoGenConfigForm.ComparisonColors.ChangedUnset
					: AutoGenConfigForm.ComparisonColors.Changed;

		private static Color GetInitComparisonColorRefT<T>(string nestedName, T? currentValue, Func<T?, T?, bool> equalityFunc)
			where T : class
			=> equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[nestedName] as T)
				? AutoGenConfigForm.ComparisonColors.UnchangedDefault
				: AutoGenConfigForm.ComparisonColors.Unchanged;

		private static Color GetInitComparisonColorValT<T>(string nestedName, T? currentValue, Func<T?, T?, bool> equalityFunc)
			where T : struct
			=> equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[nestedName]?.Let(it => (T) it))
				? AutoGenConfigForm.ComparisonColors.UnchangedDefault
				: AutoGenConfigForm.ComparisonColors.Unchanged;

		private static AutoGenConfigForm GetMainFormParent(Control c)
		{
			var parent = c.Parent;
			while (!(parent is AutoGenConfigForm)) parent = parent.Parent;
			return (AutoGenConfigForm) parent;
		}

		private static string GetPropertyNameDesc(PropertyInfo pi)
			=> pi.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault()
				?.Let(it => $"{pi.Name}: {((DescriptionAttribute) it).Description}")
				?? pi.Name;

		public struct ComparisonColors
		{
			public Color Changed;
			public Color ChangedInvalid;
			public Color ChangedUnset;
			public Color Unchanged;
			public Color UnchangedDefault;
		}

		public interface IConfigPropEditorUIGen<out TControl>
			where TControl : Control
		{
			TControl GenerateControl(string nesting, PropertyInfo pi, object config, IDictionary<string, object?> baselineValues);
		}

		private class CheckBoxForBoolEditorUIGen : IConfigPropEditorUIGen<CheckBox>
		{
			private static bool BoolEquality(bool? a, bool? b) => a == b;

			private static void CheckBoxClickHandler(object changedEventSender, EventArgs changedEventArgs)
				=> ((CheckBox) changedEventSender).Let(cb =>
					cb.ForeColor = GetComparisonColorValT<bool>(cb.Name, cb.Checked, GetMainFormParent(cb), BoolEquality)
				);

			public CheckBox GenerateControl(string nesting, PropertyInfo pi, object config, IDictionary<string, object?> baselineValues)
			{
				if (pi.PropertyType != typeof(bool)) throw new Exception();
				var baseline = (bool) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				baselineValues[nestedName] = baseline;
				return new CheckBox
				{
					AutoSize = true,
					Checked = baseline,
					ForeColor = GetInitComparisonColorValT<bool>(nestedName, baseline, BoolEquality),
					Name = nestedName,
					Text = GetPropertyNameDesc(pi)
				}.Also(it => it.CheckedChanged += CheckBoxClickHandler);
			}
		}

		private class NumericUpDownForInt32EditorUIGen : IConfigPropEditorUIGen<FlowLayoutPanel>
		{
			private static bool IntEquality(int? a, int? b) => a == b;

			private static void NumericUpDownChangedHandler(object changedEventSender, EventArgs changedEventArgs)
				=> ((NumericUpDown) changedEventSender).Let(nud =>
					nud.Parent.ForeColor = GetComparisonColorValT<int>(nud.Name, (int) nud.Value, GetMainFormParent(nud), IntEquality)
				);

			public FlowLayoutPanel GenerateControl(string nesting, PropertyInfo pi, object config, IDictionary<string, object?> baselineValues)
			{
				if (pi.PropertyType != typeof(int)) throw new Exception();
				var baseline = (int) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				baselineValues[nestedName] = baseline;
				return new FlowLayoutPanel {
					AutoSize = true,
					Controls = {
						new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = GetPropertyNameDesc(pi) },
						new NumericUpDown
						{
							Maximum = int.MaxValue,
							Minimum = int.MinValue,
							Name = nestedName,
							Size = new Size(72, 20),
							Value = baseline
						}.Also(it =>
						{
							if (pi.GetCustomAttributes(typeof(RangeAttribute), false).FirstOrDefault() is RangeAttribute range)
							{
								it.Maximum = (int) range.Maximum;
								it.Minimum = (int) range.Minimum;
							}
							it.ValueChanged += NumericUpDownChangedHandler;
						})
					},
					ForeColor = GetInitComparisonColorValT<int>(nestedName, baseline, IntEquality)
				};
			}
		}

		private class TextBoxForStringEditorUIGen : IConfigPropEditorUIGen<FlowLayoutPanel>
		{
			private static readonly Func<string?, string?, bool> StringEquality = string.Equals;

			public FlowLayoutPanel GenerateControl(string nesting, PropertyInfo pi, object config, IDictionary<string, object?> baselineValues)
			{
				if (pi.PropertyType != typeof(string)) throw new Exception();
				var baseline = (string) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				baselineValues[nestedName] = baseline;
				return new FlowLayoutPanel {
					AutoSize = true,
					Controls = {
						new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = GetPropertyNameDesc(pi) },
						new TextBox { AutoSize = true, Name = nestedName, Text = baseline }.Also(it => it.TextChanged += TextBoxChangedHandler)
					},
					ForeColor = GetInitComparisonColorRefT(nestedName, baseline, StringEquality)
				};
			}

			private static void TextBoxChangedHandler(object changedEventSender, EventArgs changedEventArgs)
				=> ((TextBox) changedEventSender).Let(tb =>
					tb.Parent.ForeColor = GetComparisonColorRefT(tb.Name, tb.Text, GetMainFormParent(tb), StringEquality)
				);
		}

		private class UnrepresentablePropEditorUIGen : IConfigPropEditorUIGen<GroupBox>
		{
			public GroupBox GenerateControl(string nesting, PropertyInfo pi, object config, IDictionary<string, object?> baselineValues)
				=> new GroupBox {
					AutoSize = true,
					Controls = {
						new FlowLayoutPanel {
							AutoSize = true,
							Controls = { new Label { AutoSize = true, Text = $"no editor found for type {pi.PropertyType}" } },
							Location = new Point(4, 16),
							MaximumSize = new Size(int.MaxValue, 20)
						}
					},
					MaximumSize = new Size(int.MaxValue, 40),
					Text = pi.Name
				};
		}
	}
}
