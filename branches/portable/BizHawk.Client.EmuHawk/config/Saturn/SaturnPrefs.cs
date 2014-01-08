using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Sega.Saturn;

namespace BizHawk.Client.EmuHawk
{
	public partial class SaturnPrefs : Form
	{
		public SaturnPrefs()
		{
			InitializeComponent();
			try
			{
				var ss = (Yabause.SaturnSyncSettings)Global.Emulator.GetSyncSettings();

				radioButtonGL.Checked = ss.UseGL;
				radioButtonSoft.Checked = !ss.UseGL;
				radioButtonFree.Checked = ss.DispFree;
				radioButtonFactor.Checked = !ss.DispFree;
				numericUpDownFactor.Value = ss.DispFactor;
				numericUpDown1.Value = ss.GLW;
				numericUpDown2.Value = ss.GLH;
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}

		private void radioButtonSoft_CheckedChanged(object sender, EventArgs e)
		{
			groupBox2.Enabled = radioButtonGL.Checked;
		}

		private void radioButtonFactor_CheckedChanged(object sender, EventArgs e)
		{
			numericUpDownFactor.Enabled = radioButtonFactor.Checked;
			numericUpDown1.Enabled = numericUpDown2.Enabled = radioButtonFree.Checked;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			var ss = (Yabause.SaturnSyncSettings)Global.Emulator.GetSyncSettings();
			ss.UseGL = radioButtonGL.Checked;
			ss.DispFree = radioButtonFree.Checked;
			ss.DispFactor = (int)numericUpDownFactor.Value;
			ss.GLW = (int)numericUpDown1.Value;
			ss.GLH = (int)numericUpDown2.Value;
			GlobalWin.MainForm.PutCoreSyncSettings(ss);
		}
	}
}
