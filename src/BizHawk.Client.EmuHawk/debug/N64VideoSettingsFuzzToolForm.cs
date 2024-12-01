#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk.ForDebugging
{
	internal sealed class N64VideoSettingsFuzzToolForm : ToolFormBase
	{
		public const string TOOL_NAME = "N64 Video Settings Fuzzer";

		[RequiredService]
		private N64? _maybeEmulator { get; set; } = null;

		private N64 Emulator
			=> _maybeEmulator!;

		protected override string WindowTitleStatic
			=> TOOL_NAME;

		public N64VideoSettingsFuzzToolForm()
		{
			ClientSize = new(240, 96);
			SuspendLayout();
#if true // don't think the other plugins are even worth testing anymore, but this could easily be expanded to include them --yoshi
			ComboBox comboPlugin = new() { Enabled = false, Items = { "GLideN64" }, SelectedIndex = 0 };
#else
			ComboBox comboPlugin = new() { Items = { "GLideN64" } };
#endif
			Dictionary<PropertyInfo, IReadOnlyList<object?>> propDict = new();
			foreach (var pi in typeof(N64SyncSettings.N64GLideN64PluginSettings).GetProperties())
			{
				if (pi.PropertyType == typeof(bool)) propDict[pi] = new object?[] { true, false };
				else if (pi.PropertyType.IsEnum) propDict[pi] = Enum.GetValues(pi.PropertyType).Cast<object?>().ToArray();
			}
			static object? RandomElem(IReadOnlyList<object?> a, Random rng)
				=> a[rng.Next(a.Count)];
			Random rng = new();
			void Fuzz(bool limit)
			{
				var props = propDict.Keys.ToList();
				if (limit)
				{
					props.Sort((_, _) => rng.Next(2));
					var l = props.Count / 10;
					while (l < props.Count) props.RemoveAt(l);
				}
				var ss = Emulator.GetSyncSettings();
				var glidenSS = ss.GLideN64Plugin;
				foreach (var pi in props) pi.SetValue(obj: glidenSS, value: RandomElem(propDict[pi], rng));
				((MainForm) MainForm).GetSettingsAdapterForLoadedCore<N64>().PutCoreSyncSettings(ss);
			}
			Controls.Add(new SingleColumnFLP
			{
				Controls =
				{
					comboPlugin,
					GenControl.Button("--> randomise some props", width: 200, () => Fuzz(limit: true)),
					GenControl.Button("--> randomise every prop", width: 200, () => Fuzz(limit: false)),
				},
			});
			ResumeLayout();
		}
	}
}
