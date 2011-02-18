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
    /// <summary>
    /// A winform designed to search through ram values
    /// </summary>
    public partial class RamSearch : Form
    {
        //TODO:
        //Save window position & Size
        //Menu Bar
        //Reset window position item
        int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;

        List<Watch> searchList = new List<Watch>();

        public RamSearch()
        {
            InitializeComponent();
        }

        private void RamSearch_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            SetTotal();

            for (int x = 0; x < Global.Emulator.MainMemory.Size; x++)
            {

            }

            if (Global.Config.RamSearchWndx >= 0 && Global.Config.RamSearchWndy >= 0)
                this.Location = new Point(Global.Config.RamSearchWndx, Global.Config.RamSearchWndy);

            if (Global.Config.RamSearchWidth >= 0 && Global.Config.RamSearchHeight >= 0)
            {
                this.Size = new System.Drawing.Size(Global.Config.RamSearchWidth, Global.Config.RamSearchHeight);
            }
        }

        private void SetTotal()
        {
            int x = Global.Emulator.MainMemory.Size;
            string str;
            if (x == 1)
                str = " address";
            else
                str = " addresses";
            TotalSearchLabel.Text = x.ToString() + str;
        }

        private void hackyAutoLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.Config.AutoLoadRamSearch == true)
            {
                Global.Config.AutoLoadRamSearch = false;
                hackyAutoLoadToolStripMenuItem.Checked = false;
            }
            else
            {
                Global.Config.AutoLoadRamSearch = true;
                hackyAutoLoadToolStripMenuItem.Checked = true;
            }
        }

        private void OpenSearchFile()
        {

        }

        private void SaveAs()
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSearchFile();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            OpenSearchFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void SpecificValueRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (SpecificValueRadio.Checked)
            {
                SpecificValueBox.Enabled = true;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = false;
            }
        }

        private void PreviousValueRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (PreviousValueRadio.Checked)
            {
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = false;
            }
        }

        private void SpecificAddressRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (SpecificAddressRadio.Checked)
            {
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = true;
                NumberOfChangesBox.Enabled = false;
            }
        }

        private void NumberOfChangesRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (NumberOfChangesRadio.Checked)
            {
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = true;
            }
        }

        private void DifferentByRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (DifferentByRadio.Checked)
                DifferentByBox.Enabled = true;
            else
                DifferentByBox.Enabled = false;
        }

        private void ModuloRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (ModuloRadio.Checked)
                ModuloBox.Enabled = true;
            else
                ModuloBox.Enabled = false;
        }

        private void WatchtoolStripButton1_Click(object sender, EventArgs e)
        {
            //TODO: get listview watch object and feed to ram watch

            if (!Global.MainForm.RamWatch1.IsDisposed)
                Global.MainForm.RamWatch1.Focus();
            else
            {
                Global.MainForm.RamWatch1 = new RamWatch();
                Global.MainForm.RamWatch1.Show();
            }
        }

        private void RamSearch_LocationChanged(object sender, EventArgs e)
        {
            Global.Config.RamSearchWndx = this.Location.X;
            Global.Config.RamSearchWndy = this.Location.Y;
        }

        private void RamSearch_Resize(object sender, EventArgs e)
        {
            Global.Config.RamSearchWidth = this.Right - this.Left;
            Global.Config.RamSearchHeight = this.Bottom - this.Top;
        }

        private void restoreOriginalWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }
    }
}
