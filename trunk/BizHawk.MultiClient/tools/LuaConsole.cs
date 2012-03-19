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
		//options - autoload session
		//options - disable scripts on load
		//TODO: remember column widths
		//TODO: restore column width on restore default settings

		int defaultWidth;	//For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		string currentSessionFile = "";
		List<LuaFiles> luaList = new List<LuaFiles>();
		public LuaImplementation LuaImp;
		string lastLuaFile = "";
		bool changes = false;

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

		private void StopScript(int x)
		{
			luaList[x].Enabled = false;
			LuaImp.Close();
			LuaImp = new LuaImplementation(this);
			changes = true;
		}

		private void StopAllScripts()
		{
			for (int x = 0; x < luaList.Count; x++)
				luaList[x].Enabled = false;
			LuaImp.Close();
			LuaImp = new LuaImplementation(this);
			changes = true;
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
			changes = true;
		}

		private void OpenLuaFile()
		{
			var file = GetFileFromUser("Lua Scripts (*.lua)|*.lua|Text (*.text)|*.txt|All Files|*.*");
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
			changes = true;
		}

		private void RunLuaScripts()
		{
			for (int x = 0; x < luaList.Count; x++)
			{
				if (luaList[x].Enabled)
				{
					LuaImp.DoLuaFile(luaList[x].Path);
				}
				else
				{
					StopScript(x);
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
			MessageBox.Show("");
			//Toggle();
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
			if (string.Compare(currentSessionFile, "") == 0) return;

			if (changes)
			{
				SaveSession(currentSessionFile);
				AddText('\n' + Path.GetFileName(currentSessionFile) + " saved.");
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewLuaSession(false);
		}

		private void NewLuaSession(bool suppressAsk)
		{
			bool result = true;
			if (changes) result = AskSave();

			if (result == true || suppressAsk)
			{
				StopAllScripts();
				ClearOutput();
				luaList.Clear();
				DisplayLuaList();
				UpdateNumberOfScripts();
				changes = false;
			}
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
			changes = true;
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
			changes = true;
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
			else if (Path.GetExtension(filePaths[0]) == (".luases"))
			{
				LoadLuaSession(filePaths[0]);
				DisplayLuaList();
				UpdateNumberOfScripts();
				ClearOutput();
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

			OutputBox.Invoke(() =>
			{
				OutputBox.Text += message;
				OutputBox.Refresh();
			});
		}

		public void ClearOutputWindow()
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
				return;

			OutputBox.Invoke(() =>
			{
				OutputBox.Text = "";
				OutputBox.Refresh();
			});
		}

		private void openToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			OpenLuaFile();
		}

		private void luaFunctionsListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Sound.StopSound();
			new LuaFunctionList().ShowDialog();
			Global.Sound.StartSound();
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
			Global.Config.RecentLuaSession.Add(path);
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
				ClearOutput();
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

		public bool WaitOne(int timeout)
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return true;

			return this.LuaImp.LuaWait.WaitOne(timeout);
		}

		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			OpenLuaFile();
		}

		private void LuaListView_ItemActivate(object sender, EventArgs e)
		{
			Toggle();
		}

		private void clearToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			ClearOutput();
		}

		private void ClearOutput()
		{
			OutputBox.Text = "";
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (currentSessionFile.Length > 0)
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentSessionFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentSessionFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
				
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
			}
			sfd.Filter = "Lua Session Files (*.luases)|*.luases|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			return file;
		}

		private void SaveAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				SaveSession(file.FullName);
				currentSessionFile = file.FullName;
				AddText('\n' + Path.GetFileName(currentSessionFile) + " saved.");
				Global.Config.RecentLuaSession.Add(file.FullName);
				changes = false;
			}
		}

		private bool SaveSession(string path)
		{
			var file = new FileInfo(path);

			using (StreamWriter sw = new StreamWriter(path))
			{
				string str = "";
				for (int i = 0; i < luaList.Count; i++)
				{
					if (!luaList[i].IsSeparator)
					{
						if (luaList[i].Enabled)
							str += "1 ";
						else
							str += "0 ";

						str += luaList[i].Path;
					}
				}
				sw.WriteLine(str);
			}

			changes = false;
			return true;
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (string.Compare(currentSessionFile, "") == 0 || !changes)
			{
				saveToolStripMenuItem.Enabled = false;
			}
			else
			{
				saveToolStripMenuItem.Enabled = true;
			}
		}

		private void recentSessionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Cheats list
			//repopulate it with an up to date list
			recentSessionsToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentLuaSession.IsEmpty())
			{
				var none = new ToolStripMenuItem();
				none.Enabled = false;
				none.Text = "None";
				recentSessionsToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentLuaSession.Length(); x++)
				{
					string path = Global.Config.RecentLuaSession.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadSessionFromRecent(path);
					recentSessionsToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentSessionsToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentLuaSession.Clear();
			recentSessionsToolStripMenuItem.DropDownItems.Add(clearitem);
		}

		public void LoadSessionFromRecent(string file)
		{
			bool z = true;
			if (changes) z = AskSave();

			if (z)
			{
				bool r = LoadLuaSession(file);
				if (!r)
				{
					DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
					if (result == DialogResult.Yes)
						Global.Config.RecentLuaSession.Remove(file);
				}
				RunLuaScripts();
				DisplayLuaList();
				UpdateNumberOfScripts();
				ClearOutput();
				changes = false;
			}
		}

		public bool AskSave()
		{
			if (changes)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save changes to session?", "Lua Console", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				Global.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					if (string.Compare(currentSessionFile, "") == 0)
					{
						SaveAs();
					}
					else
						SaveSession(currentSessionFile);
					return true;
				}
				else if (result == DialogResult.No)
					return true;
				else if (result == DialogResult.Cancel)
					return false;
			}
			return true;
		}

		private void moveUpToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveDownToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void scriptToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			bool luaRunning = false;
			for (int i = 0; i < luaList.Count; i++)
			{
				if (luaList[i].Enabled)
					luaRunning = true;
			}

			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				scriptToolStripMenuItem.DropDownItems[1].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[3].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[6].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[7].Enabled = true;

				bool allSeparators = true;
				for (int i = 0; i < indexes.Count; i++)
				{
					if (!luaList[indexes[i]].IsSeparator)
						allSeparators = false;
				}
				if (allSeparators)
					scriptToolStripMenuItem.DropDownItems[2].Enabled = false;
				else
					scriptToolStripMenuItem.DropDownItems[2].Enabled = true;
			}
			else
			{
				scriptToolStripMenuItem.DropDownItems[1].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[2].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[3].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[6].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[7].Enabled = false;
			}

			if (luaList.Count > 0)
				scriptToolStripMenuItem.DropDownItems[8].Enabled = true;
			else
				scriptToolStripMenuItem.DropDownItems[8].Enabled = false;

			if (luaRunning)
				scriptToolStripMenuItem.DropDownItems[10].Enabled = true;
			else
				scriptToolStripMenuItem.DropDownItems[10].Enabled = false;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			bool luaRunning = false;
			for (int i = 0; i < luaList.Count; i++)
			{
				if (luaList[i].Enabled)
					luaRunning = true;
			}

			if (indexes.Count > 0)
			{
				contextMenuStrip1.Items[0].Enabled = true;
				contextMenuStrip1.Items[1].Enabled = true;
				contextMenuStrip1.Items[2].Enabled = true;

				bool allSeparators = true;
				for (int i = 0; i < indexes.Count; i++)
				{
					if (!luaList[indexes[i]].IsSeparator)
						allSeparators = false;
				}
				if (allSeparators)
					contextMenuStrip1.Items[1].Enabled = false;
				else
					contextMenuStrip1.Items[1].Enabled = true;
			}
			else
			{
				contextMenuStrip1.Items[0].Enabled = false;
				contextMenuStrip1.Items[1].Enabled = false;
				contextMenuStrip1.Items[2].Enabled = false;
			}

			if (luaRunning)
			{
				contextMenuStrip1.Items[4].Visible = true;
				contextMenuStrip1.Items[5].Visible = true;
			}
			else
			{
				contextMenuStrip1.Items[4].Visible = false;
				contextMenuStrip1.Items[5].Visible = false;
			}
		}
	}
}
