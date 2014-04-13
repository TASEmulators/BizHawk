using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Components.H6280;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCECDL : Form, IToolForm
	{
		// TODO
		// Loading doesn't work
		// Save
		// Save Window position and size
		// Restore settings
		private PCEngine _emu;
		private CodeDataLog _cdl;
		
		public PCECDL()
		{
			InitializeComponent();
			TopMost = Global.Config.PceCdlSettings.TopMost;
			Restart();
		}

		public void UpdateValues()
		{
			UpdateDisplay();
		}

		public void Restart()
		{
			if (Global.Emulator is PCEngine)
			{
				_emu = (PCEngine)Global.Emulator;
				LoggingActiveCheckbox.Checked = _emu.Cpu.CDLLoggingActive;
				_cdl = _emu.Cpu.CDL;
				_emu.InitCDLMappings();
				UpdateDisplay();
			}
			else
			{
				_emu = null;
				Close();
			}
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

		public bool AskSave()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		private void LoadFileFromRecent(string path)
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

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentPceCdlFiles, LoadFileFromRecent));
			RecentSubMenu.DropDownItems.Add(
				ToolHelpers.GenerateAutoLoadItem(Global.Config.RecentPceCdlFiles));
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
				var ofd = new OpenFileDialog();
				result = ofd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
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
							Global.Config.RecentPceCdlFiles.Add(ofd.FileName);
						}
					}
				}
			}
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			// TODO
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				MessageBox.Show(this, "Cannot save with no CDL loaded!", "Alert");
			}
			else
			{
				var sfd = new SaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
					{
						_cdl.Save(fs);
						Global.Config.RecentPceCdlFiles.Add(sfd.FileName);
					}
				}
			}
		}

		private void AppendMenuItem_Click(object sender, EventArgs e)
		{
			if (_cdl == null)
			{
				MessageBox.Show(this, "Cannot union with no CDL loaded!", "Alert");
			}
			else
			{
				var ofd = new OpenFileDialog();
				var result = ofd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
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
				var sfd = new SaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
					{
						_cdl.Disassemble(fs, Global.Emulator.MemoryDomains);
					}
				}
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.PceCdlSettings.SaveWindowPosition;
			TopMost = AlwaysOnTopMenuItem.Checked = Global.Config.PceCdlSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.PceCdlSettings.FloatingWindow;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceCdlSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceCdlSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceCdlSettings.FloatingWindow ^= true;
		}

		private void PCECDL_Load(object sender, EventArgs e)
		{
			if (Global.Config.RecentPceCdlFiles.AutoLoad)
			{
				LoadFileFromRecent(Global.Config.RecentPceCdlFiles.MostRecent);
			}
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

		#endregion
	}
}
