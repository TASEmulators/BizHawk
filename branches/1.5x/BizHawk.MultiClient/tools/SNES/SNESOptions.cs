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
	public partial class SNESOptions : Form
	{
		public SNESOptions()
		{
			InitializeComponent();
		}

		public string Profile
		{
			get { return rbCompatibility.Checked ? "Compatibility" : "Performance"; }
			set
			{
				rbCompatibility.Checked = (value == "Compatibility");
				rbPerformance.Checked = (value == "Performance");
			}
		}

		public bool UseRingBuffer
		{
			get { return cbRingbuf.Checked; }
			set { cbRingbuf.Checked = value; }
		}

		public bool AlwaysDoubleSize
		{
			get { return cbDoubleSize.Checked; }
			set { cbDoubleSize.Checked = value; }
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Close();
		}
	}
}
