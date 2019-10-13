using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class PathConfig : Form
	{
		// All path text boxes should do some kind of error checking
		// Config path under base, config will default to %exe%
		private void LockDownCores()
		{
			if (VersionInfo.DeveloperBuild)
			{
				return;
			}

			string[] coresToHide = { };

			foreach (var core in coresToHide)
			{
				PathTabControl.TabPages.Remove(
					PathTabControl.TabPages().FirstOrDefault(tp => tp.Name == core) ?? new TabPage());
			}
		}

		private static AutoCompleteStringCollection AutoCompleteOptions => new AutoCompleteStringCollection
		{
			"%recent%",
			"%exe%",
			"%rom%",
			".\\",
			"..\\",
		};

		public PathConfig()
		{
			InitializeComponent();
		}

		private void LoadSettings()
		{
			RecentForROMs.Checked = Global.Config.UseRecentForROMs;

			DoTabs(Global.Config.PathEntries.ToList());
			SetDefaultFocusedTab();
			DoRomToggle();
		}

		private void SetDefaultFocusedTab()
		{
			var tab = FindTabByName(Global.Game.System);
			if (tab != null)
			{
				PathTabControl.SelectTab(tab);
			}
		}

		private TabPage FindTabByName(string name)
		{
			var global = PathTabControl.TabPages
				.OfType<TabPage>()
				.First(tp => tp.Name.ToUpper().Contains("GLOBAL"));

			return PathTabControl.TabPages
				.OfType<TabPage>()
				.FirstOrDefault(tp => tp.Name.ToUpper().StartsWith(name.ToUpper()))
				?? global;
		}

		private void DoTabs(List<PathEntry> pathCollection)
		{
			PathTabControl.Visible = false;
			PathTabControl.TabPages.Clear();

			// Separate by system
			var systems = Global.Config.PathEntries
				.Select(s => s.SystemDisplayName)
				.Distinct()
				.ToList();
			systems.Sort();

			// Hacky way to put global first
			var global = systems.FirstOrDefault(s => s == "Global");
			systems.Remove(global);
			systems.Insert(0, global);

			var tabPages = new List<TabPage>(systems.Count);

			int x = UIHelper.ScaleX(6);
			int textboxWidth = UIHelper.ScaleX(70);
			int padding = UIHelper.ScaleX(5);
			int buttonWidth = UIHelper.ScaleX(26);
			int buttonHeight = UIHelper.ScaleY(23);
			int buttonOffsetY = -1; // To align the top with the textbox I guess? Always 1 pixel regardless of scaling.
			int widgetOffset = UIHelper.ScaleX(85);
			int rowHeight = UIHelper.ScaleY(30);

			foreach (var systemDisplayName in systems)
			{
				var systemId = Global.Config.PathEntries.First(p => p.SystemDisplayName == systemDisplayName).System;
				var t = new TabPage
				{
					Text = systemDisplayName,
					Name = systemId,
					Width = UIHelper.ScaleX(200), // Initial Left/Width of child controls are based on this size.
					AutoScroll = true
				};
				var paths = pathCollection
					.Where(p => p.System == systemId)
					.OrderBy(p => p.Ordinal)
					.ThenBy(p => p.Type);

				var y = UIHelper.ScaleY(14);
				foreach (var path in paths)
				{
					var box = new TextBox
					{
						Text = path.Path,
						Location = new Point(x, y),
						Width = textboxWidth,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						MinimumSize = new Size(UIHelper.ScaleX(26), UIHelper.ScaleY(23)),
						AutoCompleteMode = AutoCompleteMode.SuggestAppend,
						AutoCompleteCustomSource = AutoCompleteOptions,
						AutoCompleteSource = AutoCompleteSource.CustomSource,
					};

					var btn = new Button
					{
						Text = "",
						Image = Properties.Resources.OpenFile,
						Location = new Point(widgetOffset, y + buttonOffsetY),
						Size = new Size(buttonWidth, buttonHeight),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					var tempBox = box;
					var tempPath = path.Type;
					var tempSystem = path.System;
					btn.Click += delegate
					{
						BrowseFolder(tempBox, tempPath, tempSystem);
					};

					int infoPadding = UIHelper.ScaleX(0);
					if (t.Name.Contains("Global") && path.Type == "Firmware")
					{
						infoPadding = UIHelper.ScaleX(26);

						var firmwareButton = new Button
						{
							Name = "Global",
							Text = "",
							Image = Properties.Resources.Help,
							Location = new Point(UIHelper.ScaleX(115), y + buttonOffsetY),
							Size = new Size(buttonWidth, buttonHeight),
							Anchor = AnchorStyles.Top | AnchorStyles.Right
						};

						firmwareButton.Click += delegate
						{
							if (Owner is FirmwaresConfig)
							{
								MessageBox.Show("C-C-C-Combo Breaker!", "Nice try, but");
								return;
							}

							var f = new FirmwaresConfig { TargetSystem = "Global" };
							f.ShowDialog(this);
						};

						t.Controls.Add(firmwareButton);
					}

					var label = new Label
					{
						Text = path.Type,
						Location = new Point(widgetOffset + buttonWidth + padding + infoPadding, y + UIHelper.ScaleY(4)),
						Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					y += rowHeight;
				}

				var sys = systemDisplayName;
				if (systemDisplayName == "PCE") // Hack
				{
					sys = "PCECD";
				}

				tabPages.Add(t);
			}

			PathTabControl.TabPages.AddRange(tabPages.ToArray());
			PathTabControl.Visible = true;
		}

		private static void BrowseFolder(TextBox box, string name, string system)
		{
			// Ugly hack, we don't want to pass in the system in for system base and global paths
			if (name == "Base" || system == "Global" || system == "Global_NULL")
			{
				system = null;
			}

			var f = new FolderBrowserEx
			{
				Description = $"Set the directory for {name}",
				SelectedPath = PathManager.MakeAbsolutePath(box.Text, system)
			};
			var result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				box.Text = PathManager.TryMakeRelative(f.SelectedPath, system);
			}
		}

		private void SaveSettings()
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;

			foreach (var t in AllPathBoxes)
			{
				var pathEntry = Global.Config.PathEntries.First(p => p.System == t.Parent.Name && p.Type == t.Name);
				pathEntry.Path = t.Text;
			}
		}

		private void DoRomToggle()
		{
			foreach (var control in AllPathControls.Where(c => c.Name == "ROM"))
			{
				control.Enabled = !RecentForROMs.Checked;
			}
		}

		private IEnumerable<TextBox> AllPathBoxes
		{
			get
			{
				var allPathBoxes = new List<TextBox>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					allPathBoxes.AddRange(tp.Controls.OfType<TextBox>());
				}

				return allPathBoxes;
			}
		}

		private IEnumerable<Control> AllPathControls
		{
			get
			{
				var allPathControls = new List<Control>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					allPathControls.AddRange(tp.Controls());
				}

				return allPathControls;
			}
		}

		#region Events

		private void NewPathConfig_Load(object sender, EventArgs e)
		{
			LoadSettings();
			LockDownCores();
		}

		private void RecentForRoms_CheckedChanged(object sender, EventArgs e)
		{
			DoRomToggle();
		}

		private void SpecialCommandsBtn_Click(object sender, EventArgs e)
		{
			new PathInfo().Show();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			DoTabs(PathEntryCollection.DefaultValues);
			SetDefaultFocusedTab();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			SaveSettings();

			PathManager.RefreshTempPath();

			GlobalWin.OSD.AddMessage("Path settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Path config aborted");
			Close();
		}

		#endregion
	}
}
