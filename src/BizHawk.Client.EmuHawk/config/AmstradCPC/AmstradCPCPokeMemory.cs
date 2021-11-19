using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcPokeMemory : Form
	{
		private readonly AmstradCPC _cpc;

		public AmstradCpcPokeMemory(AmstradCPC cpc)
		{
			_cpc = cpc;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var addr = (ushort)numericUpDownAddress.Value;
			var val = (byte)numericUpDownByte.Value;

			_cpc.PokeMemory(addr, val);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
