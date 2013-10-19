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
	public partial class About : Form
	{
		public About()
		{
			InitializeComponent();
			lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}

		private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			 System.Diagnostics.Process.Start(e.LinkText);
		}
	}
}
