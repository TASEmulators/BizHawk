using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
    public partial class HexEditor : Form
    {
        //TODO:
        //Find text box - autohighlights matches, and shows total matches
        //Users can customize background, & text colors
        //Tool strip
        //Text box showing currently highlighted address(es) & total
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
            if (Global.Config.HexEditorSaveWindowPosition)
            {
                Global.Config.HexEditorWndx = this.Location.X;
                Global.Config.HexEditorWndy = this.Location.Y;
                Global.Config.HexEditorWidth = this.Right - this.Left;
                Global.Config.HexEditorHeight = this.Bottom - this.Top;
            }
        }

        private void HexEditor_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;
            if (Global.Config.HexEditorSaveWindowPosition)
            {
                if (Global.Config.HexEditorWndx >= 0 && Global.Config.HexEditorWndy >= 0)
                    this.Location = new Point(Global.Config.HexEditorWndx, Global.Config.HexEditorWndy);

                if (Global.Config.HexEditorWidth >= 0 && Global.Config.HexEditorHeight >= 0)
                {
                    this.Size = new System.Drawing.Size(Global.Config.HexEditorWidth, Global.Config.HexEditorHeight);
                }
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

            if (MemoryViewer.GetHighlightedAddress() >= 0)
                addToRamWatchToolStripMenuItem1.Enabled = true;
            else
                addToRamWatchToolStripMenuItem1.Enabled = false;
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

        public void GoToAddress(int address)
        {
            if (address < MemoryViewer.GetSize())
            {
                MemoryViewer.SetHighlighted(address);
                MemoryViewer.Refresh();
            }
        }

        private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputPrompt i = new InputPrompt();
            i.Text = "Go to Address";
            i.SetMessage("Enter a hexadecimal value");
            i.ShowDialog();

            if (i.UserOK)
            {
                if (InputValidate.IsValidHexNumber(i.UserText))
                {
                    GoToAddress(int.Parse(i.UserText, NumberStyles.HexNumber));
                }
            }
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

        private void AddToRamWatch()
        {
            //Add to RAM Watch
            int address = MemoryViewer.GetPointedAddress();
            if (address >= 0)
            {
                Watch w = new Watch();
                w.address = address;

                switch (MemoryViewer.GetDataSize())
                {
                    default:
                    case 1:
                        w.type = atype.BYTE;
                        break;
                    case 2:
                        w.type = atype.WORD;
                        break;
                    case 4:
                        w.type = atype.DWORD;
                        break;
                }

                w.bigendian = MemoryViewer.BigEndian;
                w.signed = asigned.HEX;

                Global.MainForm.LoadRamWatch();
                Global.MainForm.RamWatch1.AddWatch(w);
            }
        }

        private void MemoryViewer_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            AddToRamWatch();
        }

        private void pokeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int p = MemoryViewer.GetPointedAddress();
            if (p >= 0)
            {
                InputPrompt i = new InputPrompt();
                i.Text = "Poke " + String.Format("{0:X}", p);
                i.SetMessage("Enter a hexadecimal value");
                i.ShowDialog();

                if (i.UserOK)
                {
                    if (InputValidate.IsValidHexNumber(i.UserText))
                    {
                        int value = int.Parse(i.UserText, NumberStyles.HexNumber);
                        MemoryViewer.HighlightPointed();
                        MemoryViewer.PokeHighlighted(value);
                    }
                }
            }
        }

        private void addToRamWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddToRamWatch();
        }

        private void addToRamWatchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddToRamWatch();
        }

        private void saveWindowsSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.HexEditorSaveWindowPosition ^= true;
        }

        private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadHexEditor;
            saveWindowsSettingsToolStripMenuItem.Checked = Global.Config.HexEditorSaveWindowPosition;
        }

        
    }
}
