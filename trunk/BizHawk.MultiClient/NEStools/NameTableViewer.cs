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

        private void Display(Graphics g)
        {
            unchecked
            {
                g.DrawImage(nametables, 1, 1);
            }
        }

        private void NameTableViewer_Paint(object sender, PaintEventArgs e)
        {
            Display(e.Graphics);
        }
    }
}
