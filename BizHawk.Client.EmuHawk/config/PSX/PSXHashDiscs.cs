using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.DiscSystem;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	public partial class PSXHashDiscs : Form
	{
		public PSXHashDiscs()
		{
			InitializeComponent();
		}

		private void btnHash_Click(object sender, EventArgs e)
		{
			txtHashes.Text = "";
			btnHash.Enabled = false;
			try
			{
				var psx = ((Octoshock)Global.Emulator);
				foreach (var disc in psx.Discs)
				{
					DiscHasher hasher = new DiscHasher(disc);
					uint hash = hasher.Calculate_PSX_RedumpHash();
					txtHashes.Text += string.Format("{0:X8} {1}\r\n", hash, disc.Name);
				}
			}
			finally
			{
				btnHash.Enabled = true;
			}
		}
	}
}
