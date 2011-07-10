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
	public partial class LogWindow : Form
	{
		public LogWindow()
		{
			InitializeComponent();
		}

		public void ShowReport(string title, string report)
		{
			Text = title;
			textBox1.Text = report;
			btnClear.Visible = false;
			ShowDialog();
		}

		private void btnClear_Click(object sender, EventArgs e)
		{

		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

	}
}
