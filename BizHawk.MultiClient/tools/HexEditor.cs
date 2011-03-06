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
        //TODO:
        //Everything

        int defaultWidth;
        int defaultHeight;
        MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
        
        public HexEditor()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
        }

        public void SaveConfigSettings()
        {
            Global.Config.HexEditorWndx = this.Location.X;
            Global.Config.HexEditorWndy = this.Location.Y;
            Global.Config.HexEditorWidth = this.Right - this.Left;
            Global.Config.HexEditorHeight = this.Bottom - this.Top;
        }

        private void HexEditor_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Config.HexEditorWndx >= 0 && Global.Config.HexEditorWndy >= 0)
                this.Location = new Point(Global.Config.HexEditorWndx, Global.Config.HexEditorWndy);

            if (Global.Config.HexEditorWidth >= 0 && Global.Config.HexEditorHeight >= 0)
            {
                this.Size = new System.Drawing.Size(Global.Config.HexEditorWidth, Global.Config.HexEditorHeight);
            }
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

        public void UpdateValues()
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            //TODO
        }

        public void Restart()
        {
            //TODO
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }
    }
}
