using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class SubtitleMaker : Form
	{
		public Subtitle sub = new Subtitle();

		public SubtitleMaker()
		{
			InitializeComponent();
		}

		public void DisableFrame()
		{
			FrameNumeric.Enabled = false;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			sub.Frame = (int)FrameNumeric.Value;
			sub.Message = Message.Text;
			sub.X = (int)XNumeric.Value;
			sub.Duration = (int)DurationNumeric.Value;
			sub.Color = (uint)colorDialog1.Color.ToArgb();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SubtitleMaker_Load(object sender, EventArgs e)
		{
			FrameNumeric.Value = sub.Frame;
			Message.Text = sub.Message;
			XNumeric.Value = sub.X;
			YNumeric.Value = sub.Y;
			DurationNumeric.Value = sub.Duration;
			colorDialog1.Color = Color.FromArgb((int)sub.Color);
			ColorPanel.BackColor = colorDialog1.Color;
			Message.Focus();
		}

		private void ColorPanel_DoubleClick(object sender, EventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				ColorPanel.BackColor = colorDialog1.Color;
			}
		}
	}
}
