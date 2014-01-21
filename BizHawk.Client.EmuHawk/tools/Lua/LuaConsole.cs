using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaConsole : Form, IToolForm
	{
		// TODO:
		// remember column widths and restore column width on restore default settings
		// column reorder
		public EmuLuaLibrary LuaImp { get; set; }

		private readonly LuaFileList _luaList;
		private int _defaultWidth;
		private int _defaultHeight;
		private bool _sortReverse;
		private string _lastColumnSorted;

		public bool UpdateBefore { get { return true; } }
		public void UpdateValues() { }

		public LuaConsole Get() { return this; }

		public void ConsoleLog(string message)
		{
			OutputBox.Text += message + Environment.NewLine + Environment.NewLine;
			OutputBox.SelectionStart = OutputBox.Text.Length;
			OutputBox.ScrollToCaret();
		}

		public LuaConsole()
		{
			_sortReverse = false;
			_lastColumnSorted = String.Empty;
			_luaList = new LuaFileList
			{
				ChangedCallback = SessionChangedCallback,
				LoadCallback = ClearOutputWindow
			};

			InitializeComponent();
			LuaImp = new EmuLuaLibrary();
			Closing += (o, e) =>
			{
				if (AskSave())
				{
					SaveConfigSettings();
				}
				else
				{
					e.Cancel = true;
				}
			};
			LuaListView.QueryItemText += LuaListView_QueryItemText;
			LuaListView.QueryItemBkColor += LuaListView_QueryItemBkColor;
			LuaListView.VirtualMode = true;
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

			NewScriptToolbarItem.Visible = VersionInfo.INTERIM;
			NewScriptMenuItem.Visible = VersionInfo.INTERIM;
		}

		public void Restart()
		{
			UpdateDialog();
		}

		public void LoadLuaFile(string path)
		{
			if (LuaAlreadyInSession(path) == false)
			{
				var luaFile = new LuaFile(String.Empty, path);
				_luaList.Add(luaFile);
				LuaListView.ItemCount = _luaList.Count;
				Global.Config.RecentLua.Add(path);

				if (!Global.Config.DisableLuaScriptsOnLoad)
				{
					try
					{
						luaFile.Thread = LuaImp.SpawnCoroutine(path);
						luaFile.Enabled = true;
					}
					catch (Exception e)
					{
						if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							luaFile.Enabled = false;
							ConsoleLog(e.Message);
						}
						else
						{
							MessageBox.Show(e.ToString());
						}
					}
				}
				else
				{
					luaFile.Enabled = false;
				}

				luaFile.Paused = false;
			}
			else
			{
				foreach (var file in _luaList.Where(file => path == file.Path && file.Enabled == false && !Global.Config.DisableLuaScriptsOnLoad))
				{
					file.Toggle();
					break;
				}

				RunLuaScripts();
			}

			UpdateDialog();
		}

		public void UpdateDialog()
		{
			LuaListView.ItemCount = _luaList.Count;
			LuaListView.Refresh();
			UpdateNumberOfScripts();
			UpdateRegisteredFunctionsDialog();
		}

		public void RunLuaScripts()
		{
			foreach (var file in _luaList)
			{
				if (file.Enabled && file.Thread == null)
				{
					try
					{
						file.Thread = LuaImp.SpawnCoroutine(file.Path);
					}
					catch (Exception e)
					{
						if (e.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							file.Enabled = false;
							ConsoleLog(e.Message);
						}
						else
						{
							MessageBox.Show(e.ToString());
						}
					}
				}
				else
				{
					file.Stop();
					_luaList.Changes = true;
				}
			}
		}

		private void SessionChangedCallback()
		{
			OutputMessages.Text =
				(_luaList.Changes ? "* " : String.Empty) +
				Path.GetFileName(_luaList.Filename);
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
			text = String.Empty;

			if (column == 0)
			{
				text = Path.GetFileNameWithoutExtension(_luaList[index].Path); // TODO: how about allow the user to name scripts?
			}
			else if (column == 1)
			{
				text = _luaList[index].Path;
			}
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
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.LuaConsoleSaveWindowPosition && Global.Config.LuaConsoleWndx >= 0
				&& Global.Config.LuaConsoleWndy >= 0)
			{
				Location = new Point(Global.Config.LuaConsoleWndx, Global.Config.LuaConsoleWndy);
			}

			if (Global.Config.LuaConsoleWidth >= 0 && Global.Config.LuaConsoleHeight >= 0)
			{
				Size = new Size(Global.Config.LuaConsoleWidth, Global.Config.LuaConsoleHeight);
			}
		}

		private static FileInfo GetFileFromUser(string filter)
		{
			var ofd = new OpenFileDialog
				{
					InitialDirectory = PathManager.GetLuaPath(),
					Filter = filter,
					RestoreDirectory = true
				};

			if (!Directory.Exists(ofd.InitialDirectory))
			{
				Directory.CreateDirectory(ofd.InitialDirectory);
			}

			var result = ofd.ShowHawkDialog();
			return result == DialogResult.OK ? new FileInfo(ofd.FileName) : null;
		}

		private void UpdateNumberOfScripts()
		{
			var message = String.Empty;
			var total = SelectedFiles.Count();
			var active = _luaList.Count(file => file.Enabled);
			var paused = _luaList.Count(file => file.Enabled && file.Paused);

			if (total == 1)
			{
				message += total + " script (" + active + " active, " + paused + " paused)";
			}
			else if (total == 0)
			{
				message += total + " scripts";
			}
			else
			{
				message += total + " scripts (" + active + " active, " + paused + " paused)";
			}

			NumberOfScripts.Text = message;
		}

		private void LoadLuaFromRecent(string path)
		{
			LoadLuaFile(path);
		}

		private bool LuaAlreadyInSession(string path)
		{
			return _luaList.Any(t => path == t.Path);
		}

		public void WriteToOutputWindow(string message)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

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
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.Text = String.Empty;
				OutputBox.Refresh();
			});
		}

		public bool LoadLuaSession(string path)
		{
			return _luaList.LoadLuaSession(path);
		}

		/// <summary>
		/// resumes suspended Co-routines
		/// </summary>
		/// <param name="includeFrameWaiters">should frame waiters be waken up? only use this immediately before a frame of emulation</param>
		public void ResumeScripts(bool includeFrameWaiters)
		{
			if (_luaList.Any())
			{
				if (LuaImp.GuiLibrary.SurfaceIsNull)
				{
					LuaImp.GuiLibrary.DrawNewEmu();
				}

				foreach (var lf in _luaList)
				{
					var oldcd = Environment.CurrentDirectory; // Save old current directory before this lua thread clobbers it for the .net thread

					try
					{
						if (lf.Enabled && lf.Thread != null && !lf.Paused)
						{
							var prohibit = lf.FrameWaiting && !includeFrameWaiters;
							if (!prohibit)
							{
								// Restore this lua thread's preferred current directory
								if (lf.CurrentDirectory != null)
								{
									Environment.CurrentDirectory = lf.CurrentDirectory;
								}

								var result = LuaImp.ResumeScript(lf.Thread);
								if (result.Terminated)
								{
									lf.Stop();
								}

								lf.FrameWaiting = result.WaitForFrame;

								// If the lua thread changed its current directory, capture that here
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
							ConsoleLog(ex.ToString());
						}
						else
						{
							MessageBox.Show(ex.ToString());
						}
					}

					// Restore the current directory
					Environment.CurrentDirectory = oldcd;
				}
			}
		}

		public void StartLuaDrawing()
		{
			if (_luaList.Any() && LuaImp.GuiLibrary.SurfaceIsNull)
			{
				LuaImp.GuiLibrary.DrawNewEmu();
			}
		}

		public void EndLuaDrawing()
		{
			if (_luaList.Any())
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

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (!String.IsNullOrWhiteSpace(_luaList.Filename))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(_luaList.Filename);
				sfd.InitialDirectory = Path.GetDirectoryName(_luaList.Filename);
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
			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		private void SaveSessionAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				_luaList.SaveSession(file.FullName);
				OutputMessages.Text = Path.GetFileName(_luaList.Filename) + " saved.";
			}
		}

		public void LoadSessionFromRecent(string path)
		{
			var doload = true;
			if (_luaList.Changes)
			{
				doload = AskSave();
			}

			if (doload)
			{
				if (!_luaList.LoadLuaSession(path))
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentLuaSession, path);
				}
				else
				{
					RunLuaScripts();
					UpdateDialog();
					_luaList.Changes = false;
				}
			}
		}

		public bool AskSave()
		{
			if (_luaList.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save changes to session?", "Lua Console", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					if (!String.IsNullOrWhiteSpace(_luaList.Filename))
					{
						_luaList.SaveSession();
					}
					else
					{
						SaveSessionAs();
					}

					return true;
				}
				else if (result == DialogResult.No)
				{
					_luaList.Changes = false;
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private static void OpenLuaWriter(string path)
		{
			var writer = new LuaWriter { CurrentFile = path };
			writer.Show();
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(
				new Point(LuaListView.Location.X + 30, LuaListView.Location.Y + 30)
			);
		}

		private static void UpdateRegisteredFunctionsDialog()
		{
			foreach (var form in Application.OpenForms.OfType<LuaRegisteredFunctionsList>())
			{
				form.UpdateValues();
			}
		}

		private IEnumerable<int> SelectedIndices
		{
			get { return LuaListView.SelectedIndices.Cast<int>(); }
		}

		private IEnumerable<LuaFile> SelectedItems
		{
			get { return SelectedIndices.Select(index => _luaList[index]); }
		}

		private IEnumerable<LuaFile> SelectedFiles
		{
			get { return SelectedItems.Where(x => !x.IsSeparator); }
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveSessionMenuItem.Enabled = _luaList.Changes;
		}

		private void RecentSessionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSessionsSubMenu.DropDownItems.Clear();
			RecentSessionsSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentLuaSession, LoadSessionFromRecent)
			);
		}

		private void RecentScriptsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentScriptsSubMenu.DropDownItems.Clear();
			RecentScriptsSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentLua, LoadLuaFromRecent)
			);
		}

		private void NewSessionMenuItem_Click(object sender, EventArgs e)
		{
			var result = !_luaList.Changes || AskSave();

			if (result)
			{
				_luaList.Clear();
				ClearOutputWindow();
				UpdateDialog();
			}
		}

		private void OpenSessionMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser("Lua Session Files (*.luases)|*.luases|All Files|*.*");
			if (file != null)
			{
				_luaList.LoadLuaSession(file.FullName);
				RunLuaScripts();
				UpdateDialog();
				_luaList.Changes = false;
			}
		}

		private void SaveSessionMenuItem_Click(object sender, EventArgs e)
		{
			if (_luaList.Changes)
			{
				if (!String.IsNullOrWhiteSpace(_luaList.Filename))
				{
					_luaList.SaveSession();
				}
				else
				{
					SaveSessionAs();
				}

				OutputMessages.Text = Path.GetFileName(_luaList.Filename) + " saved.";
			}
		}

		private void SaveSessionAsMenuItem_Click(object sender, EventArgs e)
		{
			SaveSessionAs();
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Script

		private void ScriptSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToggleScriptMenuItem.Enabled =
				PauseScriptMenuItem.Enabled =
				EditScriptMenuItem.Enabled =
				SelectedFiles.Any();

			RemoveScriptMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				SelectedIndices.Any();

			SelectAllMenuItem.Enabled = _luaList.Any();
			StopAllScriptsMenuItem.Enabled = _luaList.Any(script => script.Enabled);
			RegisteredFunctionsMenuItem.Enabled = GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.Any();

			NewScriptMenuItem.Visible = VersionInfo.INTERIM;
		}

		private void NewScriptMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaWriter(null);
		}

		private void OpenScriptMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser("Lua Scripts (*.lua)|*.lua|Text (*.text)|*.txt|All Files|*.*");
			if (file != null)
			{
				LoadLuaFile(file.FullName);
				UpdateDialog();
			}
		}

		private void ToggleScriptMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var item in SelectedFiles)
			{
				item.Toggle();

				if (item.Enabled && item.Thread == null)
				{
					try
					{
						item.Thread = LuaImp.SpawnCoroutine(item.Path);
					}
					catch (Exception ex)
					{
						if (ex.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:")
						{
							item.Enabled = false;
							ConsoleLog(ex.Message);
						}
						else
						{
							MessageBox.Show(ex.ToString());
						}
					}
				}
				else if (!item.Enabled && item.Thread != null)
				{
					var items = SelectedItems.ToList();
					foreach (var sitem in items)
					{
						var temp = sitem;
						var functions = LuaImp.RegisteredFunctions.Where(x => x.Lua == temp.Thread).ToList();
						foreach (var function in functions)
						{
							LuaImp.RegisteredFunctions.Remove(function);
						}

						UpdateRegisteredFunctionsDialog();
					}

					item.Stop();
				}
			}

			_luaList.Changes = true;
			UpdateDialog();
		}

		private void PauseScriptMenuItem_Click(object sender, EventArgs e)
		{
			SelectedFiles.ToList().ForEach(x => x.TogglePause());
			UpdateDialog();
		}

		private void EditScriptMenuItem_Click(object sender, EventArgs e)
		{
			SelectedFiles.ToList().ForEach(file => System.Diagnostics.Process.Start(file.Path));
		}

		private void RemoveScriptMenuItem_Click(object sender, EventArgs e)
		{
			var items = SelectedItems.ToList();
			if (items.Any())
			{
				foreach (var item in items)
				{
					var temp = item;
					var functions = LuaImp.RegisteredFunctions.Where(x => x.Lua == temp.Thread).ToList();
					foreach (var function in functions)
					{
						LuaImp.RegisteredFunctions.Remove(function);
					}

					_luaList.Remove(item);
				}

				UpdateRegisteredFunctionsDialog();
				UpdateDialog();
			}
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			if (indices.Any() && indices.Last() < _luaList.Count)
			{
				_luaList.Insert(indices.Last(), LuaFile.SeparatorInstance);
			}
			else
			{
				_luaList.Add(LuaFile.SeparatorInstance);
			}

			UpdateDialog();
		}

		private void MoveUpMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices[0] == 0)
			{
				return;
			}

			foreach (var index in indices)
			{
				var file = _luaList[index];
				_luaList.Remove(file);
				_luaList.Insert(index - 1, file);
			}

			var newindices = indices.Select(t => t - 1).ToList();

			LuaListView.SelectedIndices.Clear();
			foreach (var newi in newindices)
			{
				LuaListView.SelectItem(newi, true);
			}

			UpdateDialog();
		}

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices.Last() == _luaList.Count - 1)
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var file = _luaList[indices[i]];
				_luaList.Remove(file);
				_luaList.Insert(indices[i] + 1, file);
			}

			var newindices = indices.Select(t => t + 1).ToList();

			LuaListView.SelectedIndices.Clear();
			foreach (var newi in newindices)
			{
				LuaListView.SelectItem(newi, true);
			}

			UpdateDialog();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < _luaList.Count; i++)
			{
				LuaListView.SelectItem(i, true);
			}
		}

		private void StopAllScriptsMenuItem_Click(object sender, EventArgs e)
		{
			_luaList.StopAllScripts();
		}

		private void RegisteredFunctionsMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaImp.RegisteredFunctions.Any())
			{
				var alreadyOpen = false;
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
					var form = new LuaRegisteredFunctionsList { StartLocation = GetPromptPoint() };
					form.Show();
				}
			}
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
			AutoloadConsoleMenuItem.Checked = Global.Config.AutoLoadLuaConsole;
			AutoloadSessionMenuItem.Checked = Global.Config.RecentLuaSession.AutoLoad;
			DisableScriptsOnLoadMenuItem.Checked = Global.Config.DisableLuaScriptsOnLoad;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.LuaConsoleSaveWindowPosition ^= true;
		}

		private void AutoloadConsoleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadLuaConsole ^= true;
		}

		private void AutoloadSessionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentLuaSession.AutoLoad ^= true;
		}

		private void DisableScriptsOnLoadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisableLuaScriptsOnLoad ^= true;
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
		}

		#endregion

		#region Help

		private void FunctionsListMenuItem_Click(object sender, EventArgs e)
		{
			new LuaFunctionsForm().Show();
		}

		private void OnlineDocsMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/BizHawk/LuaFunctions.html");
		}

		#endregion

		#region Toolbar and Context Menu

		private void EditToolbarItem_Click(object sender, EventArgs e)
		{
			if (VersionInfo.INTERIM)
			{
				SelectedFiles.ToList().ForEach(x => OpenLuaWriter(x.Path));
			}
			else
			{
				EditScriptMenuItem_Click(sender, e);
			}
		}

		private void ScriptListContextMenu_Opening(object sender, CancelEventArgs e)
		{
			ToggleScriptContextItem.Enabled =
				PauseScriptContextItem.Enabled =
				EditScriptContextItem.Enabled =
				SelectedFiles.Any();

			StopAllScriptsContextItem.Visible =
				ScriptContextSeparator.Visible =
				_luaList.Any(file => file.Enabled);
		}

		private void ConsoleContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RegisteredFunctionsContextItem.Enabled = LuaImp.RegisteredFunctions.Any();
		}

		private void ClearConsoleContextItem_Click(object sender, EventArgs e)
		{
			ClearOutputWindow();
		}

		#endregion

		#region Dialog, Listview, OutputBox

		private void LuaConsole_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			try
			{
				foreach (var path in filePaths)
				{
					if (Path.GetExtension(path) == ".lua" || Path.GetExtension(path) == ".txt")
					{
						LoadLuaFile(path);
						UpdateDialog();
					}
					else if (Path.GetExtension(path) == ".luases")
					{
						_luaList.LoadLuaSession(path);
						RunLuaScripts();
						UpdateDialog();
						_luaList.Changes = false;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				if (ex.ToString().Substring(0, 32) == "LuaInterface.LuaScriptException:" || ex.ToString().Substring(0, 26) == "LuaInterface.LuaException:")
				{
					ConsoleLog(ex.Message);
				}
				else
				{
					MessageBox.Show(ex.Message);
				}
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
				RemoveScriptMenuItem_Click(null, null);
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) // Select All
			{
				SelectAllMenuItem_Click(null, null);
			}
			else if (e.KeyCode == Keys.F12 && !e.Control && !e.Alt && !e.Shift) // F12
			{
				RegisteredFunctionsMenuItem_Click(null, null);
			}
		}

		private void LuaListView_ItemActivate(object sender, EventArgs e)
		{
			ToggleScriptMenuItem_Click(sender, e);
		}

		private void OutputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F12 && !e.Control && !e.Alt && !e.Shift) // F12
			{
				RegisteredFunctionsMenuItem_Click(null, null);
			}
		}

		/// <summary> -MightyMar
		/// Sorts the column Ascending on the first click and Descending on the second click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LuaListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			String columnToSort = LuaListView.Columns[e.Column].Text;
			List<LuaFile> luaListTemp = new List<LuaFile>();
			if (columnToSort != _lastColumnSorted)
			{
				_sortReverse = false;
			}

			//For getting the name of the .lua file, for some reason this field is kept blank in LuaFile.cs?
			//The Name variable gets emptied again near the end just in case it would break something.
			for (int i = 0; i < _luaList.Count; i++)
			{
				String[] words = Regex.Split(_luaList[i].Path, ".lua");
				String[] split = words[0].Split('\\');

				luaListTemp.Add(_luaList[i]);
				luaListTemp[i].Name = split[split.Count() - 1];
			}

			//Script, Path
			switch (columnToSort)
			{
				case "Script":
					if (_sortReverse)
					{
						luaListTemp = luaListTemp.OrderByDescending(x => x.Name).ThenBy(x => x.Path).ToList();
					}
					else
					{
						luaListTemp = luaListTemp.OrderBy(x => x.Name).ThenBy(x => x.Path).ToList();

					}
					break;
				case "Path":
					if (_sortReverse)
					{
						luaListTemp = luaListTemp.OrderByDescending(x => x.Path).ThenBy(x => x.Name).ToList();
					}
					else
					{
						luaListTemp = luaListTemp.OrderBy(x => x.Path).ThenBy(x => x.Name).ToList();
					}
					break;
			}

			for (int i = 0; i < _luaList.Count; i++)
			{
				_luaList[i] = luaListTemp[i];
				_luaList[i].Name = String.Empty;
			}
			UpdateDialog();
			_lastColumnSorted = columnToSort;
			_sortReverse = !_sortReverse;
		}



		#endregion


		#endregion
	}
}
