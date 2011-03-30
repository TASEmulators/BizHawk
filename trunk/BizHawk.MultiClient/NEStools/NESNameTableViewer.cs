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
    public partial class NESNameTableViewer : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        NES Nes;

        public NESNameTableViewer()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
        }

        private void SaveConfigSettings()
        {
            Global.Config.NESNameTableWndx = this.Location.X;
            Global.Config.NESNameTableWndy = this.Location.Y;
        }

        public void UpdateValues()
        {
            if (!(Global.Emulator is NES)) return;
            if (!this.IsHandleCreated || this.IsDisposed) return;


        }

        public void Restart()
        {
            if (!(Global.Emulator is NES)) this.Close();
            Nes = Global.Emulator as NES;
        }

        private void NESNameTableViewer_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Config.NESNameTableSaveWindowPosition && Global.Config.NESNameTableWndx >= 0 && Global.Config.NESNameTableWndy >= 0)
                this.Location = new Point(Global.Config.NESNameTableWndx, Global.Config.NESNameTableWndy);

            Nes = Global.Emulator as NES;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.AutoLoadNESNameTable ^= true;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.NESNameTableSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESNameTable;
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESNameTableSaveWindowPosition;
        }
    }
}
