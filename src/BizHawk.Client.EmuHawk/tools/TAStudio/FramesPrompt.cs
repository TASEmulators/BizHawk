using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class FramesPrompt : Form
	{
		public FramesPrompt()
		{
			InitializeComponent();
		}

		public FramesPrompt(string headMessage, string bodyMessage)
		{
			this.headMessage = headMessage;
			this.bodyMessage = bodyMessage;
			InitializeComponent();
		}

		public int Frames => NumFramesBox.ToRawInt() ?? 0;
		private string headMessage = "Insert Frames";
		private string bodyMessage = "Insert number of frames:";

		private void FramesPrompt_Load(object sender, EventArgs e)
		{
			NumFramesBox.Select();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
