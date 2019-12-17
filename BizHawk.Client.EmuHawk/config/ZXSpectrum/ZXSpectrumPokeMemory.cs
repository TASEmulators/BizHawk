using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumPokeMemory : Form
	{
		private readonly MainForm _mainForm;
		private readonly ZXSpectrum _speccy;
		public ZxSpectrumPokeMemory(
			MainForm mainForm,
			ZXSpectrum speccy)
		{
			_mainForm = mainForm;
			_speccy = speccy;

			InitializeComponent();
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
