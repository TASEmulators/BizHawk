using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Components.H6280;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Consoles.Sega;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

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
	public partial class CDL : Form, IToolFormAutoConfig
	{
		private RecentFiles _recent_fld = new RecentFiles();

		[ConfigPersist]
		private RecentFiles _recent
		{
			get
			{ return _recent_fld; }
			set
			{
				_recent_fld = value;
				if (_recent_fld.AutoLoad)
				{
					LoadFile(_recent.MostRecent);
					SetCurrentFilename(_recent.MostRecent);
				}
			}
		}

		void SetCurrentFilename(string fname)
		{
			_currentFilename = fname;
			if (_currentFilename == null)
				Text = "Code Data Logger";
			else Text = string.Format("Code Data Logger - {0}", fname);
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
		}

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
				lvCDL.BeginUpdate();
				lvCDL.Items.Clear();
				lvCDL.EndUpdate();
				return;
			}

			lvCDL.BeginUpdate();

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
				lvi[0] = string.Format("{0:X8}", addr);
				lvi[1] = kvp.Key;
				lvi[2] = string.Format("{0:0.00}%", total / (float)kvp.Value.Length * 100f);
				if (tsbViewStyle.SelectedIndex == 2)
					lvi[3] = string.Format("{0:0.00}", total / 1024.0f);
				else
					lvi[3] = string.Format("{0}", total);
				if (tsbViewStyle.SelectedIndex == 2)
				{
					int n = (int)(kvp.Value.Length / 1024.0f);
					float ncheck = kvp.Value.Length / 1024.0f;
					lvi[4] = string.Format("of {0}{1} KBytes", n == ncheck ? "" : "~", n);
				}
				else
					lvi[4] = string.Format("of {0} Bytes", kvp.Value.Length);
				for (int i = 0; i < 8; i++)
				{
				  if (tsbViewStyle.SelectedIndex == 0)
				    lvi[5 + i] = string.Format("{0:0.00}%", totals[i] / (float)kvp.Value.Length * 100f);
				  if (tsbViewStyle.SelectedIndex == 1)
				    lvi[5 + i] = string.Format("{0}", totals[i]);
				  if (tsbViewStyle.SelectedIndex == 2)
				    lvi[5 + i] = string.Format("{0:0.00}", totals[i] / 1024.0f);
				}

			}
			lvCDL.VirtualListSize = _cdl.Count;
			lvCDL.EndUpdate();
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

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
					MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
					return;
				}

				//ok, it's all good:
				_cdl = newCDL;
				CodeDataLogger.SetCDL(null);
				if (tsbLoggingActive.Checked)
					CodeDataLogger.SetCDL(_cdl);

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

			if (tsbLoggingActive.Checked)
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
			var file = ToolHelpers.OpenFileDialog(
				_currentFilename,
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
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

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_currentFilename))
			{
				RunSaveAs();
				return;
			}
			
			using (var fs = new FileStream(_currentFilename, FileMode.Create, FileAccess.Write))
			{
				_cdl.Save(fs);
			}
		}

		void RunSaveAs()
		{
			if (_cdl == null)
			{
				MessageBox.Show(this, "Cannot save with no CDL loaded!", "Alert");
			}
			else
			{
				var file = ToolHelpers.SaveFileDialog(
					_currentFilename,
					PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
					"Code Data Logger Files",
					"cdl");

				if (file != null)
				{
					using (var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
					{
						_cdl.Save(fs);
						_recent.Add(file.FullName);
						SetCurrentFilename(file.FullName);
					}
				}
			}
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
				var file = ToolHelpers.OpenFileDialog(
					_currentFilename,
					PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
					"Code Data Logger Files",
					"cdl");

				if (file != null)
				{
					using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
					{
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

			var sfd = new SaveFileDialog();
			var result = sfd.ShowDialog(this);
			if (result == DialogResult.OK)
			{
				using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
				{
					CodeDataLogger.DisassembleCDL(fs, _cdl);
				}
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			//deactivate logger
			if (CodeDataLogger != null) //just in case...
				CodeDataLogger.SetCDL(null);
		}

		private void PCECDL_Load(object sender, EventArgs e)
		{
		}

		private void PCECDL_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void PCECDL_DragDrop(object sender, DragEventArgs e)
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

		private void lvCDL_QueryItemText(int item, int subItem, out string text)
		{
			text = listContents[item][subItem];
		}

		private void tsbExportText_Click(object sender, EventArgs e)
		{
			StringWriter sw = new StringWriter();
			foreach(var line in listContents)
			{
				foreach (var entry in line)
					sw.Write("{0} |", entry);
				sw.WriteLine();
			}
			Clipboard.SetText(sw.ToString());
		}
	}
}
