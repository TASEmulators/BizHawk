using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

//todo - add some more options for libretro types

namespace BizHawk.Client.EmuHawk.ToolExtensions
{
	public static class ToolExtensions
	{
		public static ToolStripItem[] RecentMenu(
			this RecentFiles recent,
			IDialogParent mainForm,
			Action<string> loadFileCallback,
			string entrySemantic,
			bool noAutoload = false,
			bool romLoading = false)
		{
			var items = new List<ToolStripItem>();

			if (recent.Empty)
			{
				var none = new ToolStripMenuItem { Enabled = false, Text = "None" };
				items.Add(none);
			}
			else
			{
				foreach (var filename in recent)
				{
					string caption = filename;
					string path = filename;
					string physicalPath = filename;
					bool crazyStuff = true;

					//sentinel for newer format OpenAdvanced type code
					if (romLoading)
					{
						if (filename.StartsWith('*'))
						{
							var oa = OpenAdvancedSerializer.ParseWithLegacy(filename);
							caption = oa.DisplayName;

							crazyStuff = false;
							if (oa is OpenAdvanced_OpenRom openRom)
							{
								crazyStuff = true;
								physicalPath = openRom.Path;
							}
						}
					}

					// TODO - do TSMI and TSDD need disposing? yuck
					var item = new ToolStripMenuItem { Text = caption.Replace("&", "&&") };
					items.Add(item);

					item.Click += (o, ev) =>
					{
						loadFileCallback(path);
					};

					var tsdd = new ToolStripDropDownMenu();

					if (crazyStuff)
					{
						//TODO - use standard methods to split filename (hawkfile acquire?)
						var hf = new HawkFile(physicalPath ?? throw new Exception("this will probably never appear but I can't be bothered checking --yoshi"), delayIOAndDearchive: true);
						bool canExplore = File.Exists(hf.FullPathWithoutMember);

						if (canExplore)
						{
							//make a menuitem to show the last modified timestamp
							var timestamp = File.GetLastWriteTime(hf.FullPathWithoutMember);
							var tsmiTimestamp = new ToolStripLabel { Text = timestamp.ToString(DateTimeFormatInfo.InvariantInfo) };

							tsdd.Items.Add(tsmiTimestamp);
							tsdd.Items.Add(new ToolStripSeparator());

							if (hf.IsArchive)
							{
								//make a menuitem to let you copy the path
								var tsmiCopyCanonicalPath = new ToolStripMenuItem { Text = "&Copy Canonical Path" };
								tsmiCopyCanonicalPath.Click += (o, ev) => { Clipboard.SetText(physicalPath); };
								tsdd.Items.Add(tsmiCopyCanonicalPath);

								var tsmiCopyArchivePath = new ToolStripMenuItem { Text = "Copy Archive Path" };
								tsmiCopyArchivePath.Click += (o, ev) => { Clipboard.SetText(hf.FullPathWithoutMember); };
								tsdd.Items.Add(tsmiCopyArchivePath);

								var tsmiOpenArchive = new ToolStripMenuItem { Text = "Open &Archive" };
								tsmiOpenArchive.Click += (o, ev) => { System.Diagnostics.Process.Start(hf.FullPathWithoutMember); };
								tsdd.Items.Add(tsmiOpenArchive);
							}
							else
							{
								// make a menuitem to let you copy the path
								var tsmiCopyPath = new ToolStripMenuItem { Text = "&Copy Path" };
								tsmiCopyPath.Click += (o, ev) => { Clipboard.SetText(physicalPath); };
								tsdd.Items.Add(tsmiCopyPath);
							}

							tsdd.Items.Add(new ToolStripSeparator());

							// make a menuitem to let you explore to it
							var tsmiExplore = new ToolStripMenuItem { Text = "&Explore" };
							string explorePath = $"\"{hf.FullPathWithoutMember}\"";
							tsmiExplore.Click += (o, ev) => { System.Diagnostics.Process.Start("explorer.exe", $"/select, {explorePath}"); };
							tsdd.Items.Add(tsmiExplore);

							var tsmiCopyFile = new ToolStripMenuItem { Text = "Copy &File" };
							var lame = new System.Collections.Specialized.StringCollection
							{
								hf.FullPathWithoutMember
							};

							tsmiCopyFile.Click += (o, ev) => { Clipboard.SetFileDropList(lame); };
							tsdd.Items.Add(tsmiCopyFile);

							if (!OSTailoredCode.IsUnixHost)
							{
								var tsmiTest = new ToolStripMenuItem { Text = "&Shell Context Menu" };
								tsmiTest.Click += (o, ev) =>
								{
									var tsddi = (ToolStripDropDownItem)o;
									tsddi.Owner.Update();
									Win32ShellContextMenu.ShowContextMenu(hf.FullPathWithoutMember, tsddi.Owner.Handle, tsddi.Owner.Location.X, tsddi.Owner.Location.Y);
								};
								tsdd.Items.Add(tsmiTest);
							}

							tsdd.Items.Add(new ToolStripSeparator());
						}
						else
						{
							//make a menuitem to show the last modified timestamp
							var tsmiMissingFile = new ToolStripLabel { Text = "-Missing-" };
							tsdd.Items.Add(tsmiMissingFile);
							tsdd.Items.Add(new ToolStripSeparator());
						}

					} //crazystuff

					//in any case, make a menuitem to let you remove the item
					var tsmiRemovePath = new ToolStripMenuItem { Text = "&Remove" };
					tsmiRemovePath.Click += (o, ev) => {
						recent.Remove(path);
					};
					tsdd.Items.Add(tsmiRemovePath);

#if false //experiment of popping open a submenu. doesn't work well.
					item.MouseDown += (o, mev) =>
					{
						if (mev.Button != MouseButtons.Right) return;
						//location of the menu containing this item that was just right-clicked
						var pos = item.Owner.Bounds.Location;
						//the offset within that menu of this item
						pos.Offset(item.Bounds.Location);
						//the offset of the click
						pos.Offset(mev.Location);
//						tsdd.OwnerItem = item; //has interesting promise, but breaks things otherwise
						tsdd.Show(pos);
					};
#endif

					//just add it to the submenu for now. seems to work well enough, even though its a bit odd
					item.MouseDown += (o, mev) =>
					{
						if (mev.Button != MouseButtons.Right) return;
						if (item.DropDown != null)
							item.DropDown = tsdd;
						item.ShowDropDown();
					};
				}
			}

			items.Add(new ToolStripSeparator());

			var clearItem = new ToolStripMenuItem { Text = "&Clear", Enabled = !recent.Frozen };
			clearItem.Click += (o, ev) => recent.Clear();
			items.Add(clearItem);

			var freezeItem = new ToolStripMenuItem
			{
				Text = recent.Frozen ? "&Unfreeze" : "&Freeze",
				Image = recent.Frozen ? Properties.Resources.Unfreeze : Properties.Resources.Freeze
			};
			freezeItem.Click += (_, _) => recent.Frozen = !recent.Frozen;
			items.Add(freezeItem);

			if (!noAutoload)
			{
				var auto = new ToolStripMenuItem { Text = $"&Autoload {entrySemantic}", Checked = recent.AutoLoad };
				auto.Click += (o, ev) => recent.ToggleAutoLoad();
				items.Add(auto);
			}

			var settingsItem = new ToolStripMenuItem { Text = "&Recent Settings..." };
			settingsItem.Click += (o, ev) =>
			{
				using var prompt = new InputPrompt
				{
					TextInputType = InputPrompt.InputType.Unsigned,
					Message = "Number of recent files to track",
					InitialValue = recent.MAX_RECENT_FILES.ToString()
				};
				if (!mainForm.ShowDialogWithTempMute(prompt).IsOk()) return;
				var val = int.Parse(prompt.PromptText);
				if (val > 0) recent.MAX_RECENT_FILES = val;
			};
			items.Add(settingsItem);

			return items.ToArray();
		}

		public static void HandleLoadError(this RecentFiles recent, IMainFormForTools mainForm, string path, string encodedPath = null)
		{
			mainForm.DoWithTempMute(() =>
			{
				if (recent.Frozen)
				{
					mainForm.ShowMessageBox($"Could not open {path}", "File not found", EMsgBoxIcon.Error);
				}
				else
				{
					// ensure topmost, not to have to minimize everything to see and use our modal window, if it somehow got covered
					var result = MessageBox.Show(new Form { TopMost = true }, $"Could not open {path}\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
					if (result == DialogResult.Yes)
					{
						recent.Remove(encodedPath ?? path);
					}
				}
			});
		}

		public static IEnumerable<ToolStripItem> MenuItems(this IMemoryDomains domains, Action<string> setCallback, string selected = "", int? maxSize = null)
			=> domains.Select(domain =>
			{
				var name = domain.Name;
				var item = new ToolStripMenuItem
				{
					Text = name,
					Enabled = !(maxSize.HasValue && domain.Size > maxSize.Value),
					Checked = name == selected
				};
				item.Click += (o, ev) => setCallback(name);
				return item;
			});
	}
}
