using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewCheatForm : Form
	{
		private NewCheatList Cheats = new NewCheatList();

		public NewCheatForm()
		{
			InitializeComponent();
		}

		private void UpdateListView()
		{
			CheatListView.ItemCount = Global.CheatList2.Count;
			TotalLabel.Text = Global.CheatList2.CheatCount.ToString()
				+ (Global.CheatList2.CheatCount == 1 ? " cheat" : " cheats")
				+ Global.CheatList2.ActiveCheatCount.ToString() + " active";
		}

		public void LoadFileFromRecent(string path)
		{
			bool ask_result = true;
			if (Global.CheatList2.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				bool load_result = Global.CheatList2.Load(path, append: false);
				if (!load_result)
				{
					Global.Config.RecentWatches.HandleLoadError(path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					UpdateListView();
					MessageLabel.Text = Path.GetFileName(path) + " loaded";
				}
			}
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (Global.CheatList2.Changes)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save Changes?", "Cheats", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				Global.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					Global.CheatList2.Save();
				}
				else if (result == DialogResult.No)
				{
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private void LoadFile(FileInfo file, bool append)
		{
			if (file != null)
			{
				bool result = true;
				if (Cheats.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					Cheats.Load(file.FullName, append);
					UpdateListView();
					Global.Config.RecentCheats.Add(Cheats.CurrentFileName);
				}
			}
		}

		private void NewCheatForm_Load(object sender, EventArgs e)
		{

		}

		#region Events

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = Global.CheatList2.Changes;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Global.Config.RecentCheats.GenerateRecentMenu(LoadFileFromRecent));
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			bool append = sender == AppendMenuItem;
			LoadFile(NewCheatList.GetFileFromUser(Cheats.CurrentFileName), append);
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Cheats

		private void CheatsSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		#endregion

		#region Columns

		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		#endregion

		#endregion
	}
}
