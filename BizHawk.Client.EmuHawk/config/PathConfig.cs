using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PathConfig : Form
	{
		//All path text boxes should do some kind of error checking
		//config path under base, config will default to %exe%

		private void LockDownCores()
		{
			if (!VersionInfo.INTERIM)
			{
				string[] coresToHide = { "PSX", "GBA", "INTV", "C64", "GEN" };

				foreach (string core in coresToHide)
				{
					PathTabControl.TabPages.Remove(
						AllTabPages.FirstOrDefault(x => x.Name == core) ?? new TabPage()
					);
				}
			}
		}

		private AutoCompleteStringCollection AutoCompleteOptions
		{
			get
			{
				return new AutoCompleteStringCollection
				{
					"%recent%",
					"%exe%",
					".\\",
					"..\\",
				};
			}
		}

		public PathConfig()
		{
			InitializeComponent();
		}

		private void NewPathConfig_Load(object sender, EventArgs e)
		{
			LoadSettings();
			LockDownCores();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveSettings();
			GlobalWin.OSD.AddMessage("Path settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Path config aborted");
			Close();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void LoadSettings()
		{
			RecentForROMs.Checked = Global.Config.UseRecentForROMs;
			BasePathBox.Text = Global.Config.PathEntries.GlobalBase;
			
			StartTabPages();
			//DoTabs(Global.Config.PathEntries.Paths);
			
			SetDefaultFocusedTab();
			DoRomToggle();
		}

		private void SetDefaultFocusedTab()
		{
			PathTabControl.SelectTab(FindTabByName(Global.Game.System));
		}

		private TabPage FindTabByName(string name)
		{
			IEnumerable<TabPage> query = from p in PathTabControl.TabPages.OfType<TabPage>() select p;
			var tab = query.FirstOrDefault(x => x.Name.ToUpper().Contains(name.ToUpper()));
			if (tab == null)
			{
				return new TabPage();
			}
			else
			{
				return tab;
			}
		}

		private void StartTabPages()
		{
			PathTabControl.TabPages.Clear();
			//Separate by system
			List<string> systems = Global.Config.PathEntries.Select(x => x.SystemDisplayName).Distinct().ToList();
			systems.Sort();
			foreach (string systemDisplayName in systems)
			{
				PathTabControl.TabPages.Add(new TabPage
				{
					Text = systemDisplayName,
					Name = Global.Config.PathEntries.FirstOrDefault(x => x.SystemDisplayName == systemDisplayName).System
				});
			}
		}

		private void DoTabPage(TabPage tabPage)
		{
			const int xpos = 6;
			int textboxWidth = tabPage.Width - 150;
			const int padding = 5;
			const int buttonWidth = 26;
			int widgetOffset = textboxWidth + 15;
			const int rowHeight = 30;
			List<PathEntry> paths = Global.Config.PathEntries.Where(x => x.System == tabPage.Name).OrderBy(x => x.Ordinal).ThenBy(x => x.Type).ToList();

			int ypos = 14;

			foreach (var path in paths)
			{
				TextBox box = new TextBox
				{
					Text = path.Path,
					Location = new Point(xpos, ypos),
					Width = textboxWidth,
					Name = path.Type,
					Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
					MinimumSize = new Size(26, 23),
					AutoCompleteMode = AutoCompleteMode.SuggestAppend,
					AutoCompleteCustomSource = AutoCompleteOptions,
					AutoCompleteSource = AutoCompleteSource.CustomSource,
				};

				Button btn = new Button
				{
					Text = String.Empty,
					Image = Properties.Resources.OpenFile,
					Location = new Point(widgetOffset, ypos - 1),
					Width = buttonWidth,
					Name = path.Type,
					Anchor = AnchorStyles.Top | AnchorStyles.Right,
				};

				TextBox tempBox = box;
				string tempPath = path.Type;
				string tempSystem = path.System;
				btn.Click += delegate
				{
					BrowseFolder(tempBox, tempPath, tempSystem);
				};

				Label label = new Label
				{
					Text = path.Type,
					Location = new Point(widgetOffset + buttonWidth + padding, ypos + 4),
					Width = 100,
					Name = path.Type,
					Anchor = AnchorStyles.Top | AnchorStyles.Right,
				};

				tabPage.Controls.Add(label);
				tabPage.Controls.Add(btn);
				tabPage.Controls.Add(box);

				ypos += rowHeight;
			}

			string sys = tabPage.Name;
			if (tabPage.Name == "PCE") //Hack
			{
				sys = "PCECD";
			}

			bool hasFirmwares = FirmwaresConfig.SystemGroupNames.Any(x => x.Key == sys);

			if (hasFirmwares)
			{
				Button firmwareButton = new Button
				{
					Name = sys,
					Text = "&Firmware",
					Location = new Point(xpos, ypos),
					Width = 75,
				};
				firmwareButton.Click += delegate
				{
					FirmwaresConfig f = new FirmwaresConfig { TargetSystem = sys };
					f.ShowDialog();
				};

				tabPage.Controls.Add(firmwareButton);
			}
		}

		//TODO: this is only used by the defaults button, refactor since it is now redundant code (will have to force the rebuilding of all tabpages, currently they only build as necessar
		private void DoTabs(List<PathEntry> pathCollection)
		{
			PathTabControl.SuspendLayout();
			PathTabControl.TabPages.Clear();

			//Separate by system
			List<string> systems = Global.Config.PathEntries.Select(x => x.SystemDisplayName).Distinct().ToList();
			systems.Sort();

			//Hacky way to put global first
			string global = systems.FirstOrDefault(x => x == "Global");
			systems.Remove(global);
			systems.Insert(0, global);

			List<TabPage> tabPages = new List<TabPage>(systems.Count);

			const int _x = 6;
			const int textboxWidth = 70;
			const int padding = 5;
			const int buttonWidth = 26;
			const int widgetOffset = 85;
			const int rowHeight = 30;

			foreach (string systemDisplayName in systems)
			{
				string systemId = Global.Config.PathEntries.FirstOrDefault(x => x.SystemDisplayName == systemDisplayName).System;
				TabPage t = new TabPage
				{
					Text = systemDisplayName,
					Name = systemId,
				};
				List<PathEntry> paths = pathCollection.Where(x => x.System == systemId).OrderBy(x => x.Ordinal).ThenBy(x => x.Type).ToList();

				int _y = 14;
				foreach (var path in paths)
				{
					TextBox box = new TextBox
					{
						Text = path.Path,
						Location = new Point(_x, _y),
						Width = textboxWidth,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						MinimumSize = new Size(26, 23),
						AutoCompleteMode = AutoCompleteMode.SuggestAppend,
						AutoCompleteCustomSource = AutoCompleteOptions,
						AutoCompleteSource = AutoCompleteSource.CustomSource,
					};

					Button btn = new Button
					{
						Text = String.Empty,
						Image = Properties.Resources.OpenFile,
						Location = new Point(widgetOffset, _y - 1),
						Width = buttonWidth,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					TextBox tempBox = box;
					string tempPath = path.Type;
					string tempSystem = path.System;
					btn.Click += delegate
					{
						BrowseFolder(tempBox, tempPath, tempSystem);
					};

					Label label = new Label
						{
						Text = path.Type,
						Location = new Point(widgetOffset + buttonWidth + padding, _y + 4),
						Width = 100,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					_y += rowHeight;
				}

				string sys = systemDisplayName;
				if (systemDisplayName == "PCE") //Hack
				{
					sys = "PCECD";
				}

				bool hasFirmwares = FirmwaresConfig.SystemGroupNames.Any(x => x.Key == sys);

				if (hasFirmwares)
				{
					Button firmwareButton = new Button
					{
						Name = sys,
						Text = "&Firmware",
						Location = new Point(_x, _y),
						Width = 75,
					};
					firmwareButton.Click += delegate
					{
						FirmwaresConfig f = new FirmwaresConfig {TargetSystem = sys};
						f.ShowDialog();
					};

					t.Controls.Add(firmwareButton);
				}
				tabPages.Add(t);
				
			}
			PathTabControl.TabPages.AddRange(tabPages.ToArray());
			PathTabControl.ResumeLayout();
		}

		private void BrowseFolder(TextBox box, string name, string system)
		{
			//Ugly hack, we don't want to pass in the system in for system base and global paths
			if (name == "Base" || system == "Global")
			{
				system = null;
			}

			var f = new FolderBrowserDialog
			{
				Description = "Set the directory for " + name,
				SelectedPath = PathManager.MakeAbsolutePath(box.Text, system)
			};
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				box.Text = PathManager.TryMakeRelative(f.SelectedPath, system);
			}
		}

		private void SaveSettings()
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;
			Global.Config.PathEntries["Global", "Base"].Path = BasePathBox.Text;

			foreach (TextBox t in AllPathBoxes)
			{
				PathEntry path_entry = Global.Config.PathEntries.FirstOrDefault(x => x.System == t.Parent.Name && x.Type == t.Name);
				path_entry.Path = t.Text;
			}
		}

		private void BrowseBase_Click(object sender, EventArgs e)
		{
			var f = new FolderBrowserDialog
			{
				Description = "Set the directory for the base global path",
				SelectedPath = PathManager.MakeAbsolutePath(BasePathBox.Text, null)
			};
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				BasePathBox.Text = f.SelectedPath;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			new PathInfo().Show();
		}

		private void RecentForROMs_CheckedChanged(object sender, EventArgs e)
		{
			DoRomToggle();
		}

		private void DoRomToggle()
		{
			List<Control> pcontrols = AllPathControls.Where(x => x.Name == "ROM").ToList();
			foreach (Control c in pcontrols)
			{
				c.Enabled = !RecentForROMs.Checked;
			}
		}

		private IEnumerable<TextBox> AllPathBoxes
		{
			get
			{
				List<TextBox> _AllPathBoxes = new List<TextBox>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					IEnumerable<TextBox> boxes = from b in tp.Controls.OfType<TextBox>() select b;
					_AllPathBoxes.AddRange(boxes);
				}
				return _AllPathBoxes;
			}
		}

		private IEnumerable<Control> AllPathControls
		{
			get
			{
				List<Control> _AllPathControls = new List<Control>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					IEnumerable<Control> control = from c in tp.Controls.OfType<Control>() select c;
					_AllPathControls.AddRange(control);
				}
				return _AllPathControls;
			}
		}

		private IEnumerable<TabPage> AllTabPages
		{
			get
			{
				return PathTabControl.TabPages.Cast<TabPage>().ToList();
			}
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			DoTabs(PathEntryCollection.DefaultValues);
		}

		private void PathTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			var tabPage = (sender as TabControl).SelectedTab;
			if (tabPage.Controls.Count == 0)
			{
				DoTabPage((sender as TabControl).SelectedTab);
			}
		}
	}
}
