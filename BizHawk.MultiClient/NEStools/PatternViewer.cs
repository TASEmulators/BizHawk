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

        public PatternViewer()
        {
            pSize = new Size(256, 128);
            pattern = new Bitmap(pSize.Width, pSize.Height);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            this.Size = pSize;
            this.BackColor = Color.White;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PatternViewer_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PatternViewer_KeyDown);

            for (int x = 0; x < pattern.Size.Width; x++)
            {
                for (int y = 0; y < pattern.Size.Height; y++)
                {
                    pattern.SetPixel(x, y, Color.Black);
                }
            }
        }

        private void PatternViewer_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) { }
                
        private void Display(Graphics g)
        {
            unchecked
            {
                g.DrawImage(pattern, 1, 1);
            }
        }

        private void PatternViewer_Paint(object sender, PaintEventArgs e)
        {
            Display(e.Graphics);
        }
    }
}
