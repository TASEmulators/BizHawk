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

		private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel6.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/richard42/mupen64plus-core/");
		}

		private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel7.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/richard42/mupen64plus-rsp-hle/");
		}

		private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel8.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/richard42/mupen64plus-win32-deps/");
		}

		private void linkLabel9_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel9.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/richard42/mupen64plus-video-rice/");
		}

		private void linkLabel10_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel10.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/richard42/mupen64plus-video-glide64mk2");
		}

		private void linkLabel11_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel11.LinkVisited = true;
			System.Diagnostics.Process.Start("https://bitbucket.org/wahrhaft/mupen64plus-video-glide64/");
		}
	}
}
