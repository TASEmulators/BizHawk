using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewPathConfig : Form
	{
		//All path text boxes should do some kind of error checking
		//config path under base, config will default to %exe%

		private void LockDownCores()
		{
			if (!MainForm.INTERIM)
			{
				string[] coresToHide = { "PSX", "GBA", "INTV", "C64", "GEN" };

				foreach (string core in coresToHide)
				{
					TabPage tp = AllTabPages.FirstOrDefault(x => x.Name == core);
					PathTabControl.TabPages.Remove(tp);
				}
			}
		}

		private AutoCompleteStringCollection AutoCompleteOptions
		{
			get
			{
				return new AutoCompleteStringCollection()
                {
                    "%recent%",
                    "%exe%",
                    ".\\",
                    "..\\",
                };
			}
		}

		public NewPathConfig()
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
			Global.OSD.AddMessage("Path settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Path config aborted");
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
			DoTabs(Global.Config.PathEntries.Paths);
			SetDefaultFocusedTab();
			DoROMToggle();
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

		private void DoTabs(List<PathEntry> PathCollection)
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

			foreach (string systemDisplayName in systems)
			{
				string systemId = Global.Config.PathEntries.FirstOrDefault(x => x.SystemDisplayName == systemDisplayName).System;
				TabPage t = new TabPage()
				{
					Text = systemDisplayName,
					Name = systemId,
				};
				List<PathEntry> paths = PathCollection.Where(x => x.System == systemId).OrderBy(x => x.Ordinal).ThenBy(x => x.Type).ToList();

				int _x = 6;
				int _y = 14;
				int textbox_width = 70;
				int padding = 5;
				int button_width = 26;
				int widget_offset = 85;
				int row_height = 30;
				foreach (var path in paths)
				{
					TextBox box = new TextBox()
					{
						Text = path.Path,
						Location = new Point(_x, _y),
						Width = textbox_width,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						MinimumSize = new Size(26, 23),
						AutoCompleteMode = AutoCompleteMode.SuggestAppend,
						AutoCompleteCustomSource = AutoCompleteOptions,
						AutoCompleteSource = AutoCompleteSource.CustomSource,
					};

					Button btn = new Button()
					{
						Text = "",
						Image = BizHawk.MultiClient.Properties.Resources.OpenFile,
						Location = new Point(widget_offset, _y - 1),
						Width = button_width,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					TextBox tempBox = box;
					string tempPath = path.Type;
					string tempSystem = path.System;
					btn.Click += new System.EventHandler(delegate
					{
						BrowseFolder(tempBox, tempPath, tempSystem);
					});

					Label label = new Label()
					{
						Text = path.Type,
						Location = new Point(widget_offset + button_width + padding, _y + 4),
						Width = 100,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					_y += row_height;
				}

				string sys = systemDisplayName;
				if (systemDisplayName == "PCE") //Hack
				{
					sys = "PCECD";
				}

				bool hasFirmwares = FirmwaresConfig.SystemGroupNames.Any(x => x.Key == sys);

				if (hasFirmwares)
				{
					Button firmwareButton = new Button()
					{
						Name = sys,
						Text = "&Firmware",
						Location = new Point(_x, _y),
						Width = 75,
					};
					firmwareButton.Click += new System.EventHandler(delegate
					{
						FirmwaresConfig f = new FirmwaresConfig();
						f.TargetSystem = sys;
						f.ShowDialog();
					});

					t.Controls.Add(firmwareButton);
				}

				PathTabControl.TabPages.Add(t);
			}

			PathTabControl.ResumeLayout();
		}

		private void BrowseFolder(TextBox box, string _Name, string System)
		{
			//Ugly hack, we don't want to pass in the system in for system base and global paths
			if (_Name == "Base" || System == "Global")
			{
				System = null;
			}

			FolderBrowserEx f = new FolderBrowserEx
			{
				Description = "Set the directory for " + _Name,
				SelectedPath = PathManager.MakeAbsolutePath(box.Text, System)
			};
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				box.Text = PathManager.TryMakeRelative(f.SelectedPath, System);
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
			FolderBrowserEx f = new FolderBrowserEx
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
			DoROMToggle();
		}

		private void DoROMToggle()
		{
			List<Control> pcontrols = AllPathControls.Where(x => x.Name == "ROM").ToList();
			foreach (Control c in pcontrols)
			{
				c.Enabled = !RecentForROMs.Checked;
			}
		}

		private List<TextBox> AllPathBoxes
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

		private List<Label> AllPathLabels
		{
			get
			{
				List<Label> _AllPathLabels = new List<Label>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					IEnumerable<Label> control = from c in tp.Controls.OfType<Label>() select c;
					_AllPathLabels.AddRange(control);
				}
				return _AllPathLabels;
			}
		}

		private List<Button> AllPathButtons
		{
			get
			{
				List<Button> _AllPathButtons = new List<Button>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					IEnumerable<Button> control = from c in tp.Controls.OfType<Button>() select c;
					_AllPathButtons.AddRange(control);
				}
				return _AllPathButtons;
			}
		}

		private List<Control> AllPathControls
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

		private List<TabPage> AllTabPages
		{
			get
			{
				List<TabPage> _AllTabPages = new List<TabPage>();
				foreach (TabPage tp in PathTabControl.TabPages)
				{
					_AllTabPages.Add(tp);
				}
				return _AllTabPages;
			}
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			DoTabs(PathEntryCollection.DefaultValues);
		}
	}
}
