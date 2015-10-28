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
					_currentFileName = _recent.MostRecent;
				}
			}
		}

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private ICodeDataLogger CodeDataLogger { get; set; }

		private string _currentFileName = string.Empty;
		private CodeDataLog _cdl;

		public CDL()
		{
			InitializeComponent();
		}

		public void UpdateValues()
		{
			UpdateDisplay();
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
			_currentFileName = null;
			LoggingActiveCheckbox.Checked = false;
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			var lines = new List<string>();
			if (_cdl == null)
			{
				lines.Add("No CDL loaded.");
			}
			else
			{
				lines.Add("CDL contains the following domains:");
				foreach (var kvp in _cdl)
				{
					int total = 0;
					unsafe
					{
						fixed (byte* data = kvp.Value)
						{
							byte* src = data;
							byte* end = data + kvp.Value.Length;
							while (src < end)
							{
								if (*src++ != 0)
								{
									total++;
								}
							}
						}
					}

					lines.Add(string.Format("Domain {0} Size {1} Mapped {2}% ({3}/{4} bytes)", kvp.Key, kvp.Value.Length, total / (float) kvp.Value.Length * 100f, total, kvp.Value.Length));
				}
			}

			CdlTextbox.Lines = lines.ToArray();
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
				if (LoggingActiveCheckbox.Checked)
					CodeDataLogger.SetCDL(_cdl);

				_currentFileName = path;
			}

			UpdateDisplay();
		}

		#region Events

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(_currentFileName);
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

			if (LoggingActiveCheckbox.Checked)
				CodeDataLogger.SetCDL(_cdl);
			else CodeDataLogger.SetCDL(null);

			_currentFileName = null;

			UpdateDisplay();
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
				_currentFileName,
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
			if (string.IsNullOrWhiteSpace(_currentFileName))
			{
				RunSaveAs();
				return;
			}
			
			using (var fs = new FileStream(_currentFileName, FileMode.Create, FileAccess.Write))
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
					_currentFileName,
					PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
					"Code Data Logger Files",
					"cdl");

				if (file != null)
				{
					using (var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
					{
						_cdl.Save(fs);
						_recent.Add(file.FullName);
						_currentFileName = file.FullName;
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
					_currentFileName,
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
						UpdateDisplay();
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
					UpdateDisplay();
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

		#endregion

		#region Dialog Events

		private void PCECDL_Load(object sender, EventArgs e)
		{
		}

		private void LoggingActiveCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (LoggingActiveCheckbox.Checked && _cdl == null)
			{
				//implicitly create a new file
				NewFileLogic();
			}
			
			if (_cdl != null && LoggingActiveCheckbox.Checked)
				CodeDataLogger.SetCDL(_cdl);
			else
				CodeDataLogger.SetCDL(null);
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

		#endregion

		#endregion
	}
}
