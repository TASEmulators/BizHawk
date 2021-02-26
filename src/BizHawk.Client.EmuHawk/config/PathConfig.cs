using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PathConfig : Form
	{
		private readonly FirmwareManager _firmwareManager;

		private readonly IDictionary<string, string> _firmwareUserSpecifications;

		private readonly IGameInfo _game;

		private readonly IMainFormForConfig _mainForm;

		private readonly PathEntryCollection _pathEntries;

		// All path text boxes should do some kind of error checking
		// Config path under base, config will default to %exe%
		private void LockDownCores()
		{
			if (VersionInfo.DeveloperBuild)
			{
				return;
			}

			string[] coresToHide = { "GB4x", "O2", "ChannelF", "AmstradCPC" };

			foreach (var core in coresToHide)
			{
				var tabPage = PathTabControl.TabPages().First(tp => tp.Name == core);
				PathTabControl.TabPages.Remove(tabPage);
			}
		}

		private static AutoCompleteStringCollection AutoCompleteOptions => new AutoCompleteStringCollection
		{
			"%recent%",
			"%exe%",
			"%rom%",
			".\\",
			"..\\"
		};

		public PathConfig(
			FirmwareManager firmwareManager,
			IDictionary<string, string> firmwareUserSpecifications,
			IGameInfo game,
			IMainFormForConfig mainForm,
			PathEntryCollection pathEntries)
		{
			_firmwareManager = firmwareManager;
			_firmwareUserSpecifications = firmwareUserSpecifications;
			_game = game;
			_mainForm = mainForm;
			_pathEntries = pathEntries;
			InitializeComponent();
			SpecialCommandsBtn.Image = Properties.Resources.Help;
		}

		private void LoadSettings()
		{
			RecentForROMs.Checked = _pathEntries.UseRecentForRoms;

			DoTabs(_pathEntries.ToList(), _game.System);
			DoRomToggle();
		}

		private void DoTabs(IList<PathEntry> pathCollection, string focusTabOfSystem)
		{
			int x = UIHelper.ScaleX(6);
			int textBoxWidth = UIHelper.ScaleX(70);
			int padding = UIHelper.ScaleX(5);
			int buttonWidth = UIHelper.ScaleX(26);
			int buttonHeight = UIHelper.ScaleY(23);
			int buttonOffsetY = -1; // To align the top with the TextBox I guess? Always 1 pixel regardless of scaling.
			int widgetOffset = UIHelper.ScaleX(85);
			int rowHeight = UIHelper.ScaleY(30);

			void AddTabPageForSystem(string system, string systemDisplayName)
			{
				var t = new TabPage
				{
					Name = system,
					Text = systemDisplayName,
					Width = UIHelper.ScaleX(200), // Initial Left/Width of child controls are based on this size.
					AutoScroll = true
				};
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

						firmwareButton.Click += (sender, e) =>
						{
							if (Owner is FirmwaresConfig)
							{
								MessageBox.Show("C-C-C-Combo Breaker!", "Nice try, but");
								return;
							}

							using var f = new FirmwaresConfig(_firmwareManager, _firmwareUserSpecifications, _game, _mainForm, _pathEntries) { TargetSystem = "Global" };
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
						Anchor = AnchorStyles.Top | AnchorStyles.Right
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					y += rowHeight;
				}

				PathTabControl.TabPages.Add(t);
				if (system == focusTabOfSystem || system.Split('_').Contains(focusTabOfSystem))
				{
					PathTabControl.SelectTab(PathTabControl.TabPages.Count - 1);
				}
			}

			PathTabControl.Visible = false;

			PathTabControl.TabPages.Clear();
			var systems = _pathEntries.Select(e => e.System).Distinct() // group entries by "system" (intentionally using instance field here, not parameter)
				.Select(sys => (sys, PathEntryCollection.GetDisplayNameFor(sys)))
				.OrderBy(tuple => tuple.Item2) // sorted alphabetically by display name
				.ToList();
			// add the Global tab first...
			const string idGlobal = "Global_NULL";
			systems.RemoveAll(tuple => tuple.Item1 == idGlobal);
			AddTabPageForSystem(idGlobal, PathEntryCollection.GetDisplayNameFor(idGlobal));
			// ...then continue with the others
			foreach (var (sys, dispName) in systems) AddTabPageForSystem(sys, dispName);

			PathTabControl.Visible = true;
		}

		private void BrowseFolder(TextBox box, string name, string system)
		{
			// Ugly hack, we don't want to pass in the system in for system base and global paths
			if (name == "Base" || system == "Global" || system == "Global_NULL")
			{
				system = null;
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

			foreach (var t in AllPathBoxes)
			{
				var pathEntry = _pathEntries.First(p => p.System == t.Parent.Name && p.Type == t.Name);
				pathEntry.Path = t.Text;
			}

			_mainForm.MovieSession.BackupDirectory = _pathEntries.MovieBackupsAbsolutePath();
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
			=> DoTabs(PathEntryCollection.DefaultValues, "Global_NULL");

		private void Ok_Click(object sender, EventArgs e)
		{
			SaveSettings();

			_pathEntries.RefreshTempPath();
			_mainForm.AddOnScreenMessage("Path settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			_mainForm.AddOnScreenMessage("Path config aborted");
			Close();
		}
	}
}
