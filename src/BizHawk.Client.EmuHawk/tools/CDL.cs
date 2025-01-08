using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;

// TODO - select which memorydomains go out to the CDL file. will this cause a problem when re-importing it?
// perhaps missing domains shouldn't fail a check
// OR - just add a contextmenu option to the ListView item that selects it for export.
// TODO - add a contextmenu option which warps to the HexEditor with the provided domain selected for visualizing on the hex editor.
// TODO - consider setting colors for columns in CDL
// TODO - option to print domain name in caption instead of 0x01 etc.
// TODO - context menu should have copy option too
namespace BizHawk.Client.EmuHawk
{
	public partial class CDL : ToolFormBase, IToolFormAutoConfig
	{
		private static readonly FilesystemFilterSet CDLFilesFSFilterSet = new(new FilesystemFilter("Code Data Logger Files", new[] { "cdl" }));

		public static Icon ToolIcon
			=> Resources.CdLoggerIcon;

		private RecentFiles _recentFld = new RecentFiles();

		[ConfigPersist]
		private RecentFiles _recent
		{
			get => _recentFld;
			set => _recentFld = value;
		}

		[ConfigPersist]
		private bool CDLAutoSave { get; set; } = true;

		[ConfigPersist]
		private bool CDLAutoStart { get; set; } = true;
		
		[ConfigPersist]
		private bool CDLAutoResume { get; set; } = true;

		private void SetCurrentFilename(string fname)
		{
			_currentFilename = fname;
			_windowTitle = _currentFilename == null
				? WindowTitleStatic
				: $"{WindowTitleStatic} - {fname}";
			UpdateWindowTitle();
		}

		[RequiredService]
		public ICodeDataLogger/*?*/ _cdlCore { get; set; }

		private ICodeDataLogger CodeDataLogger
			=> _cdlCore!;

		private string _currentFilename;
		private CodeDataLog _cdl;

		private string _windowTitle = "Code Data Logger";

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => "Code Data Logger";

		public CDL()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			InitializeComponent();
			NewMenuItem.Image = Resources.NewFile;
			OpenMenuItem.Image = Resources.OpenFile;
			SaveMenuItem.Image = Resources.SaveAs;
			RecentSubMenu.Image = Resources.Recent;
			tsbLoggingActive.Image = Resources.Placeholder;
			tsbViewUpdate.Image = Resources.Placeholder;
			tsbExportText.Image = Resources.LoadConfig;
			Icon = ToolIcon;

			tsbViewStyle.SelectedIndex = 0;

			lvCDL.AllColumns.Clear();
			lvCDL.AllColumns.AddRange(new RollColumn[]
			{
				new(name: "CDLFile", widthUnscaled: 107, text: "CDL File @"),
				new(name: "Domain", widthUnscaled: 126, text: "Domain"),
				new(name: "Percent", widthUnscaled: 58, text: "%"),
				new(name: "Mapped", widthUnscaled: 64, text: "Mapped"),
				new(name: "Size", widthUnscaled: 112, text: "Size"),
				new(name: "0x01", widthUnscaled: 56, text: "0x01"),
				new(name: "0x02", widthUnscaled: 56, text: "0x02"),
				new(name: "0x04", widthUnscaled: 56, text: "0x04"),
				new(name: "0x08", widthUnscaled: 56, text: "0x08"),
				new(name: "0x10", widthUnscaled: 56, text: "0x10"),
				new(name: "0x20", widthUnscaled: 56, text: "0x20"),
				new(name: "0x40", widthUnscaled: 56, text: "0x40"),
				new(name: "0x80", widthUnscaled: 56, text: "0x80"),
			});
		}

		protected override void UpdateAfter() => UpdateDisplay(false);

		public override void Restart()
		{
			//don't try to recover the current CDL!
			//even though it seems like it might be nice, it might get mixed up between games. even if we use CheckCDL. Switching games with the same memory map will be bad.
			_cdl = null;
			SetCurrentFilename(null);
			SetLoggingActiveCheck(false);
			UpdateDisplay(true);
		}

		private void SetLoggingActiveCheck(bool value)
		{
			tsbLoggingActive.Checked = value;
		}

		private string[][] _listContents = Array.Empty<string[]>();

		private unsafe void UpdateDisplay(bool force)
		{
			if (!tsbViewUpdate.Checked && !force)
				return;

			int* map = stackalloc int[256];

			if (_cdl == null)
			{
				lvCDL.DeselectAll();
				return;
			}

			_listContents = new string[_cdl.Count][];

			int idx = 0;
			foreach (var (scope, dataA) in _cdl)
			{
				int[] totals = new int[8];
				int total = 0;
				
				for (int i = 0; i < 256; i++)
					map[i] = 0;

				fixed (byte* data = dataA)
				{
					byte* src = data;
					byte* end = data + dataA.Length;
					while (src < end)
					{
						byte s = *src++;
						map[s]++;
					}
				}

				for (int i = 0; i < 256; i++)
				{
					if(i!=0) total += map[i];
					if ((i & 0x01) != 0) totals[0] += map[i];
					if ((i & 0x02) != 0) totals[1] += map[i];
					if ((i & 0x04) != 0) totals[2] += map[i];
					if ((i & 0x08) != 0) totals[3] += map[i];
					if ((i & 0x10) != 0) totals[4] += map[i];
					if ((i & 0x20) != 0) totals[5] += map[i];
					if ((i & 0x40) != 0) totals[6] += map[i];
					if ((i & 0x80) != 0) totals[7] += map[i];
				}

				var bm = _cdl.GetBlockMap();
				long addr = bm[scope];

				var lvi = _listContents[idx++] = new string[13];
				lvi[0] = $"{addr:X8}";
				lvi[1] = scope;
				lvi[2] = $"{total / (float) dataA.Length:P2}";
				if (tsbViewStyle.SelectedIndex == 2)
					lvi[3] = $"{total / 1024.0f:0.00}";
				else
					lvi[3] = $"{total}";
				if (tsbViewStyle.SelectedIndex == 2)
				{
					lvi[4] = $"of {(dataA.Length % 1024 == 0 ? "" : "~")}{dataA.Length / 1024} KBytes";
				}
				else
					lvi[4] = $"of {dataA.Length} Bytes";
				for (int i = 0; i < 8; i++)
				{
					if (tsbViewStyle.SelectedIndex == 0)
						lvi[5 + i] = $"{totals[i] / (float) dataA.Length:P2}";
					if (tsbViewStyle.SelectedIndex == 1)
						lvi[5 + i] = $"{totals[i]}";
					if (tsbViewStyle.SelectedIndex == 2)
						lvi[5 + i] = $"{totals[i] / 1024.0f:0.00}";
				}
			}

			lvCDL.RowCount = _cdl.Count;
		}

		public override bool AskSaveChanges()
		{
			// nothing to fear:
			if (_cdl == null)
				return true;

			// try auto-saving if appropriate
			if (CDLAutoSave)
			{
				if (_currentFilename != null)
				{
					RunSave();
					ShutdownCDL();
					return true;
				}
			}

			// TODO - I don't like this system. It's hard to figure out how to use it. It should be done in multiple passes.
			var result = DialogController.ShowMessageBox2("Save changes to CDL session?", "CDL Auto Save", EMsgBoxIcon.Question);
			if (!result)
			{
				ShutdownCDL();
				return true;
			}

			if (string.IsNullOrWhiteSpace(_currentFilename))
			{
				if (RunSaveAs())
				{
					ShutdownCDL();
					return true;
				}
				
				ShutdownCDL();
				return false;
			}

			RunSave();
			ShutdownCDL();
			return true;
		}

		private bool _autoloading;
		public void LoadFile(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				var newCDL = new CodeDataLog();
				newCDL.Load(fs);

				//have the core create a CodeDataLog to check mapping information against
				var testCDL = new CodeDataLog();
				CodeDataLogger.NewCDL(testCDL);
				if (!newCDL.Check(testCDL))
				{
					if(!_autoloading)
						this.ModalMessageBox("CDL file does not match emulator's current memory map!");
					return;
				}

				//ok, it's all good:
				_cdl = newCDL;
				CodeDataLogger.SetCDL(null);
				if (tsbLoggingActive.Checked || CDLAutoStart)
				{
					tsbLoggingActive.Checked = true;
					CodeDataLogger.SetCDL(_cdl);
				}

				SetCurrentFilename(path);
			}

			UpdateDisplay(true);
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = _currentFilename != null;
			SaveAsMenuItem.Enabled =
				AppendMenuItem.Enabled =
				ClearMenuItem.Enabled =
				DisassembleMenuItem.Enabled =
				_cdl != null;

			miAutoSave.Checked = CDLAutoSave;
			miAutoStart.Checked = CDLAutoStart;
			miAutoResume.Checked = CDLAutoResume;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentSubMenu.ReplaceDropDownItems(_recent.RecentMenu(this, LoadFile, "Session"));

		private void NewFileLogic()
		{
			_cdl = new CodeDataLog();
			CodeDataLogger.NewCDL(_cdl);

			if (tsbLoggingActive.Checked || CDLAutoStart)
				CodeDataLogger.SetCDL(_cdl);
			else CodeDataLogger.SetCDL(null);

			SetCurrentFilename(null);

			UpdateDisplay(true);
		}

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			//take care not to clobber an existing CDL
			if (_cdl != null)
			{
				var result = this.ModalMessageBox2("OK to create new CDL?", "Query");
				if (!result) return;
			}

			NewFileLogic();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
				currentFile: _currentFilename,
				path: Config!.PathEntries.LogAbsolutePath(),
				CDLFilesFSFilterSet);

			if (file == null)
				return;

			//take care not to clobber an existing CDL
			if (_cdl != null)
			{
				var result = this.ModalMessageBox2("OK to load new CDL?", "Query");
				if (!result) return;
			}

			LoadFile(file.FullName);
		}

		private void RunSave()
		{
			_recent.Add(_currentFilename);
			using var fs = new FileStream(_currentFilename, FileMode.Create, FileAccess.Write);
			_cdl.Save(fs);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				this.ModalMessageBox("Cannot save with no CDL loaded!", "Alert");
				return;
			}

			if (string.IsNullOrWhiteSpace(_currentFilename))
			{
				RunSaveAs();
				return;
			}

			RunSave();
		}

		/// <summary>
		/// returns false if the operation was canceled
		/// </summary>
		private bool RunSaveAs()
		{
			var fileName = _currentFilename;
			if (string.IsNullOrWhiteSpace(fileName))
			{
				fileName = Game.FilesystemSafeName();
			}

			var file = SaveFileDialog(
				currentFile: fileName,
				path: Config!.PathEntries.LogAbsolutePath(),
				CDLFilesFSFilterSet,
				this);

			if (file == null)
				return false;
				
			SetCurrentFilename(file.FullName);
			RunSave();
			return true;
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			RunSaveAs();
		}

		private void AppendMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				this.ModalMessageBox("Cannot append with no CDL loaded!", "Alert");
			}
			else
			{
				var file = OpenFileDialog(
					currentFile: _currentFilename,
					path: Config!.PathEntries.LogAbsolutePath(),
					CDLFilesFSFilterSet);

				if (file != null)
				{
					using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
					var newCDL = new CodeDataLog();
					newCDL.Load(fs);
					if (!_cdl.Check(newCDL))
					{
						this.ModalMessageBox("CDL file does not match emulator's current memory map!");
						return;
					}
					_cdl.LogicalOrFrom(newCDL);
					UpdateDisplay(true);
				}
			}
		}

		private void ClearMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				this.ModalMessageBox("Cannot clear with no CDL loaded!", "Alert");
			}
			else
			{
				var result = this.ModalMessageBox2("OK to clear CDL?", "Query");
				if (result)
				{
					_cdl.ClearData();
					UpdateDisplay(true);
				}
			}
		}

		private void DisassembleMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				this.ModalMessageBox("Cannot disassemble with no CDL loaded!", "Alert");
				return;
			}
			var result = this.ShowFileSaveDialog(initDir: Config!.PathEntries.ToolsAbsolutePath());
			if (result is not null)
			{
				using var fs = new FileStream(result, FileMode.Create, FileAccess.Write);
				CodeDataLogger.DisassembleCDL(fs, _cdl);
			}
		}

		private void ShutdownCDL()
		{
			_cdl = null;
			CodeDataLogger.SetCDL(null);
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (!AskSaveChanges())
				e.Cancel = true;
			base.OnClosing(e);
		}

		protected override void OnShown(EventArgs e)
		{
			if (CDLAutoStart)
			{
				if (_cdl == null)
					NewFileLogic();
			}
			base.OnShown(e);
		}

		protected override void OnClosed(EventArgs e)
			=> CodeDataLogger.SetCDL(null);

		private void CDL_Load(object sender, EventArgs e)
		{
			Closing += (o, e) => ShutdownCDL();
			if (CDLAutoResume)
			{
				try
				{
					_autoloading = true;
					var autoResumeFile = $"{Game.FilesystemSafeName()}.cdl";
					var autoResumeDir = Config.PathEntries.LogAbsolutePath();
					var autoResumePath = Path.Combine(autoResumeDir, autoResumeFile);
					if (File.Exists(autoResumePath))
					{
						LoadFile(autoResumePath);
					}
				}
				finally
				{
					_autoloading = false;
				}
			}

			if (_recentFld.AutoLoad && !_recentFld.Empty)
			{
				if (File.Exists(_recent.MostRecent))
				{
					try
					{
						_autoloading = true;
						LoadFile(_recent.MostRecent);
					}
					finally
					{
						_autoloading = false;
					}
					SetCurrentFilename(_recent.MostRecent);
				}
			}
		}

		private void CDL_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void CDL_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".cdl")
			{
				LoadFile(filePaths[0]);
			}
		}

		private void TsbViewStyle_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateDisplay(true);
		}

		private void TsbLoggingActive_CheckedChanged(object sender, EventArgs e)
		{
			if (tsbLoggingActive.Checked && _cdl == null)
			{
				// implicitly create a new file
				NewFileLogic();
			}

			if (_cdl != null && tsbLoggingActive.Checked)
				CodeDataLogger.SetCDL(_cdl);
			else
				CodeDataLogger.SetCDL(null);
		}

		private void LvCDL_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			var subItem = lvCDL.AllColumns.IndexOf(column);
			text = _listContents[index][subItem];
		}

		private void TsbExportText_Click(object sender, EventArgs e)
		{
			using var sw = new StringWriter();
			foreach(var line in _listContents)
			{
				foreach (var entry in line)
					sw.Write("{0} |", entry);
				sw.WriteLine();
			}
			Clipboard.SetText(sw.ToString());
		}

		private void MiAutoSave_Click(object sender, EventArgs e)
			=> CDLAutoSave = !CDLAutoSave;

		private void MiAutoStart_Click(object sender, EventArgs e)
			=> CDLAutoStart = !CDLAutoStart;

		private void MiAutoResume_Click(object sender, EventArgs e)
			=> CDLAutoResume = !CDLAutoResume;
	}
}
