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
		//TODO:
		//FInish config entries
		//Set tab focus based on system ID
		//Remaining GUI items
		public NewPathConfig()
		{
			InitializeComponent();
		}

		private void NewPathConfig_Load(object sender, EventArgs e)
		{
			LoadSettings();
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
			BasePathBox.Text = Global.Config.BasePath;
			DoTabs();
			SetDefaultFocusedTab();
		}

		private void SetDefaultFocusedTab()
		{
			switch (Global.Game.System)
			{
				case "NULL":
					PathTabControl.SelectTab(FindTabByName("Global"));
					break;
				default:
					PathTabControl.SelectTab(FindTabByName(Global.Game.System));
					break;

				//"Sub" Systems and other exceptions go here
				case "PCECD":
				case "SGX":
					PathTabControl.SelectTab(FindTabByName("PCE"));
					break;
				case "GBC":
					PathTabControl.SelectTab(FindTabByName("GB"));
					break;
				case "SGB":
					PathTabControl.SelectTab(FindTabByName("SNES"));
					break;
			}
		}

		private TabPage FindTabByName(string name)
		{
			IEnumerable<TabPage> query = from p in PathTabControl.TabPages.OfType<TabPage>() select p;
			var tab = query.FirstOrDefault(x => x.Name.ToUpper() == name.ToUpper());
			if (tab == null)
			{
				return new TabPage();
			}
			else
			{
				return tab;
			}
		}

		private void DoTabs()
		{
			//Separate by system
			List<string> systems = Global.Config.PathEntries.Select(x => x.System).Distinct().ToList();
			systems.Sort();
			//TODO: put Global first

			//TODO: fix anchoring
			//TODO: fix logic of when to pass in the system (global and base do not want this)
			foreach (string tab in systems)
			{
				TabPage t = new TabPage()
				{
					Text = tab,
					Name = tab,
				};
				List<PathEntry> paths = Global.Config.PathEntries.Where(x => x.System == tab).OrderBy(x => x.Ordinal).ThenBy(x => x.Type).ToList();

				int _x = 6;
				int _y = 14;
				int textbox_width = 150;
				int padding = 10;
				int button_width = 26;
				foreach (var path in paths)
				{

					TextBox box = new TextBox()
					{
						Text = path.Path,
						Location = new Point(_x, _y),
						Width = textbox_width,
						//Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
					};

					Button btn = new Button()
					{
						Text = "",
						Image = BizHawk.MultiClient.Properties.Resources.OpenFile,
						Location = new Point(_x + textbox_width + padding, _y - 1),
						Width = button_width,
						//Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};
					btn.Click += new System.EventHandler(delegate
					{
						BrowseFolder(box, path.Type, path.System);
					});

					Label label = new Label()
					{
						Text = path.Type,
						Location = new Point(_x + textbox_width + (padding * 2) + button_width, _y + 4),
						//Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					t.Controls.Add(box);
					t.Controls.Add(btn);
					t.Controls.Add(label);

					_y += 30;
				}

				PathTabControl.TabPages.Add(t);
			}
		}

		private void BrowseFolder(TextBox box, string _Name, string System)
		{
			FolderBrowserEx f = new FolderBrowserEx
			{
				Description = "Set the directory for " + _Name,
				SelectedPath = PathManager.MakeAbsolutePath(box.Text, System)
			};
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				box.Text = f.SelectedPath;
			}
		}

		private void SaveSettings()
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;
			Global.Config.BasePath = BasePathBox.Text;
			//TODO
		}

		private void BrowseBase_Click(object sender, EventArgs e)
		{
			FolderBrowserEx f = new FolderBrowserEx
			{
				Description = "Set the directory for the base global path",
				SelectedPath = PathManager.MakeAbsolutePath(BasePathBox.Text)
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
			//TODO
		}
	}
}
