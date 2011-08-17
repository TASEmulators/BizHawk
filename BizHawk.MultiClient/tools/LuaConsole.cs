using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using LuaInterface;

namespace BizHawk.MultiClient
{
	public partial class LuaConsole : Form
	{
		//TODO: remember column widths
		//TODO: recent menu
		//TODO: drag & drop for .lua files


		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		List<LuaFiles> luaList = new List<LuaFiles>();
		LuaImplementation LuaImp;
		string lastLuaFile = "";

		private List<LuaFiles> GetLuaFileList()
		{
			List<LuaFiles> l = new List<LuaFiles>();
			for (int x = 0; x < luaList.Count; x++)
				l.Add(new LuaFiles(luaList[x]));

			return l;
		}

		public LuaConsole get()
		{
			return this;
		}

		public void AddText(string s)
		{
			OutputBox.Text += s;
		}

		public LuaConsole()
		{
			InitializeComponent();
			LuaImp = new LuaImplementation(this);
			Closing += (o, e) => SaveConfigSettings();
			LuaListView.QueryItemText += new QueryItemTextHandler(LuaListView_QueryItemText);
			LuaListView.QueryItemBkColor += new QueryItemBkColorHandler(LuaListView_QueryItemBkColor);
			LuaListView.VirtualMode = true;
		}

		private void LuaListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (luaList[index].IsSeparator)
				color = Color.DarkGray;
			else if (luaList[index].Enabled)
				color = Color.Cyan;
			else 
				color = this.BackColor;
		}

		private void LuaListView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
				text = Path.GetFileNameWithoutExtension(luaList[index].Path); //TODO: how about a list of Names and allow the user to name them?
			if (column == 1)
				text = luaList[index].Path;

		}

		private void LuaConsole_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
		}

		private void StopAllScripts()
		{
			for (int x = 0; x < luaList.Count; x++)
				luaList[x].Enabled = false;
		}

		public void Restart()
		{
			StopAllScripts();
		}

		private void SaveConfigSettings()
		{
			Global.Config.LuaConsoleWndx = this.Location.X;
			Global.Config.LuaConsoleWndy = this.Location.Y;
			Global.Config.LuaConsoleWidth = this.Right - this.Left;
			Global.Config.LuaConsoleHeight = this.Bottom - this.Top;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.LuaConsoleSaveWindowPosition && Global.Config.LuaConsoleWndx >= 0 && Global.Config.LuaConsoleWndy >= 0)
				Location = new Point(Global.Config.LuaConsoleWndx, Global.Config.LuaConsoleWndy);

			if (Global.Config.LuaConsoleWidth >= 0 && Global.Config.LuaConsoleHeight >= 0)
			{
				Size = new System.Drawing.Size(Global.Config.LuaConsoleWidth, Global.Config.LuaConsoleHeight);
			}

		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
		}

		private FileInfo GetFileFromUser()
		{
			var ofd = new OpenFileDialog();
			if (lastLuaFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(lastLuaFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
			ofd.Filter = "Lua Scripts (*.lua)|*.lua|All Files|*.*";
			ofd.RestoreDirectory = true;
			
			
			if (!Directory.Exists(ofd.InitialDirectory))
				Directory.CreateDirectory(ofd.InitialDirectory);

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		private void LoadLuaFile(string path)
		{
			LuaFiles l = new LuaFiles("", path, true);
			luaList.Add(l);
			LuaListView.ItemCount = luaList.Count;
			LuaListView.Refresh();

			LuaImp.DoLuaFile(path);
		}

		private void OpenLuaFile()
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				LoadLuaFile(file.FullName);
				DisplayLuaList();
				UpdateNumberOfScripts();
			}
		}

		public void DisplayLuaList()
		{
			LuaListView.ItemCount = luaList.Count;
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaFile();
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
			autoloadConsoleToolStripMenuItem.Checked = Global.Config.AutoLoadLuaConsole;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.LuaConsoleSaveWindowPosition ^= true;
		}

		private void Toggle()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int x = 0; x < indexes.Count; x++)
				{
					if (luaList[indexes[x]].Enabled)
						luaList[indexes[x]].Enabled = false;
					else
						luaList[indexes[x]].Enabled = true;
				}
				LuaListView.Refresh();
			}
			UpdateNumberOfScripts();
		}

		private void UpdateNumberOfScripts()
		{
			string message = "";
			int active = 0;
			for (int x = 0; x < luaList.Count; x++)
			{
				if (luaList[x].Enabled)
					active++;
			}

			int L = luaList.Count;
			if (L == 1)
				message += L.ToString() + " script (" + active.ToString() + " active)";
			else if (L == 0)
				message += L.ToString() + " script";
			else
				message += L.ToString() + " scripts (" + active.ToString() + " active)";

			NumberOfScripts.Text = message;
		}

		private void LuaListView_DoubleClick(object sender, EventArgs e)
		{
			Toggle();
		}

		private void LuaListView_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void turnOffAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopAllScripts();
		}

		private void stopAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopAllScripts();
		}

		private void autoloadConsoleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadLuaConsole ^= true;
		}

		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveScript();
		}

		private void RemoveScript()
		{
			if (luaList.Count == 0) return;
			//Changes();
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				foreach (int index in indexes)
				{
					luaList.Remove(luaList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
				}
				indexes.Clear();
				DisplayLuaList();
			}
		}

		private void removeScriptToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveScript();
		}

		private void insertSeperatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void InsertSeparator()
		{
			LuaFiles f = new LuaFiles(true);
			f.IsSeparator = true;

			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			int x;
			if (indexes.Count > 0)
			{
				x = indexes[0];
				if (indexes[0] > 0)
					luaList.Insert(indexes[0], f);
			}
			else
				luaList.Add(f);
			DisplayLuaList();
			LuaListView.Refresh();
		}

		private void insertSeperatorToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void MoveUp()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			LuaFiles temp = new LuaFiles(false);
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				temp = luaList[index];
				luaList.Remove(luaList[index]);
				luaList.Insert(index - 1, temp);

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				//Changes();
			}
			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
				i.Add(indexes[z] - 1);

			LuaListView.SelectedIndices.Clear();
			for (int z = 0; z < i.Count; z++)
				LuaListView.SelectItem(i[z], true);


			DisplayLuaList();
		}

		private void MoveDown()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			LuaFiles temp = new LuaFiles(false);
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				temp = luaList[index];

				if (index < luaList.Count - 1)
				{

					luaList.Remove(luaList[index]);
					luaList.Insert(index + 1, temp);

				}

				//Note: here it will get flagged many times redundantly potnetially, 
				//but this avoids it being flagged falsely when the user did not select an index
				//Changes();
			}

			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
				i.Add(indexes[z] + 1);

			LuaListView.SelectedIndices.Clear();
			//for (int z = 0; z < i.Count; z++)
			//CheatListView.SelectItem(i[z], true); //TODO

			DisplayLuaList();
		}
	}
}
