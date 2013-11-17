using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaConsole : Form, IToolForm
	{
		//TODO: remember column widths
		//TODO: restore column width on restore default settings

		public EmuLuaLibrary LuaImp;

		private int _defaultWidth;	//For saving the default size of the dialog, so the user can restore if desired
		private int _defaultHeight;
		private string _currentSessionFile = String.Empty;
		private List<LuaFile> _luaList = new List<LuaFile>();
		private readonly string _lastLuaFile = String.Empty; //TODO: this isn't getting used!
		private bool _changes;

		public bool UpdateBefore { get { return true; } }
		public void UpdateValues() { }

		public LuaConsole Get() { return this; }

		public void AddText(string s)
		{
			OutputBox.Text += s + "\n\n";
			OutputBox.SelectionStart = OutputBox.Text.Length;
			OutputBox.ScrollToCaret();
		}

		public LuaConsole()
		{
			InitializeComponent();
			LuaImp = new EmuLuaLibrary(this);
			Closing += (o, e) => SaveConfigSettings();
			LuaListView.QueryItemText += LuaListView_QueryItemText;
			LuaListView.QueryItemBkColor += LuaListView_QueryItemBkColor;
			LuaListView.VirtualMode = true;
		}

		private void Changes(bool changesOccured)
		{
			if (changesOccured)
			{
				_changes = true;
				OutputMessages.Text = "* " + Path.GetFileName(_currentSessionFile);
			}
			else
			{
				_changes = false;
				OutputMessages.Text = Path.GetFileName(_currentSessionFile);
			}
		}

		private void LuaListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (column == 0)
			{
				if (_luaList[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (_luaList[index].Enabled && !_luaList[index].Paused)
				{
					color = Color.LightCyan;
				}
				else if (_luaList[index].Enabled && _luaList[index].Paused)
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
				text = Path.GetFileNameWithoutExtension(_luaList[index].Path); //TODO: how about a list of Names and allow the user to name them?
			if (column == 1)
				text = _luaList[index].Path;

		}

		private void LuaConsole_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			if (Global.Config.RecentLuaSession.AutoLoad)
			{
				if (!Global.Config.RecentLuaSession.Empty)
				{
					LoadSessionFromRecent(Global.Config.RecentLuaSession[0]);
				}
			}

			newStripButton1.Visible = VersionInfo.INTERIM;
			newScriptToolStripMenuItem.Visible = VersionInfo.INTERIM;
		}

		private void StopScript(int x)
		{
			_luaList[x].Stop();
			Changes(true);
		}

		private void StopAllScripts()
		{
			foreach (LuaFile t in _luaList)
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
			_defaultWidth = Size.Width;		//Save these first so that the user can restore to its original size
			_defaultHeight = Size.Height;

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
			Size = new Size(_defaultWidth, _defaultHeight);
		}

		private FileInfo GetFileFromUser(string filter)
		{
			var ofd = new OpenFileDialog();
			if (_lastLuaFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(_lastLuaFile);
			ofd.InitialDirectory = PathManager.GetLuaPath();
			ofd.Filter = filter;
			ofd.RestoreDirectory = true;


			if (!Directory.Exists(ofd.InitialDirectory))
				Directory.CreateDirectory(ofd.InitialDirectory);

			GlobalWin.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWin.Sound.StartSound();
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
				_luaList.Add(l);
				LuaListView.ItemCount = _luaList.Count;
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
				foreach (LuaFile t in _luaList)
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
			LuaListView.ItemCount = _luaList.Count;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
			autoloadConsoleToolStripMenuItem.Checked = Global.Config.AutoLoadLuaConsole;
			autoloadSessionToolStripMenuItem.Checked = Global.Config.RecentLuaSession.AutoLoad;
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
					var item = _luaList[indexes[x]];
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
			for (int x = 0; x < _luaList.Count; x++)
			{
				if (_luaList[x].Enabled && _luaList[x].Thread == null)
				{
					try
					{
						_luaList[x].Thread = LuaImp.SpawnCoroutine(_luaList[x].Path);
					}
					catch (Exception e)
					{
						if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							_luaList[x].Enabled = false;
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
			foreach (LuaFile t in _luaList)
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

			int L = _luaList.Count - separators;
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
			if (_changes)
			{
				if (string.Compare(_currentSessionFile, "") == 0)
					SaveAs();
				else SaveSession(_currentSessionFile);
				Changes(false);
				OutputMessages.Text = Path.GetFileName(_currentSessionFile) + " saved.";
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
			if (_changes) result = AskSave();

			if (result || suppressAsk)
			{
				ClearOutput();
				StopAllScripts();
				_luaList.Clear();
				DisplayLuaList();
				_currentSessionFile = "";
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
			if (_luaList.Count == 0) return;
			
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Changes(true);

				foreach (int index in indexes)
				{
					_luaList.Remove(_luaList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
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
					_luaList.Insert(indexes[0], f);
				}
			}
			else
			{
				_luaList.Add(f);
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
				LuaFile temp = _luaList[index];
				_luaList.Remove(_luaList[index]);
				_luaList.Insert(index - 1, temp);

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
				LuaFile temp = _luaList[index];

				if (index < _luaList.Count - 1)
				{

					_luaList.Remove(_luaList[index]);
					_luaList.Insert(index + 1, temp);

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
			recentToolStripMenuItem.DropDownItems.Clear();
			recentToolStripMenuItem.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentLua, LoadLuaFromRecent)
			);
		}

		private void LoadLuaFromRecent(string path)
		{
			LoadLuaFile(path);
		}

		private bool LuaAlreadyInSession(string path)
		{
			return _luaList.Any(t => path == t.Path);
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
			else if (e.KeyCode == Keys.F12 && !e.Control && !e.Alt && !e.Shift) //F12
			{
				showRegisteredFunctionsToolStripMenuItem_Click(null, null);
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
					var item = _luaList[indexes[x]];
					if (!item.IsSeparator)
						System.Diagnostics.Process.Start(_luaList[indexes[x]].Path);
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
			for (int x = 0; x < _luaList.Count; x++)
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
			if (VersionInfo.INTERIM)
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
				OutputBox.SelectionStart = OutputBox.Text.Length;
				OutputBox.ScrollToCaret();
			});
		}

		public void ClearOutputWindow()
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
				return;

			OutputBox.Invoke(() =>
			{
				OutputBox.Text = String.Empty;
				OutputBox.Refresh();
			});
		}

		private void openToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			OpenLuaFile();
		}

		private void luaFunctionsListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Sound.StopSound();
			new LuaFunctionsForm().Show();
			GlobalWin.Sound.StartSound();
		}

		public bool LoadLuaSession(string path)
		{
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			ClearOutput();
			StopAllScripts();
			_luaList = new List<LuaFile>();

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
					_luaList.Add(l);
				}
			}
			Global.Config.RecentLuaSession.Add(path);
			_currentSessionFile = path;
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
			if (_luaList != null && _luaList.Count > 0)
			{
				if (LuaImp.GuiLibrary.SurfaceIsNull)
				{
					LuaImp.GuiLibrary.DrawNewEmu();
				}
				foreach (var lf in _luaList)
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
			if (_luaList != null && _luaList.Count > 0)
			{
				if (LuaImp.GuiLibrary.SurfaceIsNull)
				{
					LuaImp.GuiLibrary.DrawNewEmu();
				}
			}
		}

		public void EndLuaDrawing()
		{
			if (_luaList != null && _luaList.Any())
			{
				LuaImp.GuiLibrary.DrawFinishEmu();
			}
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
			if (_currentSessionFile.Length > 0)
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(_currentSessionFile);
				sfd.InitialDirectory = Path.GetDirectoryName(_currentSessionFile);
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
			GlobalWin.Sound.StopSound();
			var result = sfd.ShowDialog();
			GlobalWin.Sound.StartSound();
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
				_currentSessionFile = file.FullName;
				OutputMessages.Text = Path.GetFileName(_currentSessionFile) + " saved.";
				Global.Config.RecentLuaSession.Add(file.FullName);
				Changes(false);
			}
		}

		private void SaveSession(string path)
		{
			using (StreamWriter sw = new StreamWriter(path))
			{
				string str = "";
				foreach (LuaFile t in _luaList)
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
			saveToolStripMenuItem.Enabled = _changes;
		}

		private void recentSessionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recentSessionsToolStripMenuItem.DropDownItems.Clear();
			recentSessionsToolStripMenuItem.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentLuaSession, LoadSessionFromRecent)
			);
		}

		public void LoadSessionFromRecent(string path)
		{
			bool doload = true;
			if (_changes) doload = AskSave();

			if (doload)
			{
				if (!LoadLuaSession(path))
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentLuaSession, path);
				}
				else
				{
					RunLuaScripts();
					DisplayLuaList();
					LuaListView.Refresh();
					_currentSessionFile = path;
					Changes(false);
				}
			}
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (_changes)
			{
				GlobalWin.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save changes to session?", "Lua Console", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					if (string.Compare(_currentSessionFile, "") == 0)
					{
						SaveAs();
					}
					else
						SaveSession(_currentSessionFile);
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
			foreach (LuaFile t in _luaList)
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
					if (!_luaList[indexes[i]].IsSeparator)
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

			if (_luaList.Any())
				scriptToolStripMenuItem.DropDownItems[9].Enabled = true;
			else
				scriptToolStripMenuItem.DropDownItems[9].Enabled = false;


			turnOffAllScriptsToolStripMenuItem.Enabled = luaRunning;


			showRegisteredFunctionsToolStripMenuItem.Enabled = GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.Any();
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			bool luaRunning = false;
			foreach (LuaFile t in _luaList)
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
					if (!_luaList[indexes[i]].IsSeparator)
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
			Global.Config.RecentLuaSession.AutoLoad ^= true;
		}

		private void TogglePause()
		{
			ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int x = 0; x < indexes.Count; x++)
				{
					var item = _luaList[indexes[x]];
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
				var item = _luaList[indexes[0]];
				if (!item.IsSeparator)
				{
					OpenLuaWriter(_luaList[indexes[0]].Path);
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

		private Point GetPromptPoint()
		{
			Point p = new Point(LuaListView.Location.X + 30, LuaListView.Location.Y + 30);
			return PointToScreen(p);
		}

		private void showRegisteredFunctionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaImp.RegisteredFunctions.Any())
			{
				bool alreadyOpen = false;
				foreach (Form form in Application.OpenForms)
				{
					if (form is LuaRegisteredFunctionsList)
					{
						alreadyOpen = true;
						form.Focus();
					}
				}

				if (!alreadyOpen)
				{
					var form = new LuaRegisteredFunctionsList();
					form.StartLocation = GetPromptPoint();
					form.Show();
				}
			}
		}

		private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
		{
			registeredFunctionsToolStripMenuItem.Enabled = LuaImp.RegisteredFunctions.Any();
		}
	}
}
