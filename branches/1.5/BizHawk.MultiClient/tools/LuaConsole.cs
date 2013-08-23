using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class LuaConsole : Form
	{
		//TODO: remember column widths
		//TODO: restore column width on restore default settings

		int defaultWidth;	//For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		string currentSessionFile = "";
		List<LuaFile> luaList = new List<LuaFile>();
		public LuaImplementation LuaImp;
		string lastLuaFile = ""; //TODO: this isn't getting used!
		bool changes = false;

		private List<LuaFile> GetLuaFileList()
		{
			List<LuaFile> l = new List<LuaFile>();
			for (int x = 0; x < luaList.Count; x++)
				l.Add(new LuaFile(luaList[x]));

			return l;
		}

		public LuaConsole get()
		{
			return this;
		}

		public void AddText(string s)
		{
			OutputBox.Text += s + "\n\n";
		}

		public LuaConsole()
		{
			InitializeComponent();
			LuaImp = new LuaImplementation(this);
			Closing += (o, e) => SaveConfigSettings();
			LuaListView.QueryItemText += LuaListView_QueryItemText;
			LuaListView.QueryItemBkColor += LuaListView_QueryItemBkColor;
			LuaListView.VirtualMode = true;
		}

		private void Changes(bool changesOccured)
		{
			if (changesOccured)
			{
				changes = true;
				OutputMessages.Text = "* " + Path.GetFileName(currentSessionFile);
			}
			else
			{
				changes = false;
				OutputMessages.Text = Path.GetFileName(currentSessionFile);
			}
		}

		private void LuaListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (column == 0)
			{
				if (luaList[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (luaList[index].Enabled && !luaList[index].Paused)
				{
					color = Color.LightCyan;
				}
				else if (luaList[index].Enabled && luaList[index].Paused)
				{
					color = Color.IndianRed;
				}
			}

			UpdateNumberOfScripts();
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
			if (Global.Config.AutoLoadLuaSession)
			{
				if (!Global.Config.RecentLuaSession.Empty)
				{
					LoadSessionFromRecent(Global.Config.RecentLuaSession.GetRecentFileByPosition(0));
				}
			}

			newStripButton1.Visible = MainForm.INTERIM;
			newScriptToolStripMenuItem.Visible = MainForm.INTERIM;
			newStripButton1.Enabled = MainForm.INTERIM;
			newScriptToolStripMenuItem.Enabled = MainForm.INTERIM;
		}

		private void StopScript(int x)
		{
			luaList[x].Stop();
			Changes(true);
		}

		private void StopAllScripts()
		{
			foreach (LuaFile t in luaList)
			{
				t.Enabled = false;
			}
			Changes(true);
		}

		public void Restart()
		{
			StopAllScripts();
		}

		private void SaveConfigSettings()
		{
			LuaImp.Close();
			Global.Config.LuaConsoleWndx = Location.X;
			Global.Config.LuaConsoleWndy = Location.Y;
			Global.Config.LuaConsoleWidth = Right - Left;
			Global.Config.LuaConsoleHeight = Bottom - Top;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;		//Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.LuaConsoleSaveWindowPosition && Global.Config.LuaConsoleWndx >= 0 && Global.Config.LuaConsoleWndy >= 0)
				Location = new Point(Global.Config.LuaConsoleWndx, Global.Config.LuaConsoleWndy);

			if (Global.Config.LuaConsoleWidth >= 0 && Global.Config.LuaConsoleHeight >= 0)
			{
				Size = new Size(Global.Config.LuaConsoleWidth, Global.Config.LuaConsoleHeight);
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
		}

		private FileInfo GetFileFromUser(string filter)
		{
			var ofd = new OpenFileDialog();
			if (lastLuaFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(lastLuaFile);
			ofd.InitialDirectory = PathManager.GetLuaPath();
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

		public void LoadLuaFile(string path)
		{
			if (LuaAlreadyInSession(path) == false)
			{
				LuaFile l = new LuaFile("", path);
				luaList.Add(l);
				LuaListView.ItemCount = luaList.Count;
				LuaListView.Refresh();
				Global.Config.RecentLua.Add(path);

				if (!Global.Config.DisableLuaScriptsOnLoad)
				{
					try
					{
						l.Thread = LuaImp.SpawnCoroutine(path);
						l.Enabled = true;
					}
					catch (Exception e)
					{
						if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							l.Enabled = false;
							AddText(e.Message);
						}
						else MessageBox.Show(e.ToString());
					}
				}
				else l.Enabled = false;
				l.Paused = false;
				Changes(true);
			}
			else
			{
				foreach (LuaFile t in luaList)
				{
					if (path == t.Path && t.Enabled == false && !Global.Config.DisableLuaScriptsOnLoad)
					{
						t.Toggle();
						RunLuaScripts();
						LuaListView.Refresh();
						Changes(true);
						break;
					}
				}
			}
		}

		private void OpenLuaFile()
		{
			var file = GetFileFromUser("Lua Scripts (*.lua)|*.lua|Text (*.text)|*.txt|All Files|*.*");
			if (file != null)
			{
				LoadLuaFile(file.FullName);
				DisplayLuaList();
			}
		}

		public void DisplayLuaList()
		{
			LuaListView.ItemCount = luaList.Count;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
			autoloadConsoleToolStripMenuItem.Checked = Global.Config.AutoLoadLuaConsole;
			autoloadSessionToolStripMenuItem.Checked = Global.Config.AutoLoadLuaSession;
			disableScriptsOnLoadToolStripMenuItem.Checked = Global.Config.DisableLuaScriptsOnLoad;
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
					var item = luaList[indexes[x]];
					if (!item.IsSeparator)
					{
						item.Toggle();
					}
					if (item.Enabled && item.Thread == null)
						try
						{
							item.Thread = LuaImp.SpawnCoroutine(item.Path);
						}
						catch (Exception e)
						{
							if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
							{
								item.Enabled = false;
								AddText(e.Message);
							}
							else MessageBox.Show(e.ToString());
						}
					else if (!item.Enabled && item.Thread != null)
						item.Stop();
				}
			}
			LuaListView.Refresh();
			Changes(true);
		}

		public void RunLuaScripts()
		{
			for (int x = 0; x < luaList.Count; x++)
			{
				if (luaList[x].Enabled && luaList[x].Thread == null)
				{
					try
					{
						luaList[x].Thread = LuaImp.SpawnCoroutine(luaList[x].Path);
					}
					catch (Exception e)
					{
						if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							luaList[x].Enabled = false;
							AddText(e.Message);
						}
						else MessageBox.Show(e.ToString());
					}
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
			int active = 0, paused = 0, separators = 0;
			foreach (LuaFile t in luaList)
			{
				if (!t.IsSeparator)
				{
					if (t.Enabled)
					{
						active++;
						if (t.Paused)
							paused++;
					}
				}
				else
				{
					separators++;
				}
			}

			int L = luaList.Count - separators;
			if (L == 1)
				message += L.ToString() + " script (" + active.ToString() + " active, " + paused.ToString() + " paused)";
			else if (L == 0)
				message += L.ToString() + " script";
			else
				message += L.ToString() + " scripts (" + active.ToString() + " active, " + paused.ToString() + " paused)";

			NumberOfScripts.Text = message;
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (changes)
			{
				if (string.Compare(currentSessionFile, "") == 0)
					SaveAs();
				else SaveSession(currentSessionFile);
				Changes(false);
				OutputMessages.Text = Path.GetFileName(currentSessionFile) + " saved.";
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

			if (result || suppressAsk)
			{
				ClearOutput();
				StopAllScripts();
				luaList.Clear();
				DisplayLuaList();
				currentSessionFile = "";
				Changes(false);
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

		private void RemoveScript()
		{
			if (luaList.Count == 0) return;
			Changes(true);
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				foreach (int index in indexes)
				{
					luaList.Remove(luaList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
				}
				indexes.Clear();
				DisplayLuaList();
				UpdateNumberOfScripts();
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
			LuaFile f = new LuaFile(true) {IsSeparator = true};

			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				if (indexes[0] > 0)
				{
					luaList.Insert(indexes[0], f);
				}
			}
			else
			{
				luaList.Add(f);
			}
			DisplayLuaList();
			LuaListView.Refresh();
			Changes(true);
		}

		private void insertSeperatorToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void MoveUp()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes[0] == 0)
				return;

			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				LuaFile temp = luaList[index];
				luaList.Remove(luaList[index]);
				luaList.Insert(index - 1, temp);

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes(true);
			}
			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
				i.Add(indexes[z] - 1);

			LuaListView.SelectedIndices.Clear();
			foreach (int t in i)
			{
				LuaListView.SelectItem(t, true);
			}

			DisplayLuaList();
		}

		private void MoveDown()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				LuaFile temp = luaList[index];

				if (index < luaList.Count - 1)
				{

					luaList.Remove(luaList[index]);
					luaList.Insert(index + 1, temp);

				}

				//Note: here it will get flagged many times redundantly potnetially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes(true);
			}

			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
				i.Add(indexes[z] + 1);

			LuaListView.SelectedIndices.Clear();
			foreach (int t in i)
			{
				LuaListView.SelectItem(t, true);
			}

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

			if (Global.Config.RecentLua.Empty)
			{
				var none = new ToolStripMenuItem {Enabled = false, Text = "None"};
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentLua.Count; x++)
				{
					string path = Global.Config.RecentLua.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem {Text = path};
					item.Click += (o, ev) => LoadLuaFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem {Text = "&Clear"};
			clearitem.Click += (o, ev) => Global.Config.RecentLua.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);
		}

		private void LoadLuaFromRecent(string path)
		{
			LoadLuaFile(path);
		}

		private bool LuaAlreadyInSession(string path)
		{
			return luaList.Any(t => path == t.Path);
		}

		private void LuaConsole_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			try
			{
				foreach (string path in filePaths)
				{
					if (Path.GetExtension(path) == (".lua") || Path.GetExtension(path) == (".txt"))
					{
						LoadLuaFile(path);
						DisplayLuaList();
					}
					else if (Path.GetExtension(path) == (".luases"))
					{
						LoadLuaSession(path);
						RunLuaScripts();
						return;
					}
				}
			}
			catch (Exception ex)
			{
				if (ex.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:" || ex.ToString().Substring(0, 26) == "LuaInterface.LuaException:")
				{
					AddText(ex.Message);
				}
				else MessageBox.Show(ex.Message);
			}
		}

		private void LuaConsole_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
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

			if (indexes.Count > 0)
			{
				for (int x = 0; x < indexes.Count; x++)
				{
					var item = luaList[indexes[x]];
					if (!item.IsSeparator)
						System.Diagnostics.Process.Start(luaList[indexes[x]].Path);
				}
			}
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
			if (MainForm.INTERIM)
			{
				DoLuaWriter();
			}
			else
			{
				EditScript();
			}
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
				OutputBox.Text += message + "\n\n";
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
			new LuaFunctionList().Show();
			Global.Sound.StartSound();
		}

		public bool LoadLuaSession(string path)
		{
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			ClearOutput();
			StopAllScripts();
			luaList = new List<LuaFile>();

			using (StreamReader sr = file.OpenText())
			{
				string s;

				while ((s = sr.ReadLine()) != null)
				{
					//.luases 
					if (s.Length < 3) continue;
					LuaFile l;
					if (s.Substring(0, 3) == "---")
					{
						l = new LuaFile(true) {IsSeparator = true};
					}
					else
					{
						string temp = s.Substring(0, 1);

						bool enabled;
						try
						{
							if (int.Parse(temp) == 0)
							{
								enabled = false;
							}
							else
							{
								enabled = true;
							}
						}
						catch
						{
							return false; //TODO: report an error?
						}

						s = s.Substring(2, s.Length - 2); //Get path

						l = new LuaFile(s);

						if (!Global.Config.DisableLuaScriptsOnLoad)
							l.Enabled = enabled;
						else
							l.Enabled = false;
					}
					luaList.Add(l);
				}
			}
			Global.Config.RecentLuaSession.Add(path);
			currentSessionFile = path;
			Changes(false);
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
			}
		}

		private void openSessionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaSession();
		}

		/// <summary>
		/// resumes suspended coroutines
		/// </summary>
		/// <param name="includeFrameWaiters">should frame waiters be waken up? only use this immediately before a frame of emulation</param>
		public void ResumeScripts(bool includeFrameWaiters)
		{
			if (luaList != null && luaList.Count > 0)
			{
				if (LuaImp.luaSurface == null)
				{
					LuaImp.gui_drawNewEmu();
				}
				foreach (var lf in luaList)
				{
					//save old current directory before this lua thread clobbers it for the .net thread
					string oldcd = Environment.CurrentDirectory;

					try
					{
						//LuaImp.gui_clearGraphics();
						if (lf.Enabled && lf.Thread != null && !(lf.Paused))
						{
							bool prohibit = lf.FrameWaiting && !includeFrameWaiters;
							if (!prohibit)
							{
								//restore this lua thread's preferred current directory
								if (lf.CurrentDirectory != null)
								{
									Environment.CurrentDirectory = lf.CurrentDirectory;
								}
								var result = LuaImp.ResumeScript(lf.Thread);
								if (result.Terminated) lf.Stop();
								lf.FrameWaiting = result.WaitForFrame;

								//if the lua thread changed its current directory, capture that here
								lf.CurrentDirectory = Environment.CurrentDirectory;
							}
						}
					}
					catch (Exception ex)
					{
						if (ex is LuaInterface.LuaScriptException || ex is LuaInterface.LuaException)
						{
							lf.Enabled = false;
							lf.Thread = null;
							AddText(ex.ToString());
						}
						else MessageBox.Show(ex.ToString());
					}

					//restore the current directory
					Environment.CurrentDirectory = oldcd;
				}
			}
			//LuaImp.gui_drawFinishEmu();
		}

		public void StartLuaDrawing()
		{
			if (luaList != null && luaList.Count > 0)
			{
				if (LuaImp.luaSurface == null)
					LuaImp.gui_drawNewEmu();
			}
		}

		public void EndLuaDrawing()
		{
			if (luaList != null && luaList.Count > 0)
			{
				LuaImp.gui_drawFinishEmu();
			}
		}

		public bool IsRunning()
		{
			return true;
		}

		public bool WaitOne(int timeout)
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return true;
			}

			return LuaImp.LuaWait.WaitOne(timeout);
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

		public void ClearOutput()
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
				sfd.InitialDirectory = PathManager.GetLuaPath();

			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.GetLuaPath();
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
				OutputMessages.Text = Path.GetFileName(currentSessionFile) + " saved.";
				Global.Config.RecentLuaSession.Add(file.FullName);
				Changes(false);
			}
		}

		private void SaveSession(string path)
		{
			using (StreamWriter sw = new StreamWriter(path))
			{
				string str = "";
				foreach (LuaFile t in luaList)
				{
					if (!t.IsSeparator)
					{
						if (t.Enabled)
						{
							str += "1 ";
						}
						else
						{
							str += "0 ";
						}

						str += t.Path + "\n";
					}
					else
					{
						str += "---\n";
					}
				}
				sw.Write(str);
			}

			Changes(false);
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (!changes)
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

			if (Global.Config.RecentLuaSession.Empty)
			{
				var none = new ToolStripMenuItem {Enabled = false, Text = "None"};
				recentSessionsToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentLuaSession.Count; x++)
				{
					string path = Global.Config.RecentLuaSession.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem {Text = path};
					item.Click += (o, ev) => LoadSessionFromRecent(path);
					recentSessionsToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentSessionsToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem {Text = "&Clear"};
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
				//ClearOutput();
				LuaListView.Refresh();
				currentSessionFile = file;
				Changes(false);
			}
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

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
			foreach (LuaFile t in luaList)
			{
				if (t.Enabled)
				{
					luaRunning = true;
				}
			}

			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				scriptToolStripMenuItem.DropDownItems[1].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[2].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[3].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[4].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[7].Enabled = true;
				scriptToolStripMenuItem.DropDownItems[8].Enabled = true;

				bool allSeparators = true;
				for (int i = 0; i < indexes.Count; i++)
				{
					if (!luaList[indexes[i]].IsSeparator)
						allSeparators = false;
				}
				if (allSeparators)
					scriptToolStripMenuItem.DropDownItems[3].Enabled = false;
				else
					scriptToolStripMenuItem.DropDownItems[3].Enabled = true;
			}
			else
			{
				scriptToolStripMenuItem.DropDownItems[1].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[2].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[3].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[4].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[7].Enabled = false;
				scriptToolStripMenuItem.DropDownItems[8].Enabled = false;
			}

			if (luaList.Any())
				scriptToolStripMenuItem.DropDownItems[9].Enabled = true;
			else
				scriptToolStripMenuItem.DropDownItems[9].Enabled = false;


			turnOffAllScriptsToolStripMenuItem.Enabled = luaRunning;


			showRegisteredFunctionsToolStripMenuItem.Enabled = Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions.Any();
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			bool luaRunning = false;
			foreach (LuaFile t in luaList)
			{
				if (t.Enabled)
					luaRunning = true;
			}

			if (indexes.Count > 0)
			{
				contextMenuStrip1.Items[0].Enabled = true;
				contextMenuStrip1.Items[1].Enabled = true;
				contextMenuStrip1.Items[2].Enabled = true;
				contextMenuStrip1.Items[3].Enabled = true;

				bool allSeparators = true;
				for (int i = 0; i < indexes.Count; i++)
				{
					if (!luaList[indexes[i]].IsSeparator)
						allSeparators = false;
				}
				if (allSeparators)
					contextMenuStrip1.Items[2].Enabled = false;
				else
					contextMenuStrip1.Items[2].Enabled = true;
			}
			else
			{
				contextMenuStrip1.Items[0].Enabled = false;
				contextMenuStrip1.Items[1].Enabled = false;
				contextMenuStrip1.Items[2].Enabled = false;
				contextMenuStrip1.Items[3].Enabled = true;
			}

			if (luaRunning)
			{
				contextMenuStrip1.Items[5].Visible = true;
				contextMenuStrip1.Items[6].Visible = true;
			}
			else
			{
				contextMenuStrip1.Items[5].Visible = false;
				contextMenuStrip1.Items[6].Visible = false;
			}
		}

		private void disableScriptsOnLoadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisableLuaScriptsOnLoad ^= true;
		}

		private void autoloadSessionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadLuaSession ^= true;
		}

		private void TogglePause()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int x = 0; x < indexes.Count; x++)
				{
					var item = luaList[indexes[x]];
					if (!item.IsSeparator)
						item.TogglePause();
				}
			}
			LuaListView.Refresh();
		}

		private void pauseResumeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TogglePause();
		}

		private void toolStripButton1_Click_1(object sender, EventArgs e)
		{
			TogglePause();
		}

		private void resumePauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TogglePause();
		}

		private void onlineDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/BizHawk/LuaFunctions.html");
		}

		private void DoLuaWriter()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count == 0)
				return;

			if (indexes.Count > 0)
			{
				//If/When we want multiple file editing
				/*
				for (int x = 0; x < indexes.Count; x++)
				{
					var item = luaList[indexes[x]];
					if (!item.IsSeparator)
					{
						OpenLuaWriter(luaList[indexes[x]].Path);
					}
				}
				*/
				var item = luaList[indexes[0]];
				if (!item.IsSeparator)
				{
					OpenLuaWriter(luaList[indexes[0]].Path);
				}
			}
		}

		private void OpenLuaWriter(string path)
		{
			LuaWriter writer = new LuaWriter {CurrentFile = path};
			writer.Show();
		}

		private void newScriptToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewScript();
		}

		private void NewScript()
		{
			OpenLuaWriter(null);
		}

		private void newStripButton1_Click(object sender, EventArgs e)
		{
			NewScript();
		}

		private void showRegisteredFunctionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions.Any())
			{
				LuaRegisteredFunctionsList dialog = new LuaRegisteredFunctionsList();
				dialog.ShowDialog();
			}
		}

		private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
		{
			registeredFunctionsToolStripMenuItem.Enabled = Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions.Any();
		}
	}
}
