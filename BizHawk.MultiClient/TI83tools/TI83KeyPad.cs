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

        private void button42_Click(object sender, EventArgs e)
        {

        }

        private void button43_Click(object sender, EventArgs e)
        {

        }

        private void button39_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("2");
        }

        private void ONE_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("1");
        }

        private void THREE_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("3");
        }

        private void FOUR_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("4");
        }

        private void FIVE_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("5");
        }

        private void SIX_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("6");
        }

        private void SEVEN_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("7");
        }

        private void EIGHT_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("8");
        }

        private void NINE_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("9");
        }
    }
}
