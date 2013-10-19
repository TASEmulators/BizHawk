using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.SATTools
{
	public partial class SaturnPrefs : Form
	{
		public SaturnPrefs()
		{
			InitializeComponent();
			try
			{
				radioButtonGL.Checked = Global.Config.SaturnUseGL;
				radioButtonSoft.Checked = !Global.Config.SaturnUseGL;
				radioButtonFree.Checked = Global.Config.SaturnDispFree;
				radioButtonFactor.Checked = !Global.Config.SaturnDispFree;
				numericUpDownFactor.Value = Global.Config.SaturnDispFactor;
				numericUpDown1.Value = Global.Config.SaturnGLW;
				numericUpDown2.Value = Global.Config.SaturnGLH;
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
			Global.Config.SaturnUseGL = radioButtonGL.Checked;
			Global.Config.SaturnDispFree = radioButtonFree.Checked;
			Global.Config.SaturnDispFactor = (int)numericUpDownFactor.Value;
			Global.Config.SaturnGLW = (int)numericUpDown1.Value;
			Global.Config.SaturnGLH = (int)numericUpDown2.Value;
		}
	}
}
