using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class AboutBox : Form
	{
		private readonly SoundPlayer sfx;
		private readonly Random r = new Random();
		private int ctr;
		private Point loc;

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

			
			//panel1.Size = new System.Drawing.Size(1000, 1000);
			//pictureBox5.GetType().GetMethod("SetStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod).Invoke(pictureBox5, new object[] { ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true });
			pictureBox5.BackColor = Color.Transparent;
			pictureBox5.SendToBack();
			pictureBox3.BringToFront();
			pictureBox2.BringToFront();
			pictureBox1.BringToFront();
			pictureBox5.Visible = false;
		}

		protected override void OnClosed(EventArgs e)
		{
			if(sfx != null)
				sfx.Dispose();
		}

		//int smack = 0;
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

			pictureBox2.Location = new Point((int)(353 + 800 + -800* Math.Abs(Math.Sin(ctr / 18.0))), pictureBox2.Location.Y);

			if ((ctr) % 40 == 0)
			{
				xbleh = -10;
				bounceCounter = 0;
				pictureBox5.Visible = true;
				pictureBox3.Visible = false;
			}
			if (bounceCounter == 10)
			{
				bounceCounter++;
				xbleh = 0;
				pictureBox3.Visible = true;
			}
			else if (bounceCounter == 30)
			{
				bounceCounter = -1;
				pictureBox5.Visible = false;
			}
			else if(bounceCounter != -1)
			{
				bounceCounter++;
				if (xbleh == -10)
					xbleh = 10;
				else xbleh = -10;
			}

			pictureBox5.Invalidate();
			pictureBox5.Update();
			pictureBox4.Location = new Point(21 + xbleh, 89);
		}

		private int xbleh;
		private int bounceCounter = -1;

		public void PaintJunk(Graphics g)
		{
			g.FillRectangle(Brushes.Transparent, 0, 0, 1000, 1000);

			using (Font font = new Font("Courier New", 20, FontStyle.Bold))
			{
				if (bounceCounter == -1) return;
				const string str = "INTERIM BUILD";
				float x = 0;
				int timefactor = bounceCounter;
				for (int i = 0; i < str.Length; i++)
				{
					string slice = str.Substring(i, 1);
					g.PageUnit = GraphicsUnit.Pixel;
					x += g.MeasureString(slice, font).Width - 1;

					int offset = -i * 3 + timefactor*3;
					int yofs = 0;
					if (offset < 0)
					{ continue; }
					else
						if (offset < DigitTable.Length)
							yofs = DigitTable[offset];
					g.DrawString(slice, font, Brushes.Black, 5 + x, 15 - yofs);
				}
			}
		}

		private readonly int[] DigitTable ={
			0,3,6,9,12,
			14,15,15,16,16,16,15,15,14,12,
			9,6,3,0,2,4,4,5,5,5,
			4,4,2,1,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0};

        private void AboutBox_Load(object sender, EventArgs e)
        {
#if DEBUG
			Text = "BizHawk Interim Build (DEBUG MODE) SVN r" + SubWCRev.SVN_REV;
#else
			Text = "BizHawk Interim Build (RELEASE MODE) SVN r" + SubWCRev.SVN_REV;
#endif
        }

		private void Close_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Close_MouseEnter(object sender, EventArgs e)
		{
			Random random = new Random();
			int width = random.Next(1, Width - CloseBtn.Width);
			int height = random.Next(1, Height - CloseBtn.Height);
			CloseBtn.Location = new Point(width, height);
			CloseBtn.BringToFront();
		}
	}

	class MyViewportPanel : Control
	{
		public MyViewportPanel()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.UserMouse, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			AboutBox ab = FindForm() as AboutBox;
			if (ab != null)
				ab.PaintJunk(e.Graphics);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.Style &= ~0x04000000; //WS_CLIPSIBLINGS
				cp.Style &= ~0x02000000; //WS_CLIPCHILDREN
				return cp;
			}
		}

	}
	
}
