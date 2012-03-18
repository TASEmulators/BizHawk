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

		new public void Update()
		{
			double curr = pr.ProgressCurrent;
			double max = pr.ProgressEstimate;
			if (pr.InfoPresent)
			{
				double value = curr/max*100;
				int nValue = (int) value;
				if (nValue < 0 || nValue > 100)
					nValue = 0;
				progressBar1.Value = nValue;
			}
			lblMessage.Text = pr.Message + " - " + progressBar1.Value.ToString() + "%";
		}

		private void ProgressDialog_Load(object sender, EventArgs e)
		{

		}
	}
}
