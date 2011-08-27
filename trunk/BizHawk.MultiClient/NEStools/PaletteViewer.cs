using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class PaletteViewer : Control
	{
		public class Palette
		{
			public int address { get; set; }
			private int value { get; set; }
			private Color color;

			public Palette(int Address)
			{
				address = Address;
				value = -1;
			}

			public Palette(Palette p)
			{
				address = p.address;
				value = p.value;
				color = p.color;
			}

			public int GetValue()
			{
				return value;
			}

			public void SetValue(int val)
			{
				unchecked
				{
					value = val;
				}
				color = Color.FromArgb(value); //TODO: value should be unprocessed! then do all calculations on this line
			}

			public Color GetColor()
			{
				return color;
			}
		}

		public Palette[] bgPalettes = new Palette[16];
		public Palette[] spritePalettes = new Palette[16];

		public Palette[] bgPalettesPrev = new Palette[16];
		public Palette[] spritePalettesPrev = new Palette[16];

		public PaletteViewer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.Size = new Size(128, 32);
			this.BackColor = Color.White;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PaletteViewer_Paint);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PaletteViewer_KeyDown);

			for (int x = 0; x < 16; x++)
			{
				bgPalettes[x] = new Palette(x);
				spritePalettes[x] = new Palette(x + 16);
			}

		}

		private void PaletteViewer_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) { }

		private void PaletteViewer_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				Rectangle rect;
				for (int x = 0; x < 16; x++)
				{
					if (bgPalettes[x] != bgPalettesPrev[x])
					{
						rect = new Rectangle(new Point(x * 16, 1), new Size(16, 16));
						e.Graphics.FillRectangle(new SolidBrush(bgPalettes[x].GetColor()), rect);
					}
					if (spritePalettes != spritePalettesPrev)
					{
						rect = new Rectangle(new Point(x * 16, 17), new Size(16, 16));
						e.Graphics.FillRectangle(new SolidBrush(spritePalettes[x].GetColor()), rect);
					}
				}
			}
		}

		public bool HasChanged()
		{
			for (int x = 0; x < 16; x++)
			{
				if (bgPalettes[x] != bgPalettesPrev[x]) 
					return true;
				if (spritePalettes[x] != spritePalettesPrev[x]) 
					return true;
			}
			return false;
		}
	}
}
