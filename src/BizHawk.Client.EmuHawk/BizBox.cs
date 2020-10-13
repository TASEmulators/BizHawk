using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public partial class BizBox : Form
	{
		public BizBox()
		{
			InitializeComponent();
			Icon = Resources.Logo;
			pictureBox1.Image = Resources.CorpHawk;
			btnCopyHash.Image = Resources.Duplicate;
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			linkLabel1.LinkVisited = true;
			Process.Start(VersionInfo.HomePage);
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void BizBox_Load(object sender, EventArgs e)
		{
			string mainVersion = VersionInfo.MainVersion;
			if (IntPtr.Size == 8)
			{
				mainVersion += " (x64)";
			}

			DeveloperBuildLabel.Visible = VersionInfo.DeveloperBuild;

			Text = VersionInfo.DeveloperBuild
				? $" BizHawk  (GIT {VersionInfo.GIT_BRANCH}#{VersionInfo.GIT_SHORTHASH})"
				: $"Version {mainVersion} (GIT {VersionInfo.GIT_BRANCH}#{VersionInfo.GIT_SHORTHASH})";

			VersionLabel.Text = $"Version {mainVersion}";
			DateLabel.Text = VersionInfo.ReleaseDate;

			foreach (var core in CoreInventory.Instance.SystemsFlat.Where(core => core.CoreAttr.Released)
				.OrderByDescending(core => core.Name.ToLowerInvariant()))
			{
				CoreInfoPanel.Controls.Add(new BizBoxInfoControl(core.CoreAttr)
				{
					Dock = DockStyle.Top
				});
			}

			linkLabel2.Text = $"Commit # {VersionInfo.GIT_SHORTHASH}";
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start($"https://github.com/TASVideos/BizHawk/commit/{VersionInfo.GIT_SHORTHASH}");
		}

		private void btnCopyHash_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(VersionInfo.GIT_SHORTHASH);
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("https://github.com/TASVideos/BizHawk/graphs/contributors");
		}
	}
}
