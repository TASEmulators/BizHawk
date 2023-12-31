using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public partial class BizBox : Form
	{
		private static readonly byte[] _bizBoxSound = ReflectionCache.EmbeddedResourceStream("Resources.nothawk.wav").ReadAllBytes();
		private readonly Action<byte[]> _playWavFileCallback;

		public BizBox(Action<byte[]> playWavFileCallback)
		{
			InitializeComponent();
			Icon = Resources.Logo;
			pictureBox1.Image = Resources.CorpHawk;
			btnCopyHash.Image = Resources.Duplicate;
			_playWavFileCallback = playWavFileCallback;
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

#if true //TODO prepare for re-adding x86 and adding ARM/RISC-V
			const string targetArch = "x64";
#else
			var targetArch = IntPtr.Size is 8 ? "x64" : "x86";
#endif
#if DEBUG
			const string buildConfig = "Debug";
#else
			const string buildConfig = "Release";
#endif
			VersionLabel.Text = $"Version {VersionInfo.MainVersion}";
			VersionLabel.Text += VersionInfo.DeveloperBuild
				? $" — dev build ({buildConfig}, {targetArch})"
				: $" ({targetArch})";
			DateLabel.Text = VersionInfo.ReleaseDate;

			foreach (var core in CoreInventory.Instance.SystemsFlat.Where(core => core.CoreAttr.Released)
				.OrderByDescending(core => core.Name.ToLowerInvariant()))
			{
				CoreInfoPanel.Controls.Add(new BizBoxInfoControl(core.CoreAttr)
				{
					Dock = DockStyle.Top
				});
			}

			linkLabel2.Text = $"Commit :{VersionInfo.GIT_BRANCH}@{VersionInfo.GIT_SHORTHASH}";
		}

		private void BizBox_Shown(object sender, EventArgs e)
			=> _playWavFileCallback(_bizBoxSound);

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start($"https://github.com/TASEmulators/BizHawk/commit/{VersionInfo.GIT_SHORTHASH}");
		}

		private void btnCopyHash_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(VersionInfo.GIT_SHORTHASH);
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("https://github.com/TASEmulators/BizHawk/graphs/contributors");
		}
	}
}
