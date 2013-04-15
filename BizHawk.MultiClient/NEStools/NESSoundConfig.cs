using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NESSoundConfig : Form
	{
		public NESSoundConfig()
		{
			InitializeComponent();
		}

		private void NESSoundConfig_Load(object sender, EventArgs e)
		{
			Square1.Checked = Global.Config.NESEnableSquare1;
			Square2.Checked = Global.Config.NESEnableSquare2;
			Triangle.Checked = Global.Config.NESEnableTriangle;
			Noise.Checked = Global.Config.NESEnableNoise;
			DMC.Checked = Global.Config.NESEnableDMC;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.NESEnableSquare1 = Square1.Checked;
			Global.Config.NESEnableSquare2 = Square2.Checked;
			Global.Config.NESEnableTriangle = Triangle.Checked;
			Global.Config.NESEnableNoise = Noise.Checked;
			Global.Config.NESEnableDMC = DMC.Checked;
			Global.MainForm.SetNESSoundChannels();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SelectAll_Click(object sender, EventArgs e)
		{
			if (SelectAll.Checked)
			{
				Square1.Checked = true;
				Square2.Checked = true;
				Triangle.Checked = true;
				Noise.Checked = true;
				DMC.Checked = true;
			}
			else
			{
				Square1.Checked = false;
				Square2.Checked = false;
				Triangle.Checked = false;
				Noise.Checked = false;
				DMC.Checked = false;
			}
		}
	}
}
