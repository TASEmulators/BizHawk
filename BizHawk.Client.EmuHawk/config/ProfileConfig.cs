using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		public ProfileConfig()
		{
			InitializeComponent();
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{

		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			/* Saving logic goes here */

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void checkBox1_MouseHover(object sender, EventArgs e)
		{
			richTextBox1.Text = "Save Screenshot with Savestates: \r\n * Required for TASing \r\n * Not Recommended for \r\n   Longplays or Casual Gaming";
		}
		private void checkBox2_MouseHover(object sender, EventArgs e)
		{
			richTextBox1.Text = "Save Large Screenshot With States: \r\n * Required for TASing \r\n * Not Recommended for \r\n   Longplays or Casual Gaming";
		}
		private void checkBox3_MouseHover(object sender, EventArgs e)
		{
			richTextBox1.Text = "All Up+Down or Left+Right: \r\n * Useful for TASing \r\n * Unchecked for Casual Gaming \r\n * Unknown for longplays";
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox1.SelectedIndex == 0) //Casual Gaming
			{
				checkBox1.Checked = false;
				checkBox2.Checked = false;
				checkBox3.Checked = false;
			}
			else if (comboBox1.SelectedIndex == 1) //TAS
			{
				checkBox1.Checked = true;
				checkBox2.Checked = true;
				checkBox3.Checked = true;
			}
			else if (comboBox1.SelectedIndex == 2) //Long Plays
			{
				checkBox1.Checked = false;
				checkBox2.Checked = false;
				checkBox3.Checked = false;
			}
		}
	}
}
