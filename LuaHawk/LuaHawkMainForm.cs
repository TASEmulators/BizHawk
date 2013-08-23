using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LuaHawk
{
	public partial class LuaHawkMainForm : Form
	{
		public LuaHawkMainForm()
		{
			InitializeComponent();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LuaHawkMainForm_Load(object sender, EventArgs e)
		{

		}
	}
}
