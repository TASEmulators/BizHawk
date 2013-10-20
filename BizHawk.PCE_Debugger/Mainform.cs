using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.PCE_Debugger
{
	public partial class Mainform : Form
	{
		public Mainform()
		{
			InitializeComponent();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Mainform_Load(object sender, EventArgs e)
		{

		}
	}
}
