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
            if (Global.Config.TI83ToolTips)
                SetToolTips();
        }

        private void SetToolTips()
        {
            //Set button hotkey mapping into tooltips
            toolTip1.SetToolTip(ZERO, Global.Config.TI83Controller[0]._0);
            toolTip1.SetToolTip(ONE, Global.Config.TI83Controller[0]._1);
            toolTip1.SetToolTip(TWO, Global.Config.TI83Controller[0]._2);
            toolTip1.SetToolTip(THREE, Global.Config.TI83Controller[0]._3);
            toolTip1.SetToolTip(FOUR, Global.Config.TI83Controller[0]._4);
            toolTip1.SetToolTip(FIVE, Global.Config.TI83Controller[0]._5);
            toolTip1.SetToolTip(SIX, Global.Config.TI83Controller[0]._6);
            toolTip1.SetToolTip(SEVEN, Global.Config.TI83Controller[0]._7);
            toolTip1.SetToolTip(EIGHT, Global.Config.TI83Controller[0]._8);
            toolTip1.SetToolTip(NINE, Global.Config.TI83Controller[0]._9);
            toolTip1.SetToolTip(PERIOD, Global.Config.TI83Controller[0].DOT);
            toolTip1.SetToolTip(ON, Global.Config.TI83Controller[0].ON);
            toolTip1.SetToolTip(ENTER, Global.Config.TI83Controller[0].ENTER);
            toolTip1.SetToolTip(UP, Global.Config.TI83Controller[0].UP);
            toolTip1.SetToolTip(DOWN, Global.Config.TI83Controller[0].DOWN);
            toolTip1.SetToolTip(LEFT, Global.Config.TI83Controller[0].LEFT);
            toolTip1.SetToolTip(RIGHT, Global.Config.TI83Controller[0].RIGHT);
            toolTip1.SetToolTip(PLUS, Global.Config.TI83Controller[0].PLUS);
            toolTip1.SetToolTip(MINUS, Global.Config.TI83Controller[0].MINUS);
            toolTip1.SetToolTip(MULTIPLY, Global.Config.TI83Controller[0].MULTIPLY);
            toolTip1.SetToolTip(DIVIDE, Global.Config.TI83Controller[0].DIVIDE);
            toolTip1.SetToolTip(CLEAR, Global.Config.TI83Controller[0].CLEAR);
            toolTip1.SetToolTip(EXP, Global.Config.TI83Controller[0].EXP);
            toolTip1.SetToolTip(DASH, Global.Config.TI83Controller[0].DASH);
            toolTip1.SetToolTip(PARACLOSE, Global.Config.TI83Controller[0].PARACLOSE);
            toolTip1.SetToolTip(PARAOPEN, Global.Config.TI83Controller[0].PARAOPEN);
            toolTip1.SetToolTip(TAN, Global.Config.TI83Controller[0].TAN);
            toolTip1.SetToolTip(VARS, Global.Config.TI83Controller[0].VARS);
            toolTip1.SetToolTip(COS, Global.Config.TI83Controller[0].COS);
            toolTip1.SetToolTip(PRGM, Global.Config.TI83Controller[0].PRGM);
            toolTip1.SetToolTip(STAT, Global.Config.TI83Controller[0].STAT);
            toolTip1.SetToolTip(MATRIX, Global.Config.TI83Controller[0].MATRIX);
            toolTip1.SetToolTip(XT, Global.Config.TI83Controller[0].X);
            toolTip1.SetToolTip(STO, Global.Config.TI83Controller[0].STO);
            toolTip1.SetToolTip(LN, Global.Config.TI83Controller[0].LN);
            toolTip1.SetToolTip(LOG, Global.Config.TI83Controller[0].LOG);
            toolTip1.SetToolTip(SQUARED, Global.Config.TI83Controller[0].SQUARED);
            toolTip1.SetToolTip(NEG1, Global.Config.TI83Controller[0].NEG1);
            toolTip1.SetToolTip(MATH, Global.Config.TI83Controller[0].MATH);
            toolTip1.SetToolTip(ALPHA, Global.Config.TI83Controller[0].ALPHA);
            toolTip1.SetToolTip(GRAPH, Global.Config.TI83Controller[0].GRAPH);
            toolTip1.SetToolTip(TRACE, Global.Config.TI83Controller[0].TRACE);
            toolTip1.SetToolTip(ZOOM, Global.Config.TI83Controller[0].ZOOM);
            toolTip1.SetToolTip(WINDOW, Global.Config.TI83Controller[0].WINDOW);
            toolTip1.SetToolTip(YEQUAL, Global.Config.TI83Controller[0].Y);
            toolTip1.SetToolTip(SECOND, Global.Config.TI83Controller[0].SECOND);
            toolTip1.SetToolTip(MODE, Global.Config.TI83Controller[0].MODE);
            toolTip1.SetToolTip(DEL, Global.Config.TI83Controller[0].DEL);
            toolTip1.SetToolTip(COMMA, Global.Config.TI83Controller[0].COMMA);
            toolTip1.SetToolTip(SIN, Global.Config.TI83Controller[0].SIN);
        }

        public void StopToolTips()
        {
            toolTip1.RemoveAll();
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
            showHotkToolStripMenuItem.Checked = Global.Config.TI83ToolTips;
        }

        private void button42_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("ENTER");
        }

        private void button43_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("DASH");
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

        private void PERIOD_Click(object sender, EventArgs e)
        {
            Global.Emulator.Controller.ForceButton("DOT");
        }

        private void showHotkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.TI83ToolTips ^= true;
            if (Global.Config.TI83ToolTips == true)
                SetToolTips();
            else
                StopToolTips();
        }
    }
}
