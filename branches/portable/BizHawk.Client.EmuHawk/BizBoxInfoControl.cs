using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BizBoxInfoControl : UserControl
	{
		private string url = string.Empty;

		public BizBoxInfoControl(CoreAttributes attributes)
		{
			InitializeComponent();
			CoreNameLabel.Text = attributes.CoreName;
			
			if (!string.IsNullOrEmpty(attributes.Author))
			{ 
				CoreAuthorLabel.Text = "authors: " + attributes.Author;
			}
			else
			{
				CoreAuthorLabel.Visible = false;
			}

			CorePortedLabel.Text = attributes.Ported ? " (Ported)" : string.Empty;

			if (!attributes.Ported)
			{
				CoreUrlLink.Visible = false;
			}
			else
			{
				CoreUrlLink.Visible = true;
				CoreUrlLink.Text = attributes.PortedVersion;
				url = attributes.PortedUrl;
			}
		}

		private void BizBoxInfoControl_Load(object sender, EventArgs e)
		{

		}

		private void CoreUrlLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			CoreUrlLink.LinkVisited = true;
			System.Diagnostics.Process.Start(url);
		}
	}
}
