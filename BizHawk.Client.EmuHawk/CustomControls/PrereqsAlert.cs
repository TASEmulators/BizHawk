using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public partial class PrereqsAlert : Form
	{
		public PrereqsAlert(bool warn_only)
		{
			InitializeComponent();
			if (warn_only)
				button1.Text = "Continue";
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel1.LinkVisited = true;
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk.html");
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel2.LinkVisited = true;
			System.Diagnostics.Process.Start("http://sf.net/projects/bizhawk");
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
