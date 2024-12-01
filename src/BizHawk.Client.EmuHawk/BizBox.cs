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
		public BizBox(Action/*?*/ playNotHawkCallSFX = null)
		{
			InitializeComponent();
			Icon = Resources.Logo;
			pictureBox1.Image = Resources.CorpHawk;
			btnCopyHash.Image = Resources.Duplicate;
			if (playNotHawkCallSFX is not null) Shown += (_, _) => playNotHawkCallSFX();
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
			DeveloperBuildLabel.Visible = VersionInfo.DeveloperBuild;
			VersionLabel.Text = VersionInfo.GetFullVersionDetails();
			DateLabel.Text = VersionInfo.ReleaseDate;
			(linkLabel2.Text, linkLabel2.Tag) = VersionInfo.GetGitCommitLink();
			foreach (var core in CoreInventory.Instance.SystemsFlat.Where(core => core.CoreAttr.Released)
				.OrderByDescending(core => core.Name.ToLowerInvariant()))
			{
				CoreInfoPanel.Controls.Add(new BizBoxInfoControl(core.CoreAttr)
				{
					Dock = DockStyle.Top
				});
			}
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
			=> Process.Start((string) ((Control) sender).Tag);

		private void btnCopyHash_Click(object sender, EventArgs e)
			=> Clipboard.SetText(VersionInfo.GIT_HASH);

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
			=> Process.Start(VersionInfo.BizHawkContributorsListURI);
	}
}
