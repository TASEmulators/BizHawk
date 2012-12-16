using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class BizBox : Form
	{
		public BizBox()
		{
			InitializeComponent();
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.linkLabel1.LinkVisited = true;
			System.Diagnostics.Process.Start("http://code.google.com/p/bizhawk/");
		}

		private void OK_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void BizBox_Load(object sender, EventArgs e)
		{
			Text = " BizHawk  (SVN r" + SubWCRev.SVN_REV + ")";
			VersionLabel.Text = MainForm.EMUVERSION + "  Released " + MainForm.RELEASEDATE;
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.linkLabel3.LinkVisited = true;
			System.Diagnostics.Process.Start("http://byuu.org/bsnes/");
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.linkLabel2.LinkVisited = true;
			System.Diagnostics.Process.Start("http://gambatte.sourceforge.net/");
		}

		private void label30_Click(object sender, EventArgs e)
		{

		}

		private void label28_Click(object sender, EventArgs e)
		{

		}

		private void label29_Click(object sender, EventArgs e)
		{

		}

		private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.linkLabel4.LinkVisited = true;
			System.Diagnostics.Process.Start("http://emu7800.sourceforge.net/");
		}
	}
}
