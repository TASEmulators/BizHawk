using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//todo - perks - pause, copy to clipboard, backlog length limiting

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

		public void ShowConsole()
		{
			Show();
		}

		public void SetLogText(string str)
		{
			textBox1.Text = str;
			textBox1.SelectionStart = str.Length;
			textBox1.ScrollToCaret();
			Refresh();
		}

		StringBuilder sbLog = new StringBuilder();
		public void Append(string str)
		{
			sbLog.Append(str);
			SetLogText(sbLog.ToString());
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			sbLog.Length = 0;
			SetLogText("");
			Refresh();
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LogWindow_Load(object sender, EventArgs e)
		{

		}

	}
}
