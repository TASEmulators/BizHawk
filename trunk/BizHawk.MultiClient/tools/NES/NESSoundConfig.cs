using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NESSoundConfig : Form
	{
		int sq1, sq2, tr, no, dmc = 0;
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
			//save value for cancel
			sq1 = Global.Config.NESSquare1;
			sq2 = Global.Config.NESSquare2;
			tr = Global.Config.NESTriangle;
			no = Global.Config.NESNoise;
			dmc = Global.Config.NESDMC;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			//restore previous value
			//restore value
			Global.Config.NESSquare1 = sq1;
			Global.Config.NESSquare2 = sq2;
			Global.Config.NESTriangle = tr;
			Global.Config.NESNoise = no;
			Global.Config.NESDMC = dmc;
			Global.MainForm.SetNESSoundChannels();
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
			Global.Config.NESSquare1 = trackBar1.Value;
			Global.MainForm.SetNESSoundChannels();
		}

		private void trackBar2_ValueChanged(object sender, EventArgs e)
		{
			label7.Text = trackBar2.Value.ToString();
			Global.Config.NESSquare2 = trackBar2.Value;
			Global.MainForm.SetNESSoundChannels();
		}

		private void trackBar3_ValueChanged(object sender, EventArgs e)
		{
			label8.Text = trackBar3.Value.ToString();
			Global.Config.NESTriangle = trackBar3.Value;
			Global.MainForm.SetNESSoundChannels();
		}

		private void trackBar4_ValueChanged(object sender, EventArgs e)
		{
			label9.Text = trackBar4.Value.ToString();
			Global.Config.NESNoise = trackBar4.Value;
			Global.MainForm.SetNESSoundChannels();
		}

		private void trackBar5_ValueChanged(object sender, EventArgs e)
		{
			label10.Text = trackBar5.Value.ToString();
			Global.Config.NESDMC = trackBar5.Value;
			Global.MainForm.SetNESSoundChannels();
		}
	}
}
