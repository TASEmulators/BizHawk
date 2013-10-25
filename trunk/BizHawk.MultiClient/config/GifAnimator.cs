using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class GifAnimator : Form
	{
		public GifAnimator()
		{
			InitializeComponent();
		}

		private void GifAnimator_Load(object sender, EventArgs e)
		{
			comboBox1.Items.AddRange(new object[] { "x1/32", "x1/16", "x1/8", "x1/4", "x1/2", "x1", "x2", "x4", "x8", "x16", "x32" });
			switch (Global.Config.GifAnimatorSpeed)
			{
				case (-32): comboBox1.SelectedIndex = 0; break;
				case (-16): comboBox1.SelectedIndex = 1; break;
				case (-8): comboBox1.SelectedIndex = 2; break;
				case (-4): comboBox1.SelectedIndex = 3; break;
				case (-2): comboBox1.SelectedIndex = 4; break;
				case (1): comboBox1.SelectedIndex = 5; break;
				case (2): comboBox1.SelectedIndex = 6; break;
				case (4): comboBox1.SelectedIndex = 7; break;
				case (8): comboBox1.SelectedIndex = 8; break;
				case (16): comboBox1.SelectedIndex = 9; break;
				case (32): comboBox1.SelectedIndex = 10; break;
				default: comboBox1.SelectedIndex = 5; break;
			}
			if (Global.Config.GifAnimatorNumFrames == 0) Global.Config.GifAnimatorNumFrames = 1;
			if (Global.Config.GifAnimatorFrameSkip == 0) Global.Config.GifAnimatorFrameSkip = 1;
			TB_Frame_Skip.Text = Global.Config.GifAnimatorFrameSkip.ToString();
			TB_Num_Frames.Text = Global.Config.GifAnimatorNumFrames.ToString();
			checkBox1.Checked = Global.Config.GifAnimatorReversable;
		}

		private void Exit_Click(object sender, EventArgs e)
		{
			int FrameSkip;
			int NumFrames;
			if (!Int32.TryParse(TB_Frame_Skip.Text, out FrameSkip) || !Int32.TryParse(TB_Num_Frames.Text, out NumFrames) || FrameSkip < 1 || NumFrames < 1)
			{
				MessageBox.Show("The values you've selected are invalid");
				return;
			}

			Global.Config.GifAnimatorNumFrames = NumFrames;
			Global.Config.GifAnimatorFrameSkip = FrameSkip;
			Global.Config.GifAnimatorReversable = checkBox1.Checked;

			switch (comboBox1.SelectedIndex)
			{
				case (0): Global.Config.GifAnimatorSpeed = -32; break;
				case (1): Global.Config.GifAnimatorSpeed = -16; break;
				case (2): Global.Config.GifAnimatorSpeed = -8; break;
				case (3): Global.Config.GifAnimatorSpeed = -4; break;
				case (4): Global.Config.GifAnimatorSpeed = -2; break;
				case (5): Global.Config.GifAnimatorSpeed = 1; break;
				case (6): Global.Config.GifAnimatorSpeed = 2; break;
				case (7): Global.Config.GifAnimatorSpeed = 4; break;
				case (8): Global.Config.GifAnimatorSpeed = 8; break;
				case (9): Global.Config.GifAnimatorSpeed = 16; break;
				case (10): Global.Config.GifAnimatorSpeed = 32; break;
			}

			this.Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
