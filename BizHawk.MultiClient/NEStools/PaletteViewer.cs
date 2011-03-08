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
                value = val;
                color = Color.FromArgb(val);
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
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PaletteViewer_MouseMove);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PaletteViewer_MouseClick);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PaletteViewer_KeyDown);

            for (int x = 0; x < 16; x++)
            {
                bgPalettes[x] = new Palette(x);
                spritePalettes[x] = new Palette(x);
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
                        
        private void PaletteViewer_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void PaletteViewer_MouseClick(object sender, MouseEventArgs e)
        {

        }
        /*
        //adelikat: Using my own approximation of the NES color palette until we have a decent palette system
        private Color GetColorByValue(int value)
        {
            switch (value)
            {
                case 0x00:
                    return Color.White;
                case 0x01:
                    return Color.LightBlue;
                case 0x02:
                    return Color.Blue;
                case 0x03:
                    return Color.DarkBlue;
                case 0x04:
                    return Color.Magenta;
                case 0x05:
                    return Color.OrangeRed;
                case 0x06:
                    return Color.Red;
                case 0x07:
                    return Color.DarkRed;
                case 0x08:
                    return Color.Brown;
                case 0x09:
                    return Color.DarkGreen;
                case 0x0A:
                    return Color.Green;
                case 0x0B:
                    return Color.LightGreen;
                case 0x0C:
                    return Color.Aqua;
                case 0x0D:
                    return Color.DarkGray;
                case 0x0E:
                    return Color.Gray;
                case 0x0F:
                    return Color.LightGray;
                default:
                    return Color.Black;
            }
        }
        */
    }
}
