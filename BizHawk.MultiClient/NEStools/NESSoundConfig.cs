using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NESSoundConfig : Form
	{
		public NESSoundConfig()
		{
			InitializeComponent();
			trackBar1.Maximum = Config.NESSquare1Max;
			trackBar2.Maximum = Config.NESSquare2Max;
			trackBar3.Maximum = Config.NESTriangleMax;
			trackBar4.Maximum = Config.NESNoiseMax;
			trackBar5.Maximum = Config.NESDMCMax;
		}

		private void NESSoundConfig_Load(object sender, EventArgs e)
		{
			trackBar1.Value = Global.Config.NESSquare1;
			trackBar2.Value = Global.Config.NESSquare2;
			trackBar3.Value = Global.Config.NESTriangle;
			trackBar4.Value = Global.Config.NESNoise;
			trackBar5.Value = Global.Config.NESDMC;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.NESSquare1 = trackBar1.Value;
			Global.Config.NESSquare2 = trackBar2.Value;
			Global.Config.NESTriangle = trackBar3.Value;
			Global.Config.NESNoise = trackBar4.Value;
			Global.Config.NESDMC = trackBar5.Value;
			Global.MainForm.SetNESSoundChannels();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SelectAll_Click(object sender, EventArgs e)
		{
			/*
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
			*/
		}

		private void trackBar1_ValueChanged(object sender, EventArgs e)
		{
			label6.Text = trackBar1.Value.ToString();
		}

		private void trackBar2_ValueChanged(object sender, EventArgs e)
		{
			label7.Text = trackBar2.Value.ToString();
		}

		private void trackBar3_ValueChanged(object sender, EventArgs e)
		{
			label8.Text = trackBar3.Value.ToString();
		}

		private void trackBar4_ValueChanged(object sender, EventArgs e)
		{
			label9.Text = trackBar4.Value.ToString();
		}

		private void trackBar5_ValueChanged(object sender, EventArgs e)
		{
			label10.Text = trackBar5.Value.ToString();
		}
	}
}
