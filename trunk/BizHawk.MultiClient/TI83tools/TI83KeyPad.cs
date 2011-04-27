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
            Global.Emulator.Controller.ForceButton("ENTER");
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

        private void ON_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("ON");
        }

        private void STO_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("STO");
        }

        private void PLUS_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("PLUS");
        }

        private void LN_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("LN");
        }

        private void MINUS_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("MINUS");
        }

        private void LOG_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("LOG");
        }

        private void MULTIPLY_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("MULTIPLY");
        }

        private void button26_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("SQUARED");
        }

        private void button25_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("COMMA");
        }

        private void button24_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("PARAOPEN");
        }

        private void button23_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("PARACLOSE");
        }

        private void button22_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("DIVIDE");
        }

        private void button17_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("NEG1");
        }

        private void button18_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("SIN");
        }

        private void button19_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("COS");
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("TAN");
        }

        private void button21_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("EXP");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("MATH");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("MATRIX");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("PRGM");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("VARS");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("CLEAR");
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("ALPHA");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("X");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("STAT");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("2ND");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("MODE");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("DEL");
        }

        private void button47_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("LEFT");
        }

        private void button49_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("DOWN");
        }

        private void button48_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("RIGHT");
        }

        private void button50_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("UP");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("Y");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("WINDOW");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("ZOOM");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("TRACE");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("GRAPH");
        }
    }
}
