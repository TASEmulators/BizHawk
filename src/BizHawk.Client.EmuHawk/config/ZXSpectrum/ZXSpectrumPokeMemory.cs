using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumPokeMemory : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly ZXSpectrum _speccy;
		public ZxSpectrumPokeMemory(
			IMainFormForConfig mainForm,
			ZXSpectrum speccy)
		{
			_mainForm = mainForm;
			_speccy = speccy;

			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var addr = (ushort)numericUpDownAddress.Value;
			var val = (byte)numericUpDownByte.Value;

			_speccy.PokeMemory(addr, val);

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
