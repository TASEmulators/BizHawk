using System;
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

		private void BtnHash_Click(object sender, EventArgs e)
		{
			txtHashes.Text = "";
			btnHash.Enabled = false;
			try
			{
				var psx = (Octoshock)Global.Emulator;
				foreach (var disc in psx.Discs)
				{
					DiscHasher hasher = new DiscHasher(disc);
					uint hash = hasher.Calculate_PSX_RedumpHash();
					txtHashes.Text += $"{hash:X8} {disc.Name}\r\n";
				}
			}
			finally
			{
				btnHash.Enabled = true;
			}
		}
	}
}
