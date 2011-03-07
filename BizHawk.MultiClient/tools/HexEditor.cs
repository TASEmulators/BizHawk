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

        int defaultWidth;
        int defaultHeight;
        
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

        public void UpdateValues()
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            MemoryViewer.Refresh();
        }

        public void Restart()
        {
            SetMemoryDomainMenu(); //Calls update routines
            MemoryViewer.ResetScrollBar();
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
            enToolStripMenuItem.Checked = MemoryViewer.BigEndian;
            switch (MemoryViewer.GetDataSize())
            {
                default:
                case 1:
                    byteToolStripMenuItem.Checked = true;
                    byteToolStripMenuItem1.Checked = false;
                    byteToolStripMenuItem2.Checked = false;
                    break;
                case 2:
                    byteToolStripMenuItem.Checked = false;
                    byteToolStripMenuItem1.Checked = true;
                    byteToolStripMenuItem2.Checked = false;
                    break;
                case 4:
                    byteToolStripMenuItem.Checked = false;
                    byteToolStripMenuItem1.Checked = false;
                    byteToolStripMenuItem2.Checked = true;
                    break;
            }
        }

        private void SetMemoryDomain(int pos)
        {
            if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
            {
                MemoryViewer.SetMemoryDomain(Global.Emulator.MemoryDomains[pos]);
            }
            UpdateDomainString();
            MemoryViewer.ResetScrollBar();
        }

        private void UpdateDomainString()
        {
            string memoryDomain = MemoryViewer.GetMemoryDomainStr();
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
        }

        private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO
        }

        

        private void HexEditor_Resize(object sender, EventArgs e)
        {
            MemoryViewer.SetUpScrollBar();
            MemoryViewer.Refresh();
        }

        private void byteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryViewer.SetDataSize(1);
        }

        private void byteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MemoryViewer.SetDataSize(2);
        }

        private void byteToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MemoryViewer.SetDataSize(4);
        }

        private void enToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryViewer.BigEndian ^= true;
        }

        private void MemoryViewer_Paint(object sender, PaintEventArgs e)
        {

        }

        
    }
}
