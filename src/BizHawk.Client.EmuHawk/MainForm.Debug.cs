#nullable enable
#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private sealed class DebugVSystemChildItem : ToolStripMenuItemEx
		{
			public string? RequiresCore = null;

			public bool RequiresLoadedRom = true;

			public DebugVSystemChildItem(string labelText, Action onClick)
			{
				Text = labelText;
				Click += (_, _) => onClick();
			}
		}

		private sealed class DebugVSystemMenuItem : ToolStripMenuItemEx
		{
			public readonly IReadOnlyCollection<string> ExtraSysIDs;

			public DebugVSystemMenuItem(string labelText, IReadOnlyCollection<string>? extraSysIDs = null)
			{
				ExtraSysIDs = extraSysIDs ?? Array.Empty<string>();
				Text = labelText;
			}
		}

		private sealed class N64VideoSettingsFuzzToolForm : ToolFormBase
		{
			public const string TOOL_NAME = "N64 Video Settings Fuzzer";

			[RequiredService]
			private IEmulator? Emulator { get; set; }

			protected override string WindowTitleStatic { get; } = TOOL_NAME;

			public N64VideoSettingsFuzzToolForm()
			{
				ClientSize = new(240, 96);
				SuspendLayout();
				// don't think the other plugins are even worth testing anymore, but this could easily be expanded to include them all --yoshi
				ComboBox comboPlugin = new() { Enabled = false, Items = { "GLideN64" }, SelectedIndex = 0 };
				Dictionary<PropertyInfo, IReadOnlyList<object>> propDict = new();
				foreach (var pi in typeof(N64SyncSettings.N64GLideN64PluginSettings).GetProperties())
				{
					if (pi.PropertyType == typeof(bool)) propDict[pi] = new object[] { true, false };
					else if (pi.PropertyType.IsEnum) propDict[pi] = Enum.GetValues(pi.PropertyType).Cast<object>().ToArray();
				}
				static object RandomElem(IReadOnlyList<object> a, Random rng) => a[rng.Next(a.Count)];
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
					var ss = ((N64) Emulator!).GetSyncSettings();
					var glidenSS = ss.GLideN64Plugin;
					foreach (var pi in props) pi.SetValue(obj: glidenSS, value: RandomElem(propDict[pi], rng));
					((MainForm) MainForm).PutCoreSyncSettings(ss);
				}
				SzButtonEx btnLightFuzz = new() { Size = new(200, 23), Text = "--> randomise some props" };
				btnLightFuzz.Click += (_, _) => Fuzz(limit: true);
				SzButtonEx btnHeavyFuzz = new() { Size = new(200, 23), Text = "--> randomise every prop" };
				btnHeavyFuzz.Click += (_, _) => Fuzz(limit: false);
				Controls.Add(new SingleColumnFLP { Controls = { comboPlugin, btnLightFuzz, btnHeavyFuzz } });
				ResumeLayout();
			}
		}

		private void AddDebugMenu()
		{
//			void OpenModal<T>()
//				where T : Form, new()
//			{
//				using T form = new();
//				this.ShowDialogAsChild(form);
//			}
			void OpenTool<T>() where T : class, IToolForm => Tools.Load<T>();
			ToolStripMenuItemEx debugMenu = new()
			{
				DropDownItems =
				{
					new DebugVSystemMenuItem("N64")
					{
						DropDownItems =
						{
							new DebugVSystemChildItem(N64VideoSettingsFuzzToolForm.TOOL_NAME, OpenTool<N64VideoSettingsFuzzToolForm>),
						},
					},
				},
				Image = Resources.Bug,
				Text = "&Debug Utilities",
			};
			debugMenu.DropDownOpened += (ddoSender, _) =>
			{
				var sysID = Emulator.SystemId;
				var coreName = Emulator.Attributes().CoreName;
				foreach (var item in ((ToolStripMenuItemEx) ddoSender).DropDownItems.OfType<DebugVSystemMenuItem>())
				{
					var groupEnabled = item.Text == sysID || item.ExtraSysIDs.Contains(sysID);
					foreach (var child in item.DropDownItems.Cast<DebugVSystemChildItem>().Where(static child => child.RequiresLoadedRom)) // RequiresLoadedRom == false => leave Enabled as default true
					{
						child.Enabled = groupEnabled && (child.RequiresCore is null || child.RequiresCore == coreName);
					}
				}
			};
			HelpSubMenu.DropDownItems.Insert(0, debugMenu);
		}
	}
}
#endif
