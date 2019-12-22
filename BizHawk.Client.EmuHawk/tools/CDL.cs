using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;

//TODO - select which memorydomains go out to the CDL file. will this cause a problem when re-importing it? 
  //perhaps missing domains shouldnt fail a check
//OR - just add a contextmenu option to the listview item that selects it for export.
//TODO - add a contextmenu option which warps to the hexeditor with the provided domain selected for visualizing on the hex editor.
//TODO - consider setting colors for columns in CDL
//TODO - option to print domain name in caption instead of 0x01 etc.
//TODO - context menu should have copy option too

namespace BizHawk.Client.EmuHawk
{
	public partial class CDL : ToolFormBase, IToolFormAutoConfig
	{
		private RecentFiles _recent_fld = new RecentFiles();

		[ConfigPersist]
		private RecentFiles _recent
		{
			get => _recent_fld;
			set => _recent_fld = value;
		}

		void SetCurrentFilename(string fname)
		{
			_currentFilename = fname;
			Text = _currentFilename == null
				? "Code Data Logger"
				: $"Code Data Logger - {fname}";
		}

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private ICodeDataLogger CodeDataLogger { get; set; }

		private string _currentFilename = null;
		private CodeDataLog _cdl;

		public CDL()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();

			tsbViewStyle.SelectedIndex = 0;

			lvCDL.AllColumns.Clear();
			lvCDL.AllColumns.AddRange(new []
			{
				new RollColumn { Name = "CDLFile", Text = "CDL File @", Width = 107, Type = ColumnType.Text  },
				new RollColumn { Name = "Domain", Text = "Domain", Width = 126, Type = ColumnType.Text  },
				new RollColumn { Name = "Percent", Text = "%", Width = 58, Type = ColumnType.Text  },
				new RollColumn { Name = "Mapped", Text = "Mapped", Width = 64, Type = ColumnType.Text  },
				new RollColumn { Name = "Size", Text = "Size", Width = 112, Type = ColumnType.Text  },
				new RollColumn { Name = "0x01", Text = "0x01", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x02", Text = "0x02", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x04", Text = "0x04", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x08", Text = "0x08", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x10", Text = "0x10", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x20", Text = "0x20", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x40", Text = "0x40", Width = 56, Type = ColumnType.Text  },
				new RollColumn { Name = "0x80", Text = "0x80", Width = 56, Type = ColumnType.Text  }
			});
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			UpdateDisplay(false);
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			//don't try to recover the current CDL!
			//even though it seems like it might be nice, it might get mixed up between games. even if we use CheckCDL. Switching games with the same memory map will be bad.
			_cdl = null;
			SetCurrentFilename(null);
			SetLoggingActiveCheck(false);
			UpdateDisplay(true);
		}

		void SetLoggingActiveCheck(bool value)
		{
			tsbLoggingActive.Checked = value;
		}

		string[][] listContents = new string[0][];

		private void UpdateDisplay(bool force)
		{
			if (!tsbViewUpdate.Checked && !force)
				return;


			if (_cdl == null)
			{
				lvCDL.DeselectAll();
				return;
			}

			listContents = new string[_cdl.Count][];

			int idx = 0;
			foreach (var kvp in _cdl)
			{
				int[] totals = new int[8];
				int total = 0;
				unsafe
				{
					int* map = stackalloc int[256];
					for (int i = 0; i < 256; i++)
						map[i] = 0;

					fixed (byte* data = kvp.Value)
					{
						byte* src = data;
						byte* end = data + kvp.Value.Length;
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
				}

				var bm = _cdl.GetBlockMap();
				long addr = bm[kvp.Key];

				var lvi = listContents[idx++] = new string[13];
				lvi[0] = $"{addr:X8}";
				lvi[1] = kvp.Key;
				lvi[2] = $"{total / (float)kvp.Value.Length * 100f:0.00}%";
				if (tsbViewStyle.SelectedIndex == 2)
					lvi[3] = $"{total / 1024.0f:0.00}";
				else
					lvi[3] = $"{total}";
				if (tsbViewStyle.SelectedIndex == 2)
				{
					int n = (int)(kvp.Value.Length / 1024.0f);
					float ncheck = kvp.Value.Length / 1024.0f;
					lvi[4] = $"of {(n == ncheck ? "" : "~")}{n} KBytes";
				}
				else
					lvi[4] = $"of {kvp.Value.Length} Bytes";
				for (int i = 0; i < 8; i++)
				{
					if (tsbViewStyle.SelectedIndex == 0)
						lvi[5 + i] = $"{totals[i] / (float)kvp.Value.Length * 100f:0.00}%";
					if (tsbViewStyle.SelectedIndex == 1)
						lvi[5 + i] = $"{totals[i]}";
					if (tsbViewStyle.SelectedIndex == 2)
						lvi[5 + i] = $"{totals[i] / 1024.0f:0.00}";
				}

			}
			lvCDL.RowCount = _cdl.Count;
		}

		public bool AskSaveChanges()
		{
			// nothing to fear:
			if (_cdl == null)
				return true;

			// try auto-saving if appropriate
			if (Config.CDLAutoSave)
			{
				if (_currentFilename != null)
				{
					RunSave();
					ShutdownCDL();
					return true;
				}
			}

			// TODO - I don't like this system. It's hard to figure out how to use it. It should be done in multiple passes.
			var result = MessageBox.Show("Save changes to CDL session?", "CDL Auto Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.No)
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

		public bool UpdateBefore => false;

		bool autoloading = false;
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
					if(!autoloading)
						MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
					return;
				}

				//ok, it's all good:
				_cdl = newCDL;
				CodeDataLogger.SetCDL(null);
				if (tsbLoggingActive.Checked || Config.CDLAutoStart)
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

			miAutoSave.Checked = Config.CDLAutoSave;
			miAutoStart.Checked = Config.CDLAutoStart;
			miAutoResume.Checked = Config.CDLAutoResume;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(_recent.RecentMenu(LoadFile, true));
		}

		void NewFileLogic()
		{
			_cdl = new CodeDataLog();
			CodeDataLogger.NewCDL(_cdl);

			if (tsbLoggingActive.Checked || Config.CDLAutoStart)
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
				var result = MessageBox.Show(this, "OK to create new CDL?", "Query", MessageBoxButtons.YesNo);
				if (result != DialogResult.Yes)
					return;
			}

			NewFileLogic();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
				_currentFilename,
				PathManager.MakeAbsolutePath(Config.PathEntries.LogPathFragment, null),
				"Code Data Logger Files",
				"cdl");

			if (file == null)
				return;

			//take care not to clobber an existing CDL
			if (_cdl != null)
			{
				var result = MessageBox.Show(this, "OK to load new CDL?", "Query", MessageBoxButtons.YesNo);
				if (result != DialogResult.Yes)
					return;
			}

			LoadFile(file.FullName);
		}

		void RunSave()
		{
			_recent.Add(_currentFilename);
			using var fs = new FileStream(_currentFilename, FileMode.Create, FileAccess.Write);
			_cdl.Save(fs);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				MessageBox.Show(this, "Cannot save with no CDL loaded!", "Alert");
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
		bool RunSaveAs()
		{
			var file = SaveFileDialog(
				_currentFilename,
				PathManager.MakeAbsolutePath(Config.PathEntries.LogPathFragment, null),
				"Code Data Logger Files",
				"cdl");

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
				MessageBox.Show(this, "Cannot append with no CDL loaded!", "Alert");
			}
			else
			{
				var file = ToolFormBase.OpenFileDialog(
					_currentFilename,
					PathManager.MakeAbsolutePath(Config.PathEntries.LogPathFragment, null),
					"Code Data Logger Files",
					"cdl");

				if (file != null)
				{
					using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
					var newCDL = new CodeDataLog();
					newCDL.Load(fs);
					if (!_cdl.Check(newCDL))
					{
						MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
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
				MessageBox.Show(this, "Cannot clear with no CDL loaded!", "Alert");
			}
			else
			{
				var result = MessageBox.Show(this, "OK to clear CDL?", "Query", MessageBoxButtons.YesNo);
				if (result == DialogResult.Yes)
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
				MessageBox.Show(this, "Cannot disassemble with no CDL loaded!", "Alert");
				return;
			}

			using var sfd = new SaveFileDialog();
			var result = sfd.ShowDialog(this);
			if (result == DialogResult.OK)
			{
				using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write);
				CodeDataLogger.DisassembleCDL(fs, _cdl);
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			ShutdownCDL();
			Close();
		}

		void ShutdownCDL()
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
			if (Config.CDLAutoStart)
			{
				if (_cdl == null)
					NewFileLogic();
			}
			base.OnShown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			CodeDataLogger?.SetCDL(null);
		}

		private void CDL_Load(object sender, EventArgs e)
		{
			if (Config.CDLAutoResume)
			{
				try
				{
					autoloading = true;
					var autoresume_file = $"{PathManager.FilesystemSafeName(Global.Game)}.cdl";
					var autoresume_dir = PathManager.MakeAbsolutePath(Config.PathEntries.LogPathFragment, null);
					var autoresume_path = Path.Combine(autoresume_dir, autoresume_file);
					if (File.Exists(autoresume_path))
						LoadFile(autoresume_path);
				}
				finally
				{
					autoloading = false;
				}
			}

			if (_recent_fld.AutoLoad && !_recent_fld.Empty)
			{
				if (File.Exists(_recent.MostRecent))
				{
					try
					{
						autoloading = true;
						LoadFile(_recent.MostRecent);
					}
					finally
					{
						autoloading = false;
					}
					SetCurrentFilename(_recent.MostRecent);
				}
			}
		}

		private void CDL_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void CDL_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".cdl")
			{
				LoadFile(filePaths[0]);
			}
		}

	

		private void tsbViewStyle_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateDisplay(true);
		}

		private void tsbLoggingActive_CheckedChanged(object sender, EventArgs e)
		{
			if (tsbLoggingActive.Checked && _cdl == null)
			{
				//implicitly create a new file
				NewFileLogic();
			}

			if (_cdl != null && tsbLoggingActive.Checked)
				CodeDataLogger.SetCDL(_cdl);
			else
				CodeDataLogger.SetCDL(null);
		}

		private void lvCDL_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			var subItem = lvCDL.AllColumns.IndexOf(column);
			text = listContents[index][subItem];
		}

		private void tsbExportText_Click(object sender, EventArgs e)
		{
			using var sw = new StringWriter();
			foreach(var line in listContents)
			{
				foreach (var entry in line)
					sw.Write("{0} |", entry);
				sw.WriteLine();
			}
			Clipboard.SetText(sw.ToString());
		}

		private void miAutoSave_Click(object sender, EventArgs e)
		{
			Config.CDLAutoSave ^= true;
		}

		private void miAutoStart_Click(object sender, EventArgs e)
		{
			Config.CDLAutoStart ^= true;
		}

		private void miAutoResume_Click(object sender, EventArgs e)
		{
			Config.CDLAutoResume ^= true;
		}
	}
}
