using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZXSpectrumPokeMemory : Form
	{
		private ZXSpectrum.ZXSpectrumSettings _settings;

		public ZXSpectrumPokeMemory()
		{
			InitializeComponent();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
            var speccy = (ZXSpectrum)Global.Emulator;
            var addr = (ushort)numericUpDownAddress.Value;
            var val = (byte)numericUpDownByte.Value;

            speccy.PokeMemory(addr, val);

            DialogResult = DialogResult.OK;
            Close();            
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("POKE memory aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
    }
}
