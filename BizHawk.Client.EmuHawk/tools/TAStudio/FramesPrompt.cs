using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class FramesPrompt : Form
	{
		public FramesPrompt()
		{
			InitializeComponent();
		}

		public int Frames
		{
			get { return NumFramesBox.ToRawInt() ?? 0;  }
		}

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
