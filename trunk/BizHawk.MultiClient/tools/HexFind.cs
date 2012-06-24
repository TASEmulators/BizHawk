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
	public partial class HexFind : Form
	{
		private Point location;
		public HexFind()
		{
			InitializeComponent();
		}

		public void SetInitialValue(string value)
		{
			FindBox.Text = value;
		}

		public void SetLocation(Point p)
		{
			location = p;
			
		}

		private void HexFind_Load(object sender, EventArgs e)
		{
			if (location.X > 0 && location.Y > 0)
				this.Location = location;
		}

		private void Find_Prev_Click(object sender, EventArgs e)
		{
			Global.MainForm.HexEditor1.FindPrev(FindBox.Text);
		}

		private void Find_Next_Click(object sender, EventArgs e)
		{
			Global.MainForm.HexEditor1.FindNext(FindBox.Text);
		}
	}
}
