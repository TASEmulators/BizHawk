using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Components.H6280;
using BizHawk.Emulation.Cores.PCEngine;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCECDL : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private PCEngine _emu { get; set; }
		private CodeDataLog _cdl;

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

		private string _currentFileName = string.Empty;

		public PCECDL()
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
			LoggingActiveCheckbox.Checked = _emu.Cpu.CDLLoggingActive;
			_cdl = _emu.Cpu.CDL;
			_emu.InitCDLMappings();
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

					lines.Add(string.Format("Domain {0} Size {1} Mapped {2}%", kvp.Key, kvp.Value.Length, total / (float) kvp.Value.Length * 100f));
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
				var newCDL = CodeDataLog.Load(fs);
				if (!newCDL.CheckConsistency(_emu.Cpu.Mappings))
				{
					MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
				}
				else
				{
					_cdl = newCDL;
					_emu.Cpu.CDL = _cdl;
					UpdateDisplay();
				}
			}
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

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			var result = MessageBox.Show(this, "OK to create new CDL?", "Query", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				_cdl = CodeDataLog.Create(_emu.Cpu.Mappings);
				_emu.Cpu.CDL = _cdl;
				UpdateDisplay();
			}
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var result = MessageBox.Show(this, "OK to load new CDL?", "Query", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
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
						var newCDL = CodeDataLog.Load(fs);
						if (!newCDL.CheckConsistency(_emu.Cpu.Mappings))
						{
							MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
						}
						else
						{
							_cdl = newCDL;
							_emu.Cpu.CDL = _cdl;
							UpdateDisplay();
							_recent.Add(file.FullName);
							_currentFileName = file.FullName;
						}
					}
				}
			}
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_currentFileName))
			{
				using (var fs = new FileStream(_currentFileName, FileMode.Create, FileAccess.Write))
				{
					_cdl.Save(fs);
				}
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
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
						var newCDL = CodeDataLog.Load(fs);
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
			}
			else
			{
				var sfd = HawkDialogFactory.CreateSaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
					{
						_cdl.Disassemble(fs, MemoryDomains);
					}
				}
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
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
				MessageBox.Show(this, "Cannot log with no CDL loaded!", "Alert");
				LoggingActiveCheckbox.Checked = false;
			}

			_emu.Cpu.CDLLoggingActive = LoggingActiveCheckbox.Checked;
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
