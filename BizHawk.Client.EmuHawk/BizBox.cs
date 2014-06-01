using BizHawk.Emulation.Common;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
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
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk.html");
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void BizBox_Load(object sender, EventArgs e)
		{
			if (VersionInfo.INTERIM)
			{
				Text = " BizHawk  (SVN r" + SubWCRev.SVN_REV + ")";
			}
			else
			{
				Text = "Version " + VersionInfo.MAINVERSION + " (SVN " + SubWCRev.SVN_REV + ")";
			}

			VersionLabel.Text = "Version " + VersionInfo.MAINVERSION + " " + VersionInfo.RELEASEDATE;

			var cores = Assembly
				.Load("BizHawk.Emulation.Cores")
				.GetTypes()
				.Where(t => typeof(IEmulator).IsAssignableFrom(t))
				.Select(t => t.GetCustomAttributes(false).OfType<CoreAttributes>().FirstOrDefault())
				.Where(a => a != null)
				.Where(a => a.Released)
				.OrderByDescending(a => a.CoreName.ToLower())
				.ToList();

			foreach(var core in cores)
			{
				CoreInfoPanel.Controls.Add(new BizBoxInfoControl(core)
				{
					Dock = DockStyle.Top
				});

			}
		}
	}
}
