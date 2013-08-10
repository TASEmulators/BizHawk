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
			//Separate by system
			List<string> systems = Global.Config.PathEntries.Select(x => x.System).Distinct().ToList();
			systems.Sort();
			//TODO: put Global first

			//TODO: fix anchoring
			//TODO: fix logic of when to pass in the system (global and base do not want this)
			foreach(string tab in systems)
			{
				TabPage t = new TabPage()
				{
					Text = tab,
				};
				List<PathEntry> paths = Global.Config.PathEntries.Where(x => x.System == tab).OrderBy(x => x.Ordinal).ThenBy(x => x.Type).ToList();

				int _x = 6;
				int _y = 14;
				int textbox_width = 150;
				int padding = 10;
				int button_width = 26;
				foreach(var path in paths)
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
					btn.Click += new System.EventHandler(delegate {
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

		}
	}
}
