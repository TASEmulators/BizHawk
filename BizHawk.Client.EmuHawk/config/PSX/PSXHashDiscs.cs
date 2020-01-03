using System;
using System.Windows.Forms;

using BizHawk.Emulation.DiscSystem;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	public partial class PSXHashDiscs : Form
	{
		private readonly Octoshock _psx;

		public PSXHashDiscs(Octoshock psx)
		{
			_psx = psx;
			InitializeComponent();
		}

		private void BtnHash_Click(object sender, EventArgs e)
		{
			txtHashes.Text = "";
			btnHash.Enabled = false;
			try
			{
				foreach (var disc in _psx.Discs)
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
