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
		//session file saving
		//track changes
		//new session
		//open session
		//recent session
		//save/save as session
		//options - autoload session
		//options - disable scripts on load
		//TODO: remember column widths
		//TODO: restore column width on restore default settings
		//TODO: load scripts from recent scripts menu
		//TODO: context menu & main menu - Edit is grayed out if seperator is highlighted
		//Free lua object when toggling a lua script off?
		//Fix up lua functions list display

		int defaultWidth;	//For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		List<LuaFiles> luaList = new List<LuaFiles>();
		public LuaImplementation LuaImp;
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
			if (column == 0)
			{
				if (luaList[index].IsSeparator)
					color = this.BackColor;
				else if (luaList[index].Enabled)
					color = Color.LightCyan;
			}
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
			LuaImp.Close();
			LuaImp = new LuaImplementation(this);
		}

		public void Restart()
		{
			StopAllScripts();
			LuaImp = new LuaImplementation(this);
		}

		private void SaveConfigSettings()
		{
			LuaImp.Close();
			Global.Config.LuaConsoleWndx = this.Location.X;
			Global.Config.LuaConsoleWndy = this.Location.Y;
			Global.Config.LuaConsoleWidth = this.Right - this.Left;
			Global.Config.LuaConsoleHeight = this.Bottom - this.Top;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;		//Save these first so that the user can restore to its original size
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

		private FileInfo GetFileFromUser(string filter)
		{
			var ofd = new OpenFileDialog();
			if (lastLuaFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(lastLuaFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
			ofd.Filter = filter;
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
			Global.Config.RecentLua.Add(path);            
			LuaImp.DoLuaFile(path);
		}

		private void OpenLuaFile()
		{
			var file = GetFileFromUser("Lua Scripts (*.lua)|*.lua|All Files|*.*");
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
					luaList[indexes[x]].Toggle();
				}
			}
			LuaListView.Refresh();
			UpdateNumberOfScripts();
			RunLuaScripts();
		}

		private void RunLuaScripts()
		{
			for (int x = 0; x < luaList.Count; x++)
			{
				if (luaList[x].Enabled)
				{
					LuaImp.DoLuaFile(luaList[x].Name);
				}
			}
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
			NewLuaSession(false);
		}

		private void NewLuaSession(bool suppressAsk)
		{
			//TODO: ask save
			StopAllScripts();
			luaList.Clear();
			DisplayLuaList();
			UpdateNumberOfScripts();

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

		private void toggleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Toggle();
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Cheats list
			//repopulate it with an up to date list
			recentToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentLua.IsEmpty())
			{
				var none = new ToolStripMenuItem();
				none.Enabled = false;
				none.Text = "None";
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentLua.Length(); x++)
				{
					string path = Global.Config.RecentLua.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadLuaFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentLua.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);
		}

		private void LoadLuaFromRecent(string path)
		{
			LoadLuaFile(path);
		}

		private void LuaConsole_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".lua"))
			{
				LoadLuaFile(filePaths[0]);
				DisplayLuaList();
				UpdateNumberOfScripts();
			}
		}

		private void LuaConsole_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
		}

		private void LuaListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveScript();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				SelectAll();
			}
		}

		private void editScriptToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditScript();
		}

		private void editToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditScript();
		}

		private void EditScript()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count == 0) 
				return;
			System.Diagnostics.Process.Start(luaList[indexes[0]].Path);
		}

		private void toggleScriptToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Toggle();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SelectAll();
		}

		private void SelectAll()
		{
			for (int x = 0; x < luaList.Count; x++)
			{
				LuaListView.SelectItem(x, true);
			}
		}

		private void toolStripButtonMoveDown_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void toolStripButtonMoveUp_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void toolStripButtonSeparator_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void copyToolStripButton_Click(object sender, EventArgs e)
		{
			Toggle();
		}

		private void EditToolstripButton_Click(object sender, EventArgs e)
		{
			EditScript();
		}

		private void cutToolStripButton_Click(object sender, EventArgs e)
		{
			RemoveScript();
		}

		public void WriteToOutputWindow(string message)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
				return;

			OutputBox.Text += message;
			OutputBox.Refresh();
		}

		public void ClearOutputWindow()
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
				return;
			
			OutputBox.Text = "";
			OutputBox.Refresh();
		}

		private void openToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			OpenLuaFile();
		}

		private void luaFunctionsListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show(LuaImp.LuaLibraryList);
		}

		private bool LoadLuaSession(string path)
		{
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			StopAllScripts();
			luaList = new List<LuaFiles>();

			using (StreamReader sr = file.OpenText())
			{
				bool enabled = false;
				string s = "";
				string temp = "";

				while ((s = sr.ReadLine()) != null)
				{
					//.luases 
					if (s.Length < 3) continue;

					temp = s.Substring(0, 1); //Get enabled flag

					try
					{
						if (int.Parse(temp) == 0)
							enabled = false;
						else
							enabled = true;
					}
					catch
					{
						return false; //TODO: report an error?
					}

					s = s.Substring(2, s.Length - 2); //Get path

					LuaFiles l = new LuaFiles(s);
					l.Enabled = enabled;
					luaList.Add(l);
				}
			}
			return true;
		}

		private void OpenLuaSession()
		{
			var file = GetFileFromUser("Lua Session Files (*.luases)|*.luases|All Files|*.*");
			if (file != null)
			{
				LoadLuaSession(file.FullName);
				RunLuaScripts();
				DisplayLuaList();
				UpdateNumberOfScripts();
			}
		}

		private void openSessionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaSession();
		}

		public bool IsRunning()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
			{
				return false;
			}
			else
			{
				if (LuaImp.isRunning)
					return true;
				else
					return false;
			}
		}

		public void WaitOne()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return;

			this.LuaImp.LuaWait.WaitOne();
		}

		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			OpenLuaFile();
		}
	}
}
