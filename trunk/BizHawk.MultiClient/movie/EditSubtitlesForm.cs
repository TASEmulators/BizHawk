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
	public partial class EditSubtitlesForm : Form
	{
		public bool ReadOnly;

		public EditSubtitlesForm()
		{
			InitializeComponent();
		}

		private void EditSubtitlesForm_Load(object sender, EventArgs e)
		{
			if (ReadOnly)
			{
				//Set all columns to read only
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!ReadOnly)
			{
				//Save subtitles to movie object & write to disk
			}
			this.Close();
		}
	}
}
