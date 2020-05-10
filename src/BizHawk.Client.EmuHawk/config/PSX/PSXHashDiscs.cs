using System;
using System.Windows.Forms;

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
			txtHashes.Text = _psx.CalculateDiscHashes();
			btnHash.Enabled = true;
		}
	}
}
