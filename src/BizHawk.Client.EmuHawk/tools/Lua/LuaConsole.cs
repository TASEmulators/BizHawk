using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaConsole : ToolFormBase, IToolFormAutoConfig
	{
		private const string IconColumnName = "Icon";
		private const string ScriptColumnName = "Script";
		private const string PathColumnName = "PathName";

		private static readonly FilesystemFilterSet SessionsFSFilterSet = new FilesystemFilterSet(new FilesystemFilter("Lua Session Files", new[] { "luases" }));

		private readonly LuaAutocompleteInstaller _luaAutoInstaller = new LuaAutocompleteInstaller();
		private readonly List<FileSystemWatcher> _watches = new List<FileSystemWatcher>();

		private readonly int _defaultSplitDistance;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private bool _sortReverse;
		private string _lastColumnSorted;

		private readonly List<string> _consoleCommandHistory = new List<string>();
		private int _consoleCommandHistoryIndex = -1;

		public ToolDialogSettings.ColumnList Columns { get; set; }

		public class LuaConsoleSettings
		{
			public LuaConsoleSettings()
			{
				Columns = new List<RollColumn>
				{
					new RollColumn { Name = IconColumnName, Text = " ", Visible = true, UnscaledWidth = 22, Type = ColumnType.Image },
					new RollColumn { Name = ScriptColumnName, Text = "Script", Visible = true, UnscaledWidth = 92, Type = ColumnType.Text },
					new RollColumn { Name = PathColumnName, Text = "Path", Visible = true, UnscaledWidth = 300, Type = ColumnType.Text }
				};
			}

			public List<RollColumn> Columns { get; set; }

			public bool ReloadOnScriptFileChange { get; set; }
			public bool ToggleAllIfNoneSelected { get; set; } = true;

			public int SplitDistance { get; set; }

			public bool DisableLuaScriptsOnLoad { get; set; }
		}

		[ConfigPersist]
		public LuaConsoleSettings Settings { get; set; }

		public LuaConsole()
		{
			Settings = new LuaConsoleSettings();
			_sortReverse = false;
			_lastColumnSorted = "";

			InitializeComponent();
			ToggleScriptContextItem.Image = Resources.Refresh1;
			PauseScriptContextItem.Image = Resources.Pause;
			EditScriptContextItem.Image = Resources.CutHS;
			RemoveScriptContextItem.Image = Resources.Close;
			InsertSeperatorContextItem.Image = Resources.InsertSeparator;
			StopAllScriptsContextItem.Image = Resources.Stop;
			ClearRegisteredFunctionsContextItem.Image = Resources.Delete;
			NewSessionMenuItem.Image = Resources.NewFile;
			OpenSessionMenuItem.Image = Resources.OpenFile;
			SaveSessionMenuItem.Image = Resources.SaveAs;
			NewScriptMenuItem.Image = Resources.NewFile;
			OpenScriptMenuItem.Image = Resources.OpenFile;
			RefreshScriptMenuItem.Image = Resources.Refresh1;
			ToggleScriptMenuItem.Image = Resources.checkbox;
			PauseScriptMenuItem.Image = Resources.Pause;
			EditScriptMenuItem.Image = Resources.CutHS;
			RemoveScriptMenuItem.Image = Resources.Delete;
			InsertSeparatorMenuItem.Image = Resources.InsertSeparator;
			MoveUpMenuItem.Image = Resources.MoveUp;
			MoveDownMenuItem.Image = Resources.MoveDown;
			StopAllScriptsMenuItem.Image = Resources.Stop;
			RegisterSublimeText2MenuItem.Image = Resources.GreenCheck;
			ClearRegisteredFunctionsLogContextItem.Image = Resources.Delete;
			NewScriptToolbarItem.Image = Resources.NewFile;
			OpenScriptToolbarItem.Image = Resources.OpenFile;
			ToggleScriptToolbarItem.Image = Resources.checkbox;
			RefreshScriptToolbarItem.Image = Resources.Refresh1;
			PauseToolbarItem.Image = Resources.Pause;
			EditToolbarItem.Image = Resources.CutHS;
			RemoveScriptToolbarItem.Image = Resources.Delete;
			DuplicateToolbarButton.Image = Resources.Duplicate;
			MoveUpToolbarItem.Image = Resources.MoveUp;
			toolStripButtonMoveDown.Image = Resources.MoveDown;
			InsertSeparatorToolbarItem.Image = Resources.InsertSeparator;
			EraseToolbarItem.Image = Resources.Erase;
			RecentScriptsSubMenu.Image = Resources.Recent;
			Icon = Resources.textdoc_MultiSize;

			Closing += (o, e) =>
			{
				if (AskSaveChanges())
				{
					Settings.Columns = LuaListView.AllColumns;
					
					GlobalWin.DisplayManager.ClearLuaSurfaces();

					if (GlobalWin.DisplayManager.ClientExtraPadding != Padding.Empty)
					{
						GlobalWin.DisplayManager.ClientExtraPadding = new Padding(0);
						MainForm.FrameBufferResized();
					}

					if (GlobalWin.DisplayManager.GameExtraPadding != Padding.Empty)
					{
						GlobalWin.DisplayManager.GameExtraPadding = new Padding(0);
						MainForm.FrameBufferResized();
					}

					LuaImp.GuiLibrary?.DrawFinish();
					LuaImp?.Close();
					GlobalWin.OSD.ClearGuiText();
				}
				else
				{
					e.Cancel = true;
				}
			};

			LuaListView.QueryItemText += LuaListView_QueryItemText;
			LuaListView.QueryItemBkColor += LuaListView_QueryItemBkColor;
			LuaListView.QueryItemIcon += LuaListView_QueryItemImage;

			// this is bad, in case we ever have more than one gui part running lua.. not sure how much other badness there is like that
			LuaSandbox.DefaultLogger = WriteToOutputWindow;
			_defaultSplitDistance = splitContainer1.SplitterDistance;
		}

		public LuaLibraries LuaImp { get; private set; }

		private IEnumerable<LuaFile> SelectedItems =>  LuaListView.SelectedRows.Select(index => LuaImp.ScriptList[index]);

		private IEnumerable<LuaFile> SelectedFiles => SelectedItems.Where(x => !x.IsSeparator);

		private void LuaConsole_Load(object sender, EventArgs e)
		{
			// Hack for previous config settings
			if (Settings.Columns.Any(c => c.Text == null))
			{
				Settings = new LuaConsoleSettings();
			}

			LuaImp.ScriptList.ChangedCallback = SessionChangedCallback;

			if (Config.RecentLuaSession.AutoLoad && !Config.RecentLuaSession.Empty)
			{
				LoadSessionFromRecent(Config.RecentLuaSession.MostRecent);
			}
			else if (Config.RecentLua.AutoLoad)
			{
				if (!Config.RecentLua.Empty)
				{
					LoadLuaFile(Config.RecentLua.MostRecent);
				}
			}

			if (OSTailoredCode.IsUnixHost)
			{
				OpenSessionMenuItem.Enabled = false;
				RecentSessionsSubMenu.Enabled = false;
				RecentScriptsSubMenu.Enabled = false;
				NewScriptMenuItem.Enabled = false;
				OpenScriptMenuItem.Enabled = false;
				NewScriptToolbarItem.Enabled = false;
				OpenScriptToolbarItem.Enabled = false;
				WriteToOutputWindow("The Lua environment can currently only be created on Windows. You may not load scripts.");
			}

			LuaListView.AllColumns.Clear();
			SetColumns();

			if (Settings.SplitDistance > 0)
			{
				try
				{
					splitContainer1.SplitterDistance = Settings.SplitDistance;
				}
				catch (Exception)
				{
					splitContainer1.SplitterDistance = _defaultSplitDistance;
				}
			}
		}

		private void BranchesMarkersSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.SplitDistance = splitContainer1.SplitterDistance;
		}

		public void Restart()
		{
			var runningScripts = new List<LuaFile>();

			if (LuaImp != null) // Things we need to do with the existing LuaImp before we can make a new one
			{
				if (LuaImp.IsRebootingCore)
				{
					// Even if the lua console is self-rebooting from client.reboot_core() we still want to re-inject dependencies
					LuaImp.Restart(Emulator.ServiceProvider);
					return;
				}

				if (LuaImp.GuiLibrary != null && LuaImp.GuiLibrary.HasLuaSurface)
				{
					LuaImp.GuiLibrary.DrawFinish();
				}

				runningScripts = LuaImp.RunningScripts.ToList();

				foreach (var file in runningScripts)
				{
					LuaImp.CallExitEvent(file);

					LuaImp.RegisteredFunctions.RemoveForFile(file, Emulator);
					UpdateRegisteredFunctionsDialog();

					file.Stop();
				}
			}

			var currentScripts = LuaImp?.ScriptList; // Temp fix for now
			LuaImp = OSTailoredCode.IsUnixHost ? (LuaLibraries) new UnixLuaLibraries() : new Win32LuaLibraries((MainForm) MainForm, Emulator.ServiceProvider);
			LuaImp.ScriptList.AddRange(currentScripts ?? Enumerable.Empty<LuaFile>());

			InputBox.AutoCompleteCustomSource.AddRange(LuaImp.Docs.Select(a => $"{a.Library}.{a.Name}").ToArray());

			foreach (var file in runningScripts)
			{
				string pathToLoad = ProcessPath(file.Path);

				try
				{
					LuaSandbox.Sandbox(file.Thread, () =>
					{
						LuaImp.SpawnAndSetFileThread(pathToLoad, file);
						LuaSandbox.CreateSandbox(file.Thread, Path.GetDirectoryName(pathToLoad));
						file.State = LuaFile.RunState.Running;
					}, () =>
					{
						file.State = LuaFile.RunState.Disabled;
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}

			UpdateDialog();
		}

		private void SetColumns()
		{
			foreach (var column in Settings.Columns)
			{
				if (LuaListView.AllColumns[column.Name] == null)
				{
					LuaListView.AllColumns.Add(column);
				}
			}
		}

		private void AddFileWatches()
		{
			if (Settings.ReloadOnScriptFileChange)
			{
				_watches.Clear();
				foreach (var item in LuaImp.ScriptList.Where(s => !s.IsSeparator))
				{
					var processedPath = Config.PathEntries.TryMakeRelative(item.Path);
					string pathToLoad = ProcessPath(processedPath);

					CreateFileWatcher(pathToLoad);
				}
			}
		}

		private void CreateFileWatcher(string path)
		{
			var watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(path),
				Filter = Path.GetFileName(path),
				NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
							 | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				EnableRaisingEvents = true
			};

			// TODO, Deleted and Renamed events
			watcher.Changed += OnChanged;

			_watches.Add(watcher);
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			// Even after _watches is cleared, these callbacks hang around! So this check is necessary
			var script = LuaImp.ScriptList.FirstOrDefault(s => s.Path == e.FullPath && s.Enabled);

			if (script != null)
			{
				Invoke(new MethodInvoker(delegate { RefreshScriptMenuItem_Click(null, null); }));
			}
		}

		public void LoadLuaFile(string path)
		{
			var processedPath = Config.PathEntries.TryMakeRelative(path);

			var alreadyInSession = LuaImp.ScriptList.Any(t => processedPath == t.Path);
			if (alreadyInSession)
			{
				foreach (var file in LuaImp.ScriptList
					.Where(file => processedPath == file.Path
						&& file.Enabled == false
						&& !Config.DisableLuaScriptsOnLoad))
				{
					if (file.Thread != null)
					{
						file.Toggle();
					}

					break;
				}

				RunLuaScripts();
			}
			else
			{
				var luaFile = new LuaFile("", processedPath);

				LuaImp.ScriptList.Add(luaFile);
				LuaListView.RowCount = LuaImp.ScriptList.Count;
				Config.RecentLua.Add(processedPath);

				if (!Config.DisableLuaScriptsOnLoad)
				{
					luaFile.State = LuaFile.RunState.Running;
					EnableLuaFile(luaFile);
				}
				else
				{
					luaFile.State = LuaFile.RunState.Disabled;
				}

				if (Settings.ReloadOnScriptFileChange)
				{
					CreateFileWatcher(processedPath);
				}
			}

			UpdateDialog();
		}

		private void UpdateDialog()
		{
			LuaListView.RowCount = LuaImp.ScriptList.Count;
			UpdateNumberOfScripts();
			UpdateRegisteredFunctionsDialog();
		}

		private void RunLuaScripts()
		{
			foreach (var file in LuaImp.ScriptList.Where(s => !s.IsSeparator))
			{
				if (!file.Enabled && file.Thread == null)
				{
					try
					{
						LuaSandbox.Sandbox(null, () =>
						{
							string pathToLoad = ProcessPath(file.Path);
							LuaImp.SpawnAndSetFileThread(file.Path, file);
							LuaSandbox.CreateSandbox(file.Thread, Path.GetDirectoryName(pathToLoad));
						}, () =>
						{
							file.State = LuaFile.RunState.Disabled;
						});
					}
					catch (Exception e)
					{
						MessageBox.Show(e.ToString());
					}
				}
				else
				{
					file.Stop();
				}
			}
		}

		private void SessionChangedCallback()
		{
			OutputMessages.Text =
				(LuaImp.ScriptList.Changes ? "* " : "") +
				Path.GetFileName(LuaImp.ScriptList.Filename);
		}

		private void LuaListView_QueryItemImage(int index, RollColumn column, ref Bitmap bitmap, ref int offsetX, ref int offsetY)
		{
			if (column.Name != IconColumnName)
			{
				return;
			}

			if (LuaImp.ScriptList[index].IsSeparator)
			{
				return;
			}

			if (LuaImp.ScriptList[index].Paused)
			{
				bitmap = Properties.Resources.Pause;
			}
			else if (LuaImp.ScriptList[index].Enabled)
			{
				bitmap = Properties.Resources.ts_h_arrow_green;
			}
			else
			{
				bitmap = Properties.Resources.Stop;
			}
		}

		private void LuaListView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if (LuaImp.ScriptList[index].IsSeparator)
			{
				color = BackColor;
			}
			else if (LuaImp.ScriptList[index].Enabled && !LuaImp.ScriptList[index].Paused)
			{
				color = Color.LightCyan;
			}
			else if (LuaImp.ScriptList[index].Enabled && LuaImp.ScriptList[index].Paused)
			{
				color = Color.LightPink;
			}
		}

		private void LuaListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			if (LuaImp.ScriptList[index].IsSeparator)
			{
				return;
			}

			if (column.Name == ScriptColumnName)
			{
				text = Path.GetFileNameWithoutExtension(LuaImp.ScriptList[index].Path); // TODO: how about allow the user to name scripts?
			}
			else if (column.Name == PathColumnName)
			{
				text = DressUpRelative(LuaImp.ScriptList[index].Path);
			}
		}

		private string DressUpRelative(string path)
		{
			return path.StartsWith(".\\") ? path.Replace(".\\", "") : path;
		}

		private void UpdateNumberOfScripts()
		{
			var message = "";
			var total = LuaImp.ScriptList.Count(file => !file.IsSeparator);
			var active = LuaImp.ScriptList.Count(file => !file.IsSeparator && file.Enabled);
			var paused = LuaImp.ScriptList.Count(file => !file.IsSeparator && file.Enabled && file.Paused);

			if (total == 1)
			{
				message += $"{total} script ({active} active, {paused} paused)";
			}
			else if (total == 0)
			{
				message += $"{total} scripts";
			}
			else
			{
				message += $"{total} scripts ({active} active, {paused} paused)";
			}

			NumberOfScripts.Text = message;
		}

		private void WriteLine(string message) => WriteToOutputWindow(message + "\n");

		private int _messageCount;
		private const int MaxCount = 100;
		public void WriteToOutputWindow(string message)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			_messageCount++;

			if (_messageCount <= MaxCount)
			{
				if (_messageCount == MaxCount)
				{
					message = "Message Cap reached, supressing output.\n";
				}

				OutputBox.Invoke(() =>
				{
					OutputBox.Text += message;
					OutputBox.SelectionStart = OutputBox.Text.Length;
					OutputBox.ScrollToCaret();
				});
			}
			
		}

		public void ClearOutputWindow()
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.Text = "";
				OutputBox.Refresh();
			});
		}

		public bool LoadLuaSession(string path)
		{
			var result = LuaImp.ScriptList.Load(path, Settings.DisableLuaScriptsOnLoad);

			RunLuaScripts();
			UpdateDialog();
			LuaImp.ScriptList.Changes = false;

			Config.RecentLuaSession.Add(path);
			foreach (var script in LuaImp.ScriptList)
			{
				Config.RecentLua.Add(script.Path);
			}

			ClearOutputWindow();
			return result;
		}

		protected override void UpdateBefore()
		{
			if (LuaImp.SuppressLua)
			{
				return;
			}

			LuaImp.CallFrameBeforeEvent();
			LuaImp.StartLuaDrawing();
		}

		protected override void UpdateAfter()
		{
			if (LuaImp.SuppressLua)
			{
				return;
			}

			LuaImp.CallFrameAfterEvent();
			ResumeScripts(true);
			LuaImp.EndLuaDrawing();
		}

		protected override void FastUpdateBefore()
		{
			if (Config.RunLuaDuringTurbo)
			{
				UpdateBefore();
			}
		}

		protected override void FastUpdateAfter()
		{
			if (Config.RunLuaDuringTurbo)
			{
				UpdateAfter();
			}
		}

		/// <summary>
		/// resumes suspended Co-routines
		/// </summary>
		/// <param name="includeFrameWaiters">should frame waiters be waken up? only use this immediately before a frame of emulation</param>
		public void ResumeScripts(bool includeFrameWaiters)
		{
			if (!LuaImp.ScriptList.Any())
			{
				return;
			}

			if (LuaImp.SuppressLua)
			{
				return;
			}

			if (MainForm.IsTurboing && !Config.RunLuaDuringTurbo)
			{
				return;
			}

			if (LuaImp.GuiLibrary?.SurfaceIsNull == true)
			{
				LuaImp.GuiLibrary.DrawNew("emu");
			}

			foreach (var lf in LuaImp.ScriptList.Where(l => l.Enabled && l.Thread != null && !l.Paused))
			{
				try
				{
					LuaSandbox.Sandbox(lf.Thread, () =>
					{
						var prohibit = lf.FrameWaiting && !includeFrameWaiters;
						if (!prohibit)
						{
							var result = LuaImp.ResumeScript(lf);
							if (result.Terminated)
							{
								LuaImp.CallExitEvent(lf);
								lf.Stop();
								UpdateDialog();
							}

							lf.FrameWaiting = result.WaitForFrame;
						}
					}, () =>
					{
						lf.Stop();
						LuaListView.Refresh();
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}

			_messageCount = 0;
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (!string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(LuaImp.ScriptList.Filename);
				sfd.InitialDirectory = Path.GetDirectoryName(LuaImp.ScriptList.Filename);
			}
			else if (!Game.IsNullInstance())
			{
				sfd.FileName = Game.FilesystemSafeName();
				sfd.InitialDirectory = Config.PathEntries.LuaAbsolutePath();
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = Config.PathEntries.LuaAbsolutePath();
			}

			sfd.Filter = SessionsFSFilterSet.ToString();
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			return result.IsOk() ? new FileInfo(sfd.FileName) : null;
		}

		private void SaveSessionAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				var path = Config.PathEntries
					.AbsolutePathFor(file.FullName, "")
					.MakeRelativeTo(Path.GetDirectoryName(file.FullName));
				LuaImp.ScriptList.Save(path);

				Config.RecentLuaSession.Add(file.FullName); // TODO: should path be used here?
				OutputMessages.Text = $"{Path.GetFileName(LuaImp.ScriptList.Filename)} saved.";
			}
		}

		private void LoadSessionFromRecent(string path)
		{
			var load = true;
			if (LuaImp.ScriptList.Changes)
			{
				load = AskSaveChanges();
			}

			if (load)
			{
				if (!LoadLuaSession(path))
				{
					Config.RecentLuaSession.HandleLoadError(path);
				}
			}

			AddFileWatches();
		}

		public override bool AskSaveChanges()
		{
			if (LuaImp.ScriptList.Changes && !string.IsNullOrEmpty(LuaImp.ScriptList.Filename))
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save changes to session?", "Lua Console", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					SaveOrSaveAs();

					return true;
				}

				if (result == DialogResult.No)
				{
					LuaImp.ScriptList.Changes = false;
					return true;
				}

				if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private static void UpdateRegisteredFunctionsDialog()
		{
			foreach (var form in Application.OpenForms.OfType<LuaRegisteredFunctionsList>().ToList())
			{
				form.UpdateValues();
			}
		}

		private void SaveOrSaveAs()
		{
			if (!string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename))
			{
				LuaImp.ScriptList.Save(LuaImp.ScriptList.Filename);
				Config.RecentLuaSession.Add(LuaImp.ScriptList.Filename);
			}
			else
			{
				SaveSessionAs();
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveSessionMenuItem.Enabled = LuaImp.ScriptList.Changes;
		}

		private void RecentSessionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSessionsSubMenu.DropDownItems.Clear();
			RecentSessionsSubMenu.DropDownItems.AddRange(Config.RecentLuaSession.RecentMenu(LoadSessionFromRecent, "Session"));
		}

		private void RecentScriptsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentScriptsSubMenu.DropDownItems.Clear();
			RecentScriptsSubMenu.DropDownItems.AddRange(Config.RecentLua.RecentMenu(LoadLuaFile, "Script"));
		}

		private void NewSessionMenuItem_Click(object sender, EventArgs e)
		{
			var result = !LuaImp.ScriptList.Changes || AskSaveChanges();

			if (result)
			{
				LuaImp.ScriptList.Clear();
				ClearOutputWindow();
				UpdateDialog();
			}
		}

		private void OpenSessionMenuItem_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				InitialDirectory = Config.PathEntries.LuaAbsolutePath(),
				Filter = SessionsFSFilterSet.ToString(),
				RestoreDirectory = true,
				Multiselect = false
			};

			if (!Directory.Exists(ofd.InitialDirectory))
			{
				Directory.CreateDirectory(ofd.InitialDirectory);
			}

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
			{
				LoadLuaSession(ofd.FileName);
			}
		}

		private void SaveSessionMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaImp.ScriptList.Changes)
			{
				SaveOrSaveAs();
				OutputMessages.Text = $"{Path.GetFileName(LuaImp.ScriptList.Filename)} saved.";
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

		private void ScriptSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToggleScriptMenuItem.Enabled =
				PauseScriptMenuItem.Enabled =
				EditScriptMenuItem.Enabled =
				SelectedFiles.Any();

			RemoveScriptMenuItem.Enabled =
				DuplicateScriptMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				LuaListView.SelectedRows.Any();

			SelectAllMenuItem.Enabled = LuaImp.ScriptList.Any();
			StopAllScriptsMenuItem.Enabled = LuaImp.ScriptList.Any(script => script.Enabled);
			RegisteredFunctionsMenuItem.Enabled = LuaImp.RegisteredFunctions.Any();
		}

		private void NewScriptMenuItem_Click(object sender, EventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				InitialDirectory = !string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename)
					? Path.GetDirectoryName(LuaImp.ScriptList.Filename)
					: Config.PathEntries.LuaAbsolutePath(),
				DefaultExt = ".lua",
				FileName = !string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename)
					? Path.GetFileNameWithoutExtension(LuaImp.ScriptList.Filename)
					: Path.GetFileNameWithoutExtension(Game.Name),
				OverwritePrompt = true,
				Filter = new FilesystemFilterSet(FilesystemFilter.LuaScripts).ToString()
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK
				&& !string.IsNullOrWhiteSpace(sfd.FileName))
			{
				string defaultTemplate = "while true do\n\temu.frameadvance();\nend";
				File.WriteAllText(sfd.FileName, defaultTemplate);
				LuaImp.ScriptList.Add(new LuaFile(Path.GetFileNameWithoutExtension(sfd.FileName), sfd.FileName));
				UpdateDialog();
				Process.Start(new ProcessStartInfo
				{
					Verb = "Open",
					FileName = sfd.FileName
				});
				AddFileWatches();
			}
		}

		private void OpenScriptMenuItem_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				InitialDirectory = Config.PathEntries.LuaAbsolutePath(),
				Filter = new FilesystemFilterSet(FilesystemFilter.LuaScripts, FilesystemFilter.TextFiles).ToString(),
				RestoreDirectory = true,
				Multiselect = true
			};

			if (!Directory.Exists(ofd.InitialDirectory))
			{
				Directory.CreateDirectory(ofd.InitialDirectory);
			}

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK && ofd.FileNames != null)
			{
				foreach (var file in ofd.FileNames)
				{
					LoadLuaFile(file);
				}

				UpdateDialog();
			}
		}

		private void ToggleScriptMenuItem_Click(object sender, EventArgs e)
		{
			var files = !SelectedFiles.Any() && Settings.ToggleAllIfNoneSelected
				? LuaImp.ScriptList
				: SelectedFiles;
			foreach (var file in files)
			{
				ToggleLuaScript(file);
			}

			UpdateDialog();
		}

		private void EnableLuaFile(LuaFile item)
		{
			try
			{
				LuaSandbox.Sandbox(null, () =>
				{
					string pathToLoad = Path.IsPathRooted(item.Path)
					? item.Path
					: item.Path.MakeProgramRelativePath();

					LuaImp.SpawnAndSetFileThread(pathToLoad, item);
					LuaSandbox.CreateSandbox(item.Thread, Path.GetDirectoryName(pathToLoad));
				}, () =>
				{
					item.State = LuaFile.RunState.Disabled;
				});

				ReDraw();
			}
			catch (IOException)
			{
				WriteLine($"Unable to access file {item.Path}");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void ReDraw()
		{
			// Shenanigans
			// We want any gui.text messages from a script to immediately update even when paused
			GlobalWin.OSD.ClearGuiText();
			Tools.UpdateToolsAfter();
			LuaImp.EndLuaDrawing();
			LuaImp.StartLuaDrawing();
		}

		private void PauseScriptMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var x in SelectedFiles)
			{
				x.TogglePause();
			}
			UpdateDialog();
		}

		private string ProcessPath(string path)
		{
			return Path.IsPathRooted(path)
				? path
				: path.MakeProgramRelativePath();
		}

		private void EditScriptMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var file in SelectedFiles)
			{
				Process.Start(new ProcessStartInfo
				{
					Verb = "Open",
					FileName = ProcessPath(file.Path)
				});
			}
		}

		private void RemoveScriptMenuItem_Click(object sender, EventArgs e)
		{
			var items = SelectedItems.ToList();
			if (items.Any())
			{
				foreach (var item in items)
				{
					LuaImp.RegisteredFunctions.RemoveForFile(item, Emulator);
					LuaImp.ScriptList.Remove(item);
				}
				
				UpdateRegisteredFunctionsDialog();
				UpdateDialog();
			}
		}

		private void DuplicateScriptMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaListView.SelectedRows.Any())
			{
				var script = SelectedItems.First();

				if (script.IsSeparator)
				{
					LuaImp.ScriptList.Add(LuaFile.SeparatorInstance);
					UpdateDialog();
					return;
				}

				var sfd = new SaveFileDialog
				{
					InitialDirectory = Path.GetDirectoryName(script.Path),
					DefaultExt = ".lua",
					FileName = $"{Path.GetFileNameWithoutExtension(script.Path)} (1)",
					OverwritePrompt = true,
					Filter = new FilesystemFilterSet(FilesystemFilter.LuaScripts).ToString()
				};

				if (sfd.ShowDialog().IsOk())
				{
					string text = File.ReadAllText(script.Path);
					File.WriteAllText(sfd.FileName, text);
					LuaImp.ScriptList.Add(new LuaFile(Path.GetFileNameWithoutExtension(sfd.FileName), sfd.FileName));
					UpdateDialog();
					Process.Start(new ProcessStartInfo
					{
						Verb = "Open",
						FileName = sfd.FileName
					});
				}
			}
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			var indices = LuaListView.SelectedRows.ToList();
			if (indices.Any() && indices.Last() < LuaImp.ScriptList.Count)
			{
				LuaImp.ScriptList.Insert(indices.Last(), LuaFile.SeparatorInstance);
			}
			else
			{
				LuaImp.ScriptList.Add(LuaFile.SeparatorInstance);
			}

			UpdateDialog();
		}

		private void MoveUpMenuItem_Click(object sender, EventArgs e)
		{
			var indices = LuaListView.SelectedRows.ToList();
			if (indices.Count == 0 || indices[0] == 0)
			{
				return;
			}

			foreach (var index in indices)
			{
				var file = LuaImp.ScriptList[index];
				LuaImp.ScriptList.Remove(file);
				LuaImp.ScriptList.Insert(index - 1, file);
			}

			var newIndices = indices.Select(t => t - 1);

			LuaListView.DeselectAll();
			foreach (var i in newIndices)
			{
				LuaListView.SelectRow(i, true);
			}

			UpdateDialog();
		}

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
		{
			var indices = LuaListView.SelectedRows.ToList();
			if (indices.Count == 0 || indices.Last() == LuaImp.ScriptList.Count - 1)
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var file = LuaImp.ScriptList[indices[i]];
				LuaImp.ScriptList.Remove(file);
				LuaImp.ScriptList.Insert(indices[i] + 1, file);
			}

			var newIndices = indices.Select(t => t + 1);

			LuaListView.DeselectAll();
			foreach (var i in newIndices)
			{
				LuaListView.SelectRow(i, true);
			}

			UpdateDialog();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			LuaListView.SelectAll();
		}

		private void StopAllScriptsMenuItem_Click(object sender, EventArgs e)
		{
			LuaImp.ScriptList.StopAllScripts();
			UpdateDialog();
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
					new LuaRegisteredFunctionsList(LuaImp.RegisteredFunctions)
					{
						StartLocation = this.ChildPointToScreen(LuaListView)
					}.Show();
				}
			}
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisableScriptsOnLoadMenuItem.Checked = Config.DisableLuaScriptsOnLoad;
			ReturnAllIfNoneSelectedMenuItem.Checked = Settings.ToggleAllIfNoneSelected;
			ReloadWhenScriptFileChangesMenuItem.Checked = Settings.ReloadOnScriptFileChange;
		}

		private void DisableScriptsOnLoadMenuItem_Click(object sender, EventArgs e)
		{
			Config.DisableLuaScriptsOnLoad ^= true;
		}

		private void ToggleAllIfNoneSelectedMenuItem_Click(object sender, EventArgs e)
		{
			Settings.ToggleAllIfNoneSelected ^= true;
		}

		private void ReloadWhenScriptFileChangesMenuItem_Click(object sender, EventArgs e)
		{
			Settings.ReloadOnScriptFileChange ^= true;

			if (Settings.ReloadOnScriptFileChange)
			{
				AddFileWatches();
			}
			else
			{
				_watches.Clear();
			}
		}

		private void RegisterToTextEditorsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			// Hide until this one is implemented
			RegisterNotePadMenuItem.Visible = false;

			if (_luaAutoInstaller.IsInstalled(LuaAutocompleteInstaller.TextEditors.Sublime2))
			{
				if (_luaAutoInstaller.IsBizLuaRegistered(LuaAutocompleteInstaller.TextEditors.Sublime2))
				{
					RegisterSublimeText2MenuItem.Text = "Sublime Text 2 (installed)";
					RegisterSublimeText2MenuItem.Font = new Font(RegisterSublimeText2MenuItem.Font, FontStyle.Regular);
					RegisterSublimeText2MenuItem.Image = Properties.Resources.GreenCheck;
				}
				else
				{
					RegisterSublimeText2MenuItem.Text = "Sublime Text 2 (detected)";
					RegisterSublimeText2MenuItem.Font = new Font(RegisterSublimeText2MenuItem.Font, FontStyle.Italic);
					RegisterSublimeText2MenuItem.Image = null;
				}
			}
			else
			{
				RegisterSublimeText2MenuItem.Text = "Sublime Text 2";
				RegisterSublimeText2MenuItem.Font = new Font(RegisterSublimeText2MenuItem.Font, FontStyle.Regular);
				RegisterSublimeText2MenuItem.Image = null;
			}

			if (_luaAutoInstaller.IsInstalled(LuaAutocompleteInstaller.TextEditors.NotePad))
			{
				if (_luaAutoInstaller.IsBizLuaRegistered(LuaAutocompleteInstaller.TextEditors.NotePad))
				{
					RegisterNotePadMenuItem.Text = "Notepad++ (installed)";
					RegisterNotePadMenuItem.Font = new Font(RegisterNotePadMenuItem.Font, FontStyle.Regular);
					RegisterNotePadMenuItem.Image = Properties.Resources.GreenCheck;
				}
				else
				{
					RegisterNotePadMenuItem.Text = "Notepad++ (detected)";
					RegisterNotePadMenuItem.Font = new Font(RegisterNotePadMenuItem.Font, FontStyle.Italic);
					RegisterNotePadMenuItem.Image = null;
				}
			}
			else
			{
				RegisterNotePadMenuItem.Text = "Notepad++";
				RegisterNotePadMenuItem.Font = new Font(RegisterNotePadMenuItem.Font, FontStyle.Regular);
				RegisterNotePadMenuItem.Image = null;
			}
		}

		private void RegisterSublimeText2MenuItem_Click(object sender, EventArgs e)
		{
			_luaAutoInstaller.InstallBizLua(LuaAutocompleteInstaller.TextEditors.Sublime2, LuaImp.Docs);
		}

		private void RegisterNotePadMenuItem_Click(object sender, EventArgs e)
		{
			_luaAutoInstaller.InstallBizLua(LuaAutocompleteInstaller.TextEditors.NotePad, LuaImp.Docs);
		}

		private void FunctionsListMenuItem_Click(object sender, EventArgs e)
		{
			new LuaFunctionsForm(LuaImp.Docs).Show();
		}

		private void OnlineDocsMenuItem_Click(object sender, EventArgs e)
		{
			Process.Start("http://tasvideos.org/BizHawk/LuaFunctions.html");
		}

		private void ScriptListContextMenu_Opening(object sender, CancelEventArgs e)
		{
			ToggleScriptContextItem.Enabled =
				PauseScriptContextItem.Enabled =
				EditScriptContextItem.Enabled =
				SelectedFiles.Any();

			StopAllScriptsContextItem.Visible =
				ScriptContextSeparator.Visible =
				LuaImp.ScriptList.Any(file => file.Enabled);

			ClearRegisteredFunctionsContextItem.Enabled =
				LuaImp.RegisteredFunctions.Any();
		}

		private void ConsoleContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RegisteredFunctionsContextItem.Enabled = LuaImp.RegisteredFunctions.Any();
			CopyContextItem.Enabled = OutputBox.SelectedText.Any();
			ClearConsoleContextItem.Enabled = 
				SelectAllContextItem.Enabled = 
				OutputBox.Text.Any();

			ClearRegisteredFunctionsLogContextItem.Enabled = 
				LuaImp.RegisteredFunctions.Any();
		}

		private void ClearConsoleContextItem_Click(object sender, EventArgs e)
		{
			ClearOutputWindow();
		}

		private void SelectAllContextItem_Click(object sender, EventArgs e)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.SelectAll();
				OutputBox.Refresh();
			});
		}

		private void CopyContextItem_Click(object sender, EventArgs e)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.Copy();
				OutputBox.Refresh();
			});
		}

		private void ClearRegisteredFunctionsContextMenuItem_Click(object sender, EventArgs e)
		{
			LuaImp.RegisteredFunctions.Clear(Emulator);
		}

		private void LuaConsole_DragDrop(object sender, DragEventArgs e)
		{
			if (OSTailoredCode.IsUnixHost)
			{
				Console.WriteLine("The Lua environment can currently only be created on Windows, no scripts will be loaded.");
				return;
			}
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			try
			{
				foreach (var path in filePaths)
				{
					if (Path.GetExtension(path)?.ToLower() == ".lua" || Path.GetExtension(path)?.ToLower() == ".txt")
					{
						LoadLuaFile(path);
						UpdateDialog();
					}
					else if (Path.GetExtension(path)?.ToLower() == ".luases")
					{
						LoadLuaSession(path);
						return;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
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

		private void OutputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F12 && !e.Control && !e.Alt && !e.Shift) // F12
			{
				RegisteredFunctionsMenuItem_Click(null, null);
			}
		}

		/// <summary>
		/// Sorts the column Ascending on the first click and Descending on the second click.
		/// </summary>
		private void LuaListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var columnToSort = e.Column.Name;
			var luaListTemp = new List<LuaFile>();
			if (columnToSort != _lastColumnSorted)
			{
				_sortReverse = false;
			}

			// For getting the name of the .lua file, for some reason this field is kept blank in LuaFile.cs?
			// The Name variable gets emptied again near the end just in case it would break something.
			for (var i = 0; i < LuaImp.ScriptList.Count; i++)
			{
				var words = Regex.Split(LuaImp.ScriptList[i].Path, ".lua");
				var split = words[0].Split(Path.DirectorySeparatorChar);

				luaListTemp.Add(LuaImp.ScriptList[i]);
				luaListTemp[i].Name = split[split.Length - 1];
			}

			// Script, Path
			switch (columnToSort)
			{
				case "Script":
					luaListTemp = luaListTemp
						.OrderBy(lf => lf.Name, _sortReverse)
						.ThenBy(lf => lf.Path)
						.ToList();
					break;
				case "PathName":
					luaListTemp = luaListTemp
						.OrderBy(lf => lf.Path, _sortReverse)
						.ThenBy(lf => lf.Name)
						.ToList();
					break;
			}

			for (var i = 0; i < LuaImp.ScriptList.Count; i++)
			{
				LuaImp.ScriptList[i] = luaListTemp[i];
				LuaImp.ScriptList[i].Name = "";
			}

			UpdateDialog();
			_lastColumnSorted = columnToSort;
			_sortReverse = !_sortReverse;
		}

		private void RefreshScriptMenuItem_Click(object sender, EventArgs e)
		{
			ToggleScriptMenuItem_Click(sender, e);
			ToggleScriptMenuItem_Click(sender, e);
		}

		private void InputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				string consoleBeforeCall = OutputBox.Text;

				// TODO: Maybe make these try-catches more general
				if (!string.IsNullOrWhiteSpace(InputBox.Text))
				{
					if (InputBox.Text.Contains("emu.frameadvance("))
					{
						WriteLine("emu.frameadvance() can not be called from the console");
						return;
					}

					LuaSandbox.Sandbox(null, () =>
					{
						LuaImp.ExecuteString($"console.log({InputBox.Text})");
					}, () =>
					{
						LuaSandbox.Sandbox(null, () =>
						{
							LuaImp.ExecuteString(InputBox.Text);

							if (OutputBox.Text == consoleBeforeCall)
							{
								WriteLine("Command successfully executed");
							}
						});
					});

					_messageCount = 0;
					_consoleCommandHistory.Insert(0, InputBox.Text);
					_consoleCommandHistoryIndex = -1;
					InputBox.Clear();
				}
			}
			else if (e.KeyCode == Keys.Up)
			{
				if (_consoleCommandHistoryIndex < _consoleCommandHistory.Count - 1)
				{
					_consoleCommandHistoryIndex++;
					InputBox.Text = _consoleCommandHistory[_consoleCommandHistoryIndex];
					InputBox.Select(InputBox.Text.Length, 0);
				}

				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (_consoleCommandHistoryIndex == 0)
				{
					_consoleCommandHistoryIndex--;
					InputBox.Text = "";
				}
				else if (_consoleCommandHistoryIndex > 0)
				{
					_consoleCommandHistoryIndex--;
					InputBox.Text = _consoleCommandHistory[_consoleCommandHistoryIndex];
					InputBox.Select(InputBox.Text.Length, 0);
				}

				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Tab)
			{
				ProcessTabKey(false);
				e.Handled = true;
			}
		}

		// For whatever reason an auto-complete TextBox doesn't respond to delete
		// Which is annoying but worse is that it let's the key propagate
		// If a script is highlighted in the ListView, and the user presses 
		// delete, it will remove the script without this hack
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Delete && InputBox.Focused)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override bool ProcessTabKey(bool forward)
		{
			// TODO: Make me less dirty (please)
			return false;
		}

		private void EraseToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.ClearLuaSurfaces();
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}

		private void LuaListView_DoubleClick(object sender, EventArgs e)
		{
			var index = LuaListView.CurrentCell?.RowIndex;
			if (index < LuaImp.ScriptList.Count)
			{
				var file = LuaImp.ScriptList[index.Value];
				ToggleLuaScript(file);
				UpdateDialog();
			}
		}

		private void ToggleLuaScript(LuaFile file)
		{
			if (file.IsSeparator)
			{
				return;
			}

			file.Toggle();

			if (file.Enabled && file.Thread == null)
			{
				LuaImp.RegisteredFunctions.RemoveForFile(file, Emulator); // First remove any existing registered functions for this file
				EnableLuaFile(file);
				UpdateRegisteredFunctionsDialog();
			}
			else if (!file.Enabled && file.Thread != null)
			{
				LuaImp.CallExitEvent(file);
				LuaImp.RegisteredFunctions.RemoveForFile(file, Emulator);
				UpdateRegisteredFunctionsDialog();

				LuaImp.CallExitEvent(file);
				file.Stop();
				ReDraw();
			}
		}

		[RestoreDefaults]
		private void RestoreDefaults()
		{
			Settings = new LuaConsoleSettings();
			LuaListView.AllColumns.Clear();
			SetColumns();
			splitContainer1.SplitterDistance = _defaultSplitDistance;
			UpdateDialog();
		}
	}
}
