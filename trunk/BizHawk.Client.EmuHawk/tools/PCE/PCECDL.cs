using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Components.H6280;

using System.IO;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCECDL : Form, IToolForm
	{
		PCEngine emu;
		CodeDataLog CDL;
		
		public PCECDL()
		{
			InitializeComponent();
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
				emu = (PCEngine)Global.Emulator;
				checkBox1.Checked = emu.Cpu.CDLLoggingActive;
				CDL = emu.Cpu.CDL;
				emu.InitCDLMappings();
				UpdateDisplay();
			}
			else
			{
				emu = null;
				Close();
			}
		}

		void UpdateDisplay()
		{
			List<string> Lines = new List<string>();
			if (CDL == null)
			{
				Lines.Add("No CDL loaded.");
			}
			else
			{
				Lines.Add("CDL contains the following domains:");
				foreach (var kvp in CDL)
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
									total++;
							}
						}
					}
					Lines.Add(string.Format("Domain {0} Size {1} Mapped {2}%", kvp.Key, kvp.Value.Length, total / (float) kvp.Value.Length * 100f));
				}
			}
			textBox1.Lines = Lines.ToArray();
		}

		public bool AskSave()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = MessageBox.Show(this, "OK to create new CDL?", "Query", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				CDL = CodeDataLog.Create(emu.Cpu.Mappings);
				emu.Cpu.CDL = CDL;
				UpdateDisplay();
			}
		}

		private void loadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = MessageBox.Show(this, "OK to load new CDL?", "Query", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				var ofd = new OpenFileDialog();
				result = ofd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
					{
						var newCDL = CodeDataLog.Load(fs);
						if (!newCDL.CheckConsistency(emu.Cpu.Mappings))
						{
							MessageBox.Show(this, "CDL file does not match emulator's current memory map!");
						}
						else
						{
							CDL = newCDL;
							emu.Cpu.CDL = CDL;
							UpdateDisplay();
						}
					}
				}
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CDL == null)
			{
				MessageBox.Show(this, "Cannot save with no CDL loaded!", "Alert");
			}
			else
			{
				var sfd = new SaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
					{
						CDL.Save(fs);
					}
				}
			}
		}

		private void unionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CDL == null)
			{
				MessageBox.Show(this, "Cannot union with no CDL loaded!", "Alert");
			}
			else
			{
				var ofd = new OpenFileDialog();
				var result = ofd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
					{
						var newCDL = CodeDataLog.Load(fs);
						CDL.LogicalOrFrom(newCDL);
						UpdateDisplay();
					}
				}
			}
		}

		private void PCECDL_FormClosing(object sender, FormClosingEventArgs e)
		{

		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CDL == null)
			{
				MessageBox.Show(this, "Cannot clear with no CDL loaded!", "Alert");
			}
			else
			{
				var result = MessageBox.Show(this, "OK to clear CDL?", "Query", MessageBoxButtons.YesNo);
				if (result == DialogResult.Yes)
				{
					CDL.ClearData();
					UpdateDisplay();
				}
			}
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox1.Checked && CDL == null)
			{
				MessageBox.Show(this, "Cannot log with no CDL loaded!", "Alert");
				checkBox1.Checked = false;
			}
			emu.Cpu.CDLLoggingActive = checkBox1.Checked;
		}

		private void disassembleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CDL == null)
			{
				MessageBox.Show(this, "Cannot disassemble with no CDL loaded!", "Alert");
			}
			else
			{
				var sfd = new SaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
					{
						CDL.Disassemble(fs, Global.Emulator.MemoryDomains);
					}
				}
			}
		}
	}
}
