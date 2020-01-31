using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Experiment.AutoGenConfig
{
	public static class ConfigEditorUIGenerators
	{
		public static readonly IDictionary<Type, IConfigPropEditorUIGen<Control>> FallbackGenerators = new Dictionary<Type, IConfigPropEditorUIGen<Control>> {
			[typeof(bool)] = new CheckBoxForBoolEditorUIGen(),
			[typeof(string)] = new TextBoxForStringEditorUIGen()
		};

		public static readonly IConfigPropEditorUIGen<GroupBox> FinalFallbackGenerator = new UnrepresentablePropEditorUIGen();

		private static Color GetComparisonColorRefT<T>(string prop, T? currentValue, AutoGenConfigForm parent, Func<T?, T?, bool> equalityFunc)
			where T : class
			=> equalityFunc(currentValue, parent.BaselineValues[prop] as T)
				? GetInitComparisonColorRefT(prop, currentValue, equalityFunc)
				: equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[prop] as T)
					? AutoGenConfigForm.ComparisonColors.ChangedUnset
					: AutoGenConfigForm.ComparisonColors.Changed;

		private static Color GetComparisonColorValT<T>(string prop, T? currentValue, AutoGenConfigForm parent, Func<T?, T?, bool> equalityFunc)
			where T : struct
			=> equalityFunc(currentValue, parent.BaselineValues[prop]?.Let(it => (T) it))
				? GetInitComparisonColorValT(prop, currentValue, equalityFunc)
				: equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[prop]?.Let(it => (T) it))
					? AutoGenConfigForm.ComparisonColors.ChangedUnset
					: AutoGenConfigForm.ComparisonColors.Changed;

		private static Color GetInitComparisonColorRefT<T>(string prop, T? currentValue, Func<T?, T?, bool> equalityFunc)
			where T : class
			=> equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[prop] as T)
				? AutoGenConfigForm.ComparisonColors.UnchangedDefault
				: AutoGenConfigForm.ComparisonColors.Unchanged;

		private static Color GetInitComparisonColorValT<T>(string prop, T? currentValue, Func<T?, T?, bool> equalityFunc)
			where T : struct
			=> equalityFunc(currentValue, AutoGenConfigForm.DefaultValues[prop]?.Let(it => (T) it))
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
			TControl GenerateControl(PropertyInfo pi, Config config, IDictionary<string, object?> baselineValues);
		}

		private class CheckBoxForBoolEditorUIGen : IConfigPropEditorUIGen<CheckBox>
		{
			private static bool BoolEquality(bool? a, bool? b) => a == b;

			private static void CheckBoxClickHandler(object clickEventSender, EventArgs clickEventArgs)
				=> ((CheckBox) clickEventSender).Let(cb =>
					cb.ForeColor = GetComparisonColorValT<bool>(((PropertyInfo) cb.Tag).Name, cb.Checked, GetMainFormParent(cb), BoolEquality)
				);

			public CheckBox GenerateControl(PropertyInfo pi, Config config, IDictionary<string, object?> baselineValues)
			{
				if (pi.PropertyType != typeof(bool)) throw new Exception();
				var baseline = (bool) pi.GetValue(config);
				baselineValues[pi.Name] = baseline;
				return new CheckBox
				{
					AutoSize = true,
					Checked = baseline,
					ForeColor = GetInitComparisonColorValT<bool>(pi.Name, baseline, BoolEquality),
					Tag = pi,
					Text = GetPropertyNameDesc(pi)
				}.Also(it => it.Click += CheckBoxClickHandler);
			}
		}

		private class TextBoxForStringEditorUIGen : IConfigPropEditorUIGen<FlowLayoutPanel>
		{
			private static readonly Func<string?, string?, bool> StringEquality = string.Equals;

			public FlowLayoutPanel GenerateControl(PropertyInfo pi, Config config, IDictionary<string, object?> baselineValues)
			{
				if (pi.PropertyType != typeof(string)) throw new Exception();
				var baseline = (string) pi.GetValue(config);
				baselineValues[pi.Name] = baseline;
				return new FlowLayoutPanel {
					AutoSize = true,
					Controls = {
						new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = GetPropertyNameDesc(pi) },
						new TextBox { AutoSize = true, Tag = pi, Text = baseline }.Also(it => it.TextChanged += TextBoxChangedHandler)
					},
					ForeColor = GetInitComparisonColorRefT(pi.Name, baseline, StringEquality)
				};
			}

			private static void TextBoxChangedHandler(object changedEventSender, EventArgs changedEventArgs)
				=> ((TextBox) changedEventSender).Let(tb =>
					tb.Parent.ForeColor = GetComparisonColorRefT(((PropertyInfo) tb.Tag).Name, tb.Text, GetMainFormParent(tb), StringEquality)
				);
		}

		private class UnrepresentablePropEditorUIGen : IConfigPropEditorUIGen<GroupBox>
		{
			public GroupBox GenerateControl(PropertyInfo pi, Config config, IDictionary<string, object?> baselineValues)
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
