using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public partial class PathConfig : Form
	{
		private readonly PathEntryCollection _pathEntries;

		private readonly Action<string> _setMovieBackupPath;

		private readonly string _sysID;

		private static AutoCompleteStringCollection AutoCompleteOptions => new AutoCompleteStringCollection
		{
			"%recent%",
			"%exe%",
			"%rom%",
		};

		public PathConfig(
			PathEntryCollection pathEntries,
			string sysID,
			Action<string> setMovieBackupPath)
		{
			_pathEntries = pathEntries;
			_setMovieBackupPath = setMovieBackupPath;
			_sysID = sysID;
			InitializeComponent();
			SpecialCommandsBtn.Image = Properties.Resources.Help;
		}

		private void LoadSettings()
		{
			RecentForROMs.Checked = _pathEntries.UseRecentForRoms;

			DoTabs(_pathEntries.Paths, focusTabOfSystem: _sysID);
			DoRomToggle();
		}

		private void DoTabs(IReadOnlyList<PathEntry> pathCollection, string focusTabOfSystem)
		{
			bool IsTabPendingFocus(string system) => system == focusTabOfSystem || system.Split('_').Contains(focusTabOfSystem);

			int x = UIHelper.ScaleX(6);
			int textBoxWidth = UIHelper.ScaleX(70);
			int padding = UIHelper.ScaleX(5);
			int buttonWidth = UIHelper.ScaleX(26);
			int buttonHeight = UIHelper.ScaleY(23);
			int buttonOffsetY = -1; // To align the top with the TextBox I guess? Always 1 pixel regardless of scaling.
			int widgetOffset = UIHelper.ScaleX(85);
			int rowHeight = UIHelper.ScaleY(30);

			void PopulateTabPage(Control t, string system)
			{
				var paths = pathCollection
					.Where(p => p.System == system)
					.OrderBy(p => p.Ordinal)
					.ThenBy(p => p.Type);

				var y = UIHelper.ScaleY(14);
				foreach (var path in paths)
				{
					var box = new TextBox
					{
						Text = path.Path,
						Location = new Point(x, y),
						Width = textBoxWidth,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						MinimumSize = new Size(UIHelper.ScaleX(26), UIHelper.ScaleY(23)),
						AutoCompleteMode = AutoCompleteMode.SuggestAppend,
						AutoCompleteCustomSource = AutoCompleteOptions,
						AutoCompleteSource = AutoCompleteSource.CustomSource
					};

					var btn = new Button
					{
						Text = "",
						Image = Properties.Resources.OpenFile,
						Location = new Point(widgetOffset, y + buttonOffsetY),
						Size = new Size(buttonWidth, buttonHeight),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right
					};

					var tempBox = box;
					var tempPath = path.Type;
					var tempSystem = path.System;
					btn.Click += (sender, args) => BrowseFolder(tempBox, tempPath, tempSystem);

					var label = new Label
					{
						Text = path.Type,
						Location = new Point(widgetOffset + buttonWidth + padding, y + UIHelper.ScaleY(4)),
						Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					y += rowHeight;
				}
			}
			void AddTabPageForSystem(string system, string systemDisplayName)
			{
				var t = new TabPage
				{
					Name = system,
					Text = systemDisplayName,
					Width = UIHelper.ScaleX(200), // Initial Left/Width of child controls are based on this size.
					AutoScroll = true
				};
				PopulateTabPage(t, system);
				comboSystem.Items.Add(systemDisplayName);
				PathTabControl.TabPages.Add(t);
				if (IsTabPendingFocus(system))
				{
					comboSystem.SelectedIndex = comboSystem.Items.Count - 1; // event handler selects correct tab in inner TabControl
					tcMain.SelectTab(1);
				}
			}

			tcMain.Visible = false;

			PathTabControl.TabPages.Clear();
			var systems = _pathEntries.Paths.Select(static e => e.System).Distinct() // group entries by "system" (intentionally using instance field here, not parameter)
				.Select(sys => (SysGroup: sys, DisplayName: PathEntryCollection.GetDisplayNameFor(sys)))
				.OrderBy(tuple => tuple.DisplayName)
				.ToList();

			// add the Global tab first...
			tpGlobal.Name = PathEntryCollection.GLOBAL; // required for SaveSettings
			systems.RemoveAll(tuple => tuple.SysGroup == PathEntryCollection.GLOBAL);
			var hack = tpGlobal.Size.Width - UIHelper.ScaleX(220); // whyyyyyyyyyy
			textBoxWidth += hack;
			widgetOffset += hack;
			Size hack1 = new(17, 0); // also whyyyyyyyyyy
			PopulateTabPage(tpGlobal, PathEntryCollection.GLOBAL);
			tpGlobal.Controls[tpGlobal.Controls.Count - 1].Size -= hack1; // TextBox
			tpGlobal.Controls[tpGlobal.Controls.Count - 2].Location -= hack1; // Button
			textBoxWidth -= hack;
			widgetOffset -= hack;

			// ...then continue with the others (after removing unreleased systems in Release builds)
			if (!VersionInfo.DeveloperBuild)
			{
				var releasedCoreSysIDs = CoreInventory.Instance.AllCores.SelectMany(kvp => kvp.Value.Select(coreInfo => (SysID: kvp.Key, CoreInfo: coreInfo)))
					.Where(tuple => tuple.CoreInfo.CoreAttr.Released)
					.Select(tuple => tuple.SysID)
					.Distinct().ToList();
				releasedCoreSysIDs.Add(VSystemID.Raw.Libretro); // core not actually marked as released, but we still want to show it
				systems.RemoveAll(tuple => !releasedCoreSysIDs.Exists(sysID => PathEntryCollection.InGroup(sysID, tuple.SysGroup)));
			}
			foreach (var (sys, dispName) in systems) AddTabPageForSystem(sys, dispName);

			if (IsTabPendingFocus(PathEntryCollection.GLOBAL))
			{
				comboSystem.SelectedIndex = systems.FindIndex(tuple => tuple.SysGroup == VSystemID.Raw.NES); // event handler selects correct tab in inner TabControl
				// selected tab in tcMain is already 0 (Global)
			}

			tcMain.Visible = true;
		}

		private void BrowseFolder(TextBox box, string name, string system)
		{
			// Ugly hack, we don't want to pass in the system in for system base and global paths
			if (system is not null && (name is "Base" || system is "Global" || system == PathEntryCollection.GLOBAL))
			{
				BrowseFolder(box, name, system: null);
				return;
			}

			DialogResult result;
			string selectedPath;
			if (OSTailoredCode.IsUnixHost)
			{
				// FolderBrowserEx doesn't work in Mono for obvious reasons
				using var f = new FolderBrowserDialog
				{
					Description = $"Set the directory for {name}",
					SelectedPath = _pathEntries.AbsolutePathFor(box.Text, system)
				};
				result = f.ShowDialog();
				selectedPath = f.SelectedPath;
			}
			else
			{
				using var f = new FolderBrowserEx
				{
					Description = $"Set the directory for {name}",
					SelectedPath = _pathEntries.AbsolutePathFor(box.Text, system)
				};
				result = f.ShowDialog();
				selectedPath = f.SelectedPath;
			}
			if (result.IsOk())
			{
				box.Text = _pathEntries.TryMakeRelative(selectedPath, system);
			}
		}

		private void SaveSettings()
		{
			_pathEntries.UseRecentForRoms = RecentForROMs.Checked;

			foreach (var t in AllPathControls.OfType<TextBox>())
			{
				var pathEntry = _pathEntries.Paths.First(p => p.System == t.Parent.Name && p.Type == t.Name);
				pathEntry.Path = t.Text;
			}

			_setMovieBackupPath(_pathEntries.MovieBackupsAbsolutePath());
		}

		private void DoRomToggle()
		{
			foreach (var control in AllPathControls.Where(c => c.Name == "ROM"))
			{
				control.Enabled = !RecentForROMs.Checked;
			}
		}

		private IEnumerable<Control> AllPathControls
			=> new[] { tpGlobal }.Concat(PathTabControl.TabPages.Cast<TabPage>()).SelectMany(tp => tp.Controls());

		private void NewPathConfig_Load(object sender, EventArgs e)
		{
			LoadSettings();
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
			=> DoTabs(PathEntryCollection.Defaults.Value, PathEntryCollection.GLOBAL);

		private void Ok_Click(object sender, EventArgs e)
		{
			SaveSettings();

			_pathEntries.RefreshTempPath();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void comboSystem_SelectedIndexChanged(object sender, EventArgs e)
		{
			PathTabControl.SelectTab(((ComboBox) sender).SelectedIndex);
		}
	}
}
