using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcPokeMemory : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly AmstradCPC _cpc;

		public AmstradCpcPokeMemory(IMainFormForConfig mainForm, AmstradCPC cpc)
		{
			_mainForm = mainForm;
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
			_mainForm.AddOnScreenMessage("POKE memory aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
