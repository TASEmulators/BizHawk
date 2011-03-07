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
        //Read only the memory in the visible portion
        //Find text box - autohighlights matches, and shows total matches
        //Implement Goto address
        //Users can customize background, & text colors
        //Context menu - Poke, Freeze/Unfreeze, Watch
        //Tool strip
        //Double click addres = send to ram watch
        //Add to Ram Watch menu item, enabled conditionally on if any address is highlighted
        //Text box showing currently highlighted address(es) & total
        //Typing legit hex values = memory poke
        //Show num addresses in group box title (show "address" if 1 address)
        //big font for currently mouse over'ed value?

        Font font = new Font("Courier New", 10);
        Brush regBrush = Brushes.Black;
        int RowsVisible = 0;

        const string HEADER = "       0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";

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

            SetMemoryDomainMenu();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MemoryViewer_Paint(object sender, PaintEventArgs e)
        {
            unchecked
            {
                int row = 0;
                int rowX = 8; 
                int rowY = 16;
                int rowYoffset = 20;
                string rowStr;

                e.Graphics.DrawString(HEADER, font, regBrush, new Point(rowX, rowY));
                e.Graphics.DrawLine(new Pen(regBrush), MemoryViewer.Left + 38, MemoryViewer.Top, MemoryViewer.Left + 38, MemoryViewer.Bottom - 40);
                e.Graphics.DrawLine(new Pen(regBrush), MemoryViewer.Left, 34, MemoryViewer.Right - 16, 34);

                for (int i = 0; i < RowsVisible; i++)
                {
                    row = i+vScrollBar1.Value;
                    rowStr = String.Format("{0:X4}", row*16) + "  "; //TODO: num digits based on size of domain
                    for (int j = 0; j < 16; j++)
                    {
                        rowStr += String.Format("{0:X2}", Domain.PeekByte((row*16)+j)) + " "; //TODO: format based on data size
                    }

                    e.Graphics.DrawString(rowStr, font, regBrush, new Point(rowX, (rowY*(i+1))+rowYoffset));
                }
            }
        }

        public void UpdateValues()
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            MemoryViewer.Refresh();
        }

        public void Restart()
        {
            SetMemoryDomainMenu(); //Calls update routines
            SetUpScrollBar();
            MemoryViewer.Refresh();
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }

        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.AutoLoadHexEditor ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadHexEditor;
        }

        private void SetMemoryDomain(int pos)
        {
            if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
            {
                Domain = Global.Emulator.MemoryDomains[pos];
            }
            UpdateDomainString();
            SetUpScrollBar();
            vScrollBar1.Value = 0;
            MemoryViewer.Refresh();
        }

        private void UpdateDomainString()
        {
            string memoryDomain = Domain.ToString();
            string systemID = Global.Emulator.SystemId;
            MemoryViewer.Text = systemID + " " + memoryDomain;
        }

        private void SetMemoryDomainMenu()
        {
            memoryDomainsToolStripMenuItem.DropDownItems.Clear();
            if (Global.Emulator.MemoryDomains.Count > 0)
            {
                for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
                {
                    string str = Global.Emulator.MemoryDomains[x].ToString();
                    var item = new ToolStripMenuItem();
                    item.Text = str;
                    {
                        int z = x;
                        item.Click += (o, ev) => SetMemoryDomain(z);
                    }
                    if (x == 0)
                    {
                        //item.Checked = true; //TODO: figure out how to check/uncheck these in SetMemoryDomain
                        SetMemoryDomain(x);
                    }
                    memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
                }
            }
            else
                memoryDomainsToolStripMenuItem.Enabled = false;

            SetUpScrollBar();
            vScrollBar1.Value = 0;
            MemoryViewer.Refresh();
        }

        private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO
        }

        private void SetUpScrollBar()
        {
            RowsVisible = MemoryViewer.Height / 16;
            int totalRows = Domain.Size / 16;
            int MaxRows = (totalRows - RowsVisible) + 17;

            if (MaxRows > 0)
            {
                vScrollBar1.Visible = true;
                if (vScrollBar1.Value > MaxRows)
                    vScrollBar1.Value = MaxRows;
                vScrollBar1.Maximum = MaxRows;
            }
            else
                vScrollBar1.Visible = false;

        }

        private void HexEditor_Resize(object sender, EventArgs e)
        {
            SetUpScrollBar();
            MemoryViewer.Refresh();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            SetUpScrollBar();
            MemoryViewer.Refresh();
        }
    }
}
