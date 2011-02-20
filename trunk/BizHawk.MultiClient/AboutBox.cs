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
	public partial class AboutBox : Form
	{
		public AboutBox()
		{
			InitializeComponent();
			label1.ForeColor = Color.LightGreen;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (label1.ForeColor == Color.LightGreen)
				label1.ForeColor = Color.Pink;
			else label1.ForeColor = Color.LightGreen;
		}
	}

	
}
