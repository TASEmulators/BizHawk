using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk
{
	public partial class ProgressDialog : Form
	{
		public ProgressDialog(DiscSystem.ProgressReport pr)
		{
			InitializeComponent();
			this.pr = pr;
		}

		DiscSystem.ProgressReport pr;

		private void btnCancel_Click(object sender, EventArgs e)
		{
			btnCancel.Enabled = false;
			pr.CancelSignal = true;
		}

		public void Update()
		{
			double curr = pr.ProgressCurrent;
			double max = pr.ProgressEstimate;
			double value = curr / max * 100;
			progressBar1.Value = (int)value;
			lblMessage.Text = pr.Message;
		}
	}
}
