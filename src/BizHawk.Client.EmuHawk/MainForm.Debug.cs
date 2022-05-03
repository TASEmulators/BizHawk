#nullable enable
#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
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
			public readonly IReadOnlyCollection<string> SysIDs;

			public DebugVSystemMenuItem(string labelText, params string[] extraSysIDs)
			{
				SysIDs = new[] { labelText }.Concat(extraSysIDs).ToHashSet();
				Text = labelText;
			}
		}

		private sealed class FirmwareAutopatchDebugToolForm : ToolFormBase
		{
			public const string TOOL_NAME = "Manual Firmware Autopatching Tool";

			protected override string WindowTitleStatic { get; } = TOOL_NAME;

			public FirmwareAutopatchDebugToolForm()
			{
				static string LabelFragment(string hash) => $"{hash.Substring(0, 8)}... {FirmwareDatabase.FirmwareFilesByHash[hash].RecommendedName}";
				List<(string Label, FirmwarePatchOption PatchOption)> patches = FirmwareDatabase.AllPatches.Select(static fpo => ($"{LabelFragment(fpo.BaseHash)} --> {LabelFragment(fpo.TargetHash)}", fpo)).ToList();
				patches.Sort(static (a, b) => a.Label.CompareTo(b.Label));
				ComboBox comboPatchsets = new() { Size = new(300, 23) };
				foreach (var tuple in patches) comboPatchsets.Items.Add(tuple.Label);
				SzTextBoxEx txtBaseFile = new() { Size = new(224, 23) };
				SzButtonEx btnBaseFilePicker = new() { Size = new(75, 23), Text = "(browse)" };
				btnBaseFilePicker.Click += (_, _) =>
				{
					using OpenFileDialog ofd = new() { InitialDirectory = Config!.PathEntries.FirmwareAbsolutePath() };
					this.ShowDialogAsChild(ofd);
					txtBaseFile.Text = ofd.FileName;
				};
				CheckBoxEx cbDryRun = new() { Checked = true, Text = "dry run (skip writing to disk)" };
				SzButtonEx btnPatch = new() { Size = new(75, 23), Text = "--> patch" };
				btnPatch.Click += (_, _) =>
				{
					var fpo = patches[comboPatchsets.SelectedIndex].PatchOption;
					try
					{
						if (!cbDryRun.Checked)
						{
							var (filePath, _, _) = FirmwareManager.PerformPatchOnDisk(txtBaseFile.Text, in fpo, Config!.PathEntries);
							// if the base file (or patchset) is wrong, too bad
							this.ModalMessageBox($"wrote {filePath}");
							return;
						}
						var @base = File.ReadAllBytes(txtBaseFile.Text);
						var (_, actualHash) = FirmwareManager.PerformPatchInMemory(@base, in fpo);
						if (actualHash == fpo.TargetHash)
						{
							this.ModalMessageBox("success");
							return;
						}
						// else something happened, figure out what it was
						var baseHash = SHA1Checksum.ComputeDigestHex(@base);
						this.ModalMessageBox(baseHash == fpo.BaseHash
							? $"patchset declared with target\nSHA1:{fpo.TargetHash}\nbut produced\nSHA1:{actualHash}\n(is the patch wrong, or the hash?)"
							: $"patchset declared for base\nSHA1:{fpo.BaseHash}\nbut\nSHA1:{baseHash}\nwas provided");
					}
					catch (Exception e)
					{
						this.ModalMessageBox($"caught {e.GetType().Name}:\n{e}");
					}
				};
				ClientSize = new(320, 200);
				SuspendLayout();
				Controls.Add(new SingleColumnFLP
				{
					Controls =
					{
						new LabelEx { Text = "apply" },
						comboPatchsets,
						new LabelEx { Text = "to file" },
						new SingleRowFLP { Controls = { txtBaseFile, btnBaseFilePicker } },
						cbDryRun,
						new LabelEx { Text = "patched files are saved in dir set as \"Temp Files\"" },
						btnPatch,
					}
				});
				ResumeLayout();
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
			ToolStripMenuItemEx firmwareAutopatchDebugItem = new() { Text = FirmwareAutopatchDebugToolForm.TOOL_NAME };
			firmwareAutopatchDebugItem.Click += (_, _) => OpenTool<FirmwareAutopatchDebugToolForm>();
			ToolStripMenuItemEx debugMenu = new()
			{
				DropDownItems =
				{
					new ToolStripMenuItemEx
					{
						DropDownItems =
						{
							firmwareAutopatchDebugItem,
						},
						Text = "Firmware",
					},
					new ToolStripSeparatorEx(),
					new DebugVSystemMenuItem(VSystemID.Raw.GB, VSystemID.Raw.GBC)
					{
						DropDownItems =
						{
							new DebugVSystemChildItem(
								"Debug SameBoy States",
								() => ((Sameboy) Emulator).DebugSameBoyState())
							{
								RequiresCore = CoreNames.Sameboy,
							},
						},
					},
					new DebugVSystemMenuItem(VSystemID.Raw.GBA)
					{
						DropDownItems =
						{
							new DebugVSystemChildItem(
								"Reproduce #2805",
								() => ((MGBAMemoryCallbackSystem) Emulator.AsDebuggable().MemoryCallbacks).Debug2805())
							{
								RequiresCore = CoreNames.Mgba,
							},
						},
					},
					new DebugVSystemMenuItem(VSystemID.Raw.N64)
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
					var groupEnabled = item.SysIDs.Contains(sysID);
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
