using System.Windows.Forms;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BizBoxInfoControl : UserControl
	{
		private readonly string _url = "";

		public BizBoxInfoControl(CoreAttribute attributes)
		{
			InitializeComponent();
			CoreNameLabel.Text = attributes.CoreName;
			
			if (!string.IsNullOrEmpty(attributes.Author))
			{ 
				CoreAuthorLabel.Text = $"authors: {attributes.Author}";
			}
			else
			{
				CoreAuthorLabel.Visible = false;
			}

			CorePortedLabel.Text = attributes.Ported ? " (Ported)" : "";

			if (!attributes.Ported)
			{
				CoreUrlLink.Visible = false;
			}
			else
			{
				CoreUrlLink.Visible = true;
				CoreUrlLink.Text = attributes.PortedVersion;
				_url = attributes.PortedUrl;
			}
		}

		private void CoreUrlLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			CoreUrlLink.LinkVisited = true;
			System.Diagnostics.Process.Start(_url);
		}
	}
}
