#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk.ForDebugging
{
	internal sealed class FirmwareAutopatchDebugToolForm : ToolFormBase
	{
		public const string TOOL_NAME = "Manual Firmware Autopatching Tool";

		protected override string WindowTitleStatic
			=> TOOL_NAME;

		public FirmwareAutopatchDebugToolForm()
		{
			static string LabelFragment(string hash)
				=> $"{hash.Substring(startIndex: 0, length: 8)}... {FirmwareDatabase.FirmwareFilesByHash[hash].RecommendedName}";
			List<(string Label, FirmwarePatchOption PatchOption)> patches = FirmwareDatabase.AllPatches
				.Select(static fpo => ($"{LabelFragment(fpo.BaseHash)} --> {LabelFragment(fpo.TargetHash)}", fpo)).ToList();
			patches.Sort(static (a, b) => string.CompareOrdinal(a.Label, b.Label));
			ComboBox comboPatchsets = new() { Size = new(300, 23) };
			foreach (var tuple in patches) comboPatchsets.Items.Add(tuple.Label);
			SzTextBoxEx txtBaseFile = new() { Size = new(224, 23) };
			void ShowBaseFilePicker()
			{
				var filename = this.ShowFileSaveDialog(initDir: Config!.PathEntries.FirmwareAbsolutePath());
				if (filename is not null) txtBaseFile.Text = filename;
			}
			CheckBoxEx cbDryRun = new() { Checked = true, Text = "dry run (skip writing to disk)" };
			void DoPatch()
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
			}
			ClientSize = new(320, 200);
			SuspendLayout();
			Controls.Add(new SingleColumnFLP
			{
				Controls =
				{
					new LabelEx { Text = "apply" },
					comboPatchsets,
					new LabelEx { Text = "to file" },
					new SingleRowFLP { Controls = { txtBaseFile, GenControl.Button("(browse)", width: 75, ShowBaseFilePicker) } },
					cbDryRun,
					new LabelEx { Text = "patched files are saved in dir set as \"Temp Files\"" },
					GenControl.Button("--> patch", width: 75, DoPatch),
				},
			});
			ResumeLayout();
		}
	}
}
