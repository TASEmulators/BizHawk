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
	public partial class GifAnimator : Form
	{
		public GifAnimator()
		{
			InitializeComponent();
		}

		private void GifAnimator_Load(object sender, EventArgs e)
		{
			
		}

		private void Exit_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
