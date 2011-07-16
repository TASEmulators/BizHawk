using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class SpriteViewer : Control
	{
		Size pSize;
		public Bitmap sprites;

		public SpriteViewer()
		{
			pSize = new Size(256, 128);
			sprites = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.Size = pSize;
			this.BackColor = Color.White;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.SpriteViewer_Paint);
		}

		private void Display(Graphics g)
		{
			unchecked
			{
				g.DrawImage(sprites, 1, 1);
			}
		}

		private void SpriteViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}
	}
}
