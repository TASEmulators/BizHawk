using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Calculator;

namespace BizHawk.MultiClient
{
    public partial class TI83KeyPad : Form
    {
        //TODO: if wndx/wndy are negative, load window on the right edge of emulator window

        public TI83KeyPad()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
        }

        private void TI83KeyPad_Load(object sender, EventArgs e)
        {
            if (Global.Config.TI83KeypadSaveWindowPosition && Global.Config.TI83KeyPadWndx >= 0 && Global.Config.TI83KeyPadWndy >= 0)
                Location = new Point(Global.Config.TI83KeyPadWndx, Global.Config.TI83KeyPadWndy);
        }

        public void UpdateValues()
        {
        }

        public void Restart()
        {
            if (!(Global.Emulator is TI83))
                this.Close();
            if (!this.IsHandleCreated || this.IsDisposed) return;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveConfigSettings()
        {
            Global.Config.TI83KeyPadWndx = this.Location.X;
            Global.Config.TI83KeyPadWndy = this.Location.Y;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.TI83KeypadSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.TI83KeypadSaveWindowPosition;
        }
    }
}
