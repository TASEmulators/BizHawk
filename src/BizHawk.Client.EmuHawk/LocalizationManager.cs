using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>Loads optional external UI translations and applies them to WinForms controls.</summary>
	internal static class LocalizationManager
	{
		private static readonly ConditionalWeakTable<Control, object> HookedControls = new();
		private static readonly ConditionalWeakTable<ToolStripItem, object> HookedItems = new();
		private static readonly Dictionary<string, string> Translations = LoadTranslations();

		public static bool IsEnabled { get; } = GetLanguage().StartsWith("zh", StringComparison.OrdinalIgnoreCase);

		private static string GetLanguage()
		{
			var requested = Environment.GetEnvironmentVariable("BIZHAWK_LANGUAGE");
			return string.IsNullOrWhiteSpace(requested) ? CultureInfo.CurrentUICulture.Name : requested;
		}

		private static Dictionary<string, string> LoadTranslations()
		{
			var result = new Dictionary<string, string>(StringComparer.Ordinal);
			var path = Path.Combine(AppContext.BaseDirectory, "Localization", "zh-CN.tsv");
			if (!File.Exists(path)) return result;
			foreach (var line in File.ReadAllLines(path))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
				var separator = line.IndexOf('\t');
				if (separator <= 0) continue;
				result[line.Substring(0, separator)] = line.Substring(separator + 1);
			}
			return result;
		}

		public static void Apply(Control root)
		{
			if (!IsEnabled || root == null) return;
			ApplyControl(root);
		}

		private static string Translate(string text)
			=> text != null && Translations.TryGetValue(text, out var translated) ? translated : text;

		private static void ApplyControl(Control control)
		{
			if (!(control is FormBase) && !string.IsNullOrEmpty(control.Text))
				control.Text = Translate(control.Text);

			if (HookedControls.TryGetValue(control, out _)) return;
			HookedControls.Add(control, new object());
			control.ControlAdded += (_, e) => ApplyControl(e.Control);

			if (control is ToolStrip strip)
			{
				strip.ItemAdded += (_, e) => ApplyItem(e.Item);
				ApplyItems(strip.Items);
			}

			foreach (Control child in control.Controls) ApplyControl(child);
		}

		private static void ApplyItems(ToolStripItemCollection items)
		{
			foreach (ToolStripItem item in items) ApplyItem(item);
		}

		private static void ApplyItem(ToolStripItem item)
		{
			if (!string.IsNullOrEmpty(item.Text)) item.Text = Translate(item.Text);
			if (!string.IsNullOrEmpty(item.ToolTipText)) item.ToolTipText = Translate(item.ToolTipText);

			if (item is ToolStripDropDownItem dropDownItem)
			{
				if (!HookedItems.TryGetValue(item, out _))
				{
					HookedItems.Add(item, new object());
					dropDownItem.DropDownOpening += (_, _) => ApplyItems(dropDownItem.DropDownItems);
				}
				ApplyItems(dropDownItem.DropDownItems);
			}
		}
	}
}
