using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESSoundConfig : Form, IToolForm
	{
		private NES.NESSettings oldsettings;
		private NES.NESSettings settings;

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }
		public void UpdateValues()
		{
			if (!(Global.Emulator is NES))
			{
				Close();
			}
		}
		public void Restart()
		{
			if (!(Global.Emulator is NES))
			{
				Close();
			}
		}

		public NESSoundConfig()
		{
			InitializeComponent();
			// get baseline maxes from a default config object
			var d = new NES.NESSettings();
			trackBar1.Maximum = d.Square1;
			trackBar2.Maximum = d.Square2;
			trackBar3.Maximum = d.Triangle;
			trackBar4.Maximum = d.Noise;
			trackBar5.Maximum = d.DMC;
		}

		private void NESSoundConfig_Load(object sender, EventArgs e)
		{
			oldsettings = (NES.NESSettings)Global.Emulator.GetSettings();
			settings = oldsettings.Clone();

			trackBar1.Value = settings.Square1;
			trackBar2.Value = settings.Square2;
			trackBar3.Value = settings.Triangle;
			trackBar4.Value = settings.Noise;
			trackBar5.Value = settings.DMC;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			//restore previous value
			Global.Emulator.PutSettings(oldsettings);
			Close();
		}

		private void trackBar1_ValueChanged(object sender, EventArgs e)
		{
			label6.Text = trackBar1.Value.ToString();
			settings.Square1 = trackBar1.Value;
			Global.Emulator.PutSettings(settings);
		}

		private void trackBar2_ValueChanged(object sender, EventArgs e)
		{
			label7.Text = trackBar2.Value.ToString();
			settings.Square2 = trackBar2.Value;
			Global.Emulator.PutSettings(settings);
		}

		private void trackBar3_ValueChanged(object sender, EventArgs e)
		{
			label8.Text = trackBar3.Value.ToString();
			settings.Triangle = trackBar3.Value;
			Global.Emulator.PutSettings(settings);
		}

		private void trackBar4_ValueChanged(object sender, EventArgs e)
		{
			label9.Text = trackBar4.Value.ToString();
			settings.Noise = trackBar4.Value;
			Global.Emulator.PutSettings(settings);
		}

		private void trackBar5_ValueChanged(object sender, EventArgs e)
		{
			label10.Text = trackBar5.Value.ToString();
			settings.DMC = trackBar5.Value;
			Global.Emulator.PutSettings(settings);
		}
	}
}
