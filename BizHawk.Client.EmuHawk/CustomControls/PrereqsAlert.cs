using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public partial class PrereqsAlert : Form
	{
		public PrereqsAlert(bool warnOnly)
		{
			InitializeComponent();
			if (warnOnly)
			{
				button1.Text = "Continue";
			}
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel1.LinkVisited = true;
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk.html");
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel2.LinkVisited = true;
			System.Diagnostics.Process.Start("https://github.com/TASVideos/BizHawk-Prereqs/releases");
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
