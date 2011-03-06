using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    public partial class HexEditor : Form
    {
        public HexEditor()
        {
            InitializeComponent();
        }

        private void HexEditor_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MemoryViewer_Paint(object sender, PaintEventArgs e)
        {
            // Create a local version of the graphics object for the PictureBox.
            Graphics g = e.Graphics;

            // Draw a string on the PictureBox.
            g.DrawString("This is a diagonal line drawn on the control",
                new Font("Arial",10), System.Drawing.Brushes.Blue, new Point(30,30));

            // Draw a line in the PictureBox.
            g.DrawLine(System.Drawing.Pens.Red, this.Left, this.Top,
                this.Right, this.Bottom);

        }
    }
}
