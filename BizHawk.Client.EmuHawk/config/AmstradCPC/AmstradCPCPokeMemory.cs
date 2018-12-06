using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCPCPokeMemory : Form
	{
		private AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCPCPokeMemory()
		{
			InitializeComponent();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
            var ams = (AmstradCPC)Global.Emulator;
            var addr = (ushort)numericUpDownAddress.Value;
            var val = (byte)numericUpDownByte.Value;

            ams.PokeMemory(addr, val);

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
