using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class PatternViewer : Control
	{
		Size pSize;
		public Bitmap pattern;
		public int Pal0 = 0; //0-7 Palette choice
		public int Pal1 = 0;

		public PatternViewer()
		{
			pSize = new Size(256, 128);
			pattern = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			this.Size = pSize;
			this.BackColor = Color.Transparent;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PatternViewer_Paint);
		}

		private void PatternViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(pattern, 1, 1);
		}
	}
}
