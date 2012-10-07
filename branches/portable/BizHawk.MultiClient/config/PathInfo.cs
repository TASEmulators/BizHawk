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
	public partial class PathInfo : Form
	{
		public PathInfo()
		{
			InitializeComponent();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
