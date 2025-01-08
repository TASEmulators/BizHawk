using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SubtitleMaker : Form
	{
		public Subtitle Sub { get; set; } = new Subtitle();

		public SubtitleMaker()
		{
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
		}

		public void DisableFrame()
		{
			FrameNumeric.Enabled = false;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Sub.Frame = (int)FrameNumeric.Value;
			Sub.Message = Message.Text;
			Sub.X = (int)XNumeric.Value;
			Sub.Y = (int)YNumeric.Value;
			Sub.Duration = (int)DurationNumeric.Value;
			Sub.Color = (uint)colorDialog1.Color.ToArgb();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SubtitleMaker_Load(object sender, EventArgs e)
		{
			FrameNumeric.Value = Sub.Frame;
			Message.Text = Sub.Message;
			XNumeric.Value = Sub.X;
			YNumeric.Value = Sub.Y;
			DurationNumeric.Value = Sub.Duration;
			colorDialog1.Color = Color.FromArgb((int)Sub.Color);
			ColorPanel.BackColor = colorDialog1.Color;
			Message.Select();
		}

		private void ColorPanel_DoubleClick(object sender, EventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				ColorPanel.BackColor = colorDialog1.Color;
			}
		}
	}
}
