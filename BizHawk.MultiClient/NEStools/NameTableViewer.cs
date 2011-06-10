using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class NameTableViewer : Control
	{
		Size pSize;
		public Bitmap nametables;

		public NameTableViewer()
		{
			pSize = new Size(512, 480);
			nametables = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.Size = new Size(256, 224);
			this.BackColor = Color.White;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.NameTableViewer_Paint);
		}

		public enum WhichNametable
		{
			NT_2000, NT_2400, NT_2800, NT_2C00, NT_ALL
		}

		public WhichNametable Which = WhichNametable.NT_ALL;

		private void Display(Graphics g)
		{
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			switch (Which)
			{
				case WhichNametable.NT_ALL:
					g.DrawImageUnscaled(nametables, 1, 1);
					break;
				case WhichNametable.NT_2000:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2400:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2800:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 240, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2C00:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 240, 256, 240, GraphicsUnit.Pixel);
					break;
			}
		}

		private void NameTableViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}
	}
}
