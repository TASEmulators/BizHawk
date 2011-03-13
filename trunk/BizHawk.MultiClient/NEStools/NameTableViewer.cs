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
        public NameTableViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            this.Size = new Size(256, 224);
            this.BackColor = Color.White;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.NameTableViewer_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NameTableViewer_KeyDown);
        }

        private void NameTableViewer_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) { }
                
        private void Display(Graphics g)
        {
            unchecked
            {

            }
        }

        private void NameTableViewer_Paint(object sender, PaintEventArgs e)
        {
            Display(e.Graphics);
        }
    }
}
