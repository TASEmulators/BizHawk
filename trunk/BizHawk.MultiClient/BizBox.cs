using System;
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
			linkLabel1.LinkVisited = true;
			System.Diagnostics.Process.Start("http://code.google.com/p/bizhawk/");
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void BizBox_Load(object sender, EventArgs e)
		{
			Text = " BizHawk  (SVN r" + SubWCRev.SVN_REV + ")";
			VersionLabel.Text = MainForm.EMUVERSION + "  Released " + MainForm.RELEASEDATE;
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel3.LinkVisited = true;
			System.Diagnostics.Process.Start("http://byuu.org/bsnes/");
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel2.LinkVisited = true;
			System.Diagnostics.Process.Start("http://gambatte.sourceforge.net/");
		}

		private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel4.LinkVisited = true;
			System.Diagnostics.Process.Start("http://emu7800.sourceforge.net/");
		}

		private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel5.LinkVisited = true;
			System.Diagnostics.Process.Start("https://code.google.com/p/mupen64plus/");
		}
	}
}
