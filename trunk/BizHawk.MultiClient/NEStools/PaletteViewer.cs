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

            public int GetValue()
            {
                return value;
            }

            public void SetValue(int val)
            {
                unchecked
                {
                    value = val | (int)0xFF000000;
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
                spritePalettes[x] = new Palette(x+16);
            }

        }

        private void PaletteViewer_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) { }
                
        private void Display(Graphics g)
        {
            unchecked
            {
                Rectangle rect;
                for (int x = 0; x < 16; x++)
                {
                    rect = new Rectangle(new Point(x * 16, 1), new Size(16, 16));
                    g.FillRectangle(new SolidBrush(bgPalettes[x].GetColor()), rect);
                }
                for (int x = 0; x < 16; x++)
                {
                    rect = new Rectangle(new Point(x * 16, 17), new Size(16, 16));
                    g.FillRectangle(new SolidBrush(spritePalettes[x].GetColor()), rect);
                }
            }
        }

        private void PaletteViewer_Paint(object sender, PaintEventArgs e)
        {
            Display(e.Graphics);
        }
    }
}
