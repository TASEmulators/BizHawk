using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class AboutBox : Form
	{
		SoundPlayer sfx;
		Random r = new Random();
		int ctr = 0;
		Point loc;

		public AboutBox()
		{
			InitializeComponent();
			loc = label1.Location;

			label1.Text = "";
			try
			{
				var rm = new System.Resources.ResourceManager("BizHawk.MultiClient.Properties.Resources", GetType().Assembly);
				sfx = new SoundPlayer(rm.GetStream("nothawk"));
				sfx.Play();
			}
			catch
			{
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			if(sfx != null)
				sfx.Dispose();
		}

		int smack = 0;
		private void timer1_Tick(object sender, EventArgs e)
		{
			ctr++;
			if (ctr == 3)
				label1.Text = "BIZ";
			else if (ctr == 10)
				label1.Text = "BIZ HAWK";
			else if (ctr == 20)
			{
				label1.ForeColor = Color.LightGreen;
				label1.Text = "BIZHAWK";
			}
			else if (ctr > 20)
			{
				if (label1.ForeColor == Color.LightGreen)
					label1.ForeColor = Color.Pink;
				else label1.ForeColor = Color.LightGreen;
			}

			if (ctr/5 % 2 ==0)
			{
				mom1.Visible = true;
				mom2.Visible = false;
			}
			else
			{
				mom1.Visible = false;
				mom2.Visible = true;
			}

			if (ctr > 30)
			{
				if(ctr/7%7<4)
					label1.Location = new Point(loc.X + r.Next(3) - 1, loc.Y + r.Next(3) - 1);
				else
					label1.Location = new Point(loc.X + r.Next(5) - 3, loc.Y + r.Next(5) - 3);
			}
		}
	}

	
}
