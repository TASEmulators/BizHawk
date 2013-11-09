using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TI83KeyPad : Form, IToolForm
	{
		//TODO: if wndx/wndy are negative, load window on the right edge of emulator window

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }

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
			int x = 0;
			x++;
			int y = x;
			y++;

			var mappings = Global.Config.AllTrollers["TI83 Controller"];
			//Set button hotkey mapping into tooltips
			toolTip1.SetToolTip(ZERO, mappings["0"]);
			toolTip1.SetToolTip(ONE, mappings["1"]);
			toolTip1.SetToolTip(TWO, mappings["2"]);
			toolTip1.SetToolTip(THREE, mappings["3"]);
			toolTip1.SetToolTip(FOUR, mappings["4"]);
			toolTip1.SetToolTip(FIVE, mappings["5"]);
			toolTip1.SetToolTip(SIX, mappings["6"]);
			toolTip1.SetToolTip(SEVEN, mappings["7"]);
			toolTip1.SetToolTip(EIGHT, mappings["8"]);
			toolTip1.SetToolTip(NINE, mappings["9"]);
			toolTip1.SetToolTip(PERIOD, mappings["DOT"]);
			toolTip1.SetToolTip(ON, mappings["ON"]);
			toolTip1.SetToolTip(ENTER, mappings["ENTER"]);
			toolTip1.SetToolTip(UP, mappings["UP"]);
			toolTip1.SetToolTip(DOWN, mappings["DOWN"]);
			toolTip1.SetToolTip(LEFT, mappings["LEFT"]);
			toolTip1.SetToolTip(RIGHT, mappings["RIGHT"]);
			toolTip1.SetToolTip(PLUS, mappings["PLUS"]);
			toolTip1.SetToolTip(MINUS, mappings["MINUS"]);
			toolTip1.SetToolTip(MULTIPLY, mappings["MULTIPLY"]);
			toolTip1.SetToolTip(DIVIDE, mappings["DIVIDE"]);
			toolTip1.SetToolTip(CLEAR, mappings["CLEAR"]);
			toolTip1.SetToolTip(EXP, mappings["EXP"]);
			toolTip1.SetToolTip(DASH, mappings["DASH"]);
			toolTip1.SetToolTip(PARACLOSE, mappings["PARACLOSE"]);
			toolTip1.SetToolTip(PARAOPEN, mappings["PARAOPEN"]);
			toolTip1.SetToolTip(TAN, mappings["TAN"]);
			toolTip1.SetToolTip(VARS, mappings["VARS"]);
			toolTip1.SetToolTip(COS, mappings["COS"]);
			toolTip1.SetToolTip(PRGM, mappings["PRGM"]);
			toolTip1.SetToolTip(STAT, mappings["STAT"]);
			toolTip1.SetToolTip(MATRIX, mappings["MATRIX"]);
			toolTip1.SetToolTip(XT, mappings["X"]);
			toolTip1.SetToolTip(STO, mappings["STO"]);
			toolTip1.SetToolTip(LN, mappings["LN"]);
			toolTip1.SetToolTip(LOG, mappings["LOG"]);
			toolTip1.SetToolTip(SQUARED, mappings["SQUARED"]);
			toolTip1.SetToolTip(NEG1, mappings["NEG1"]);
			toolTip1.SetToolTip(MATH, mappings["MATH"]);
			toolTip1.SetToolTip(ALPHA, mappings["ALPHA"]);
			toolTip1.SetToolTip(GRAPH, mappings["GRAPH"]);
			toolTip1.SetToolTip(TRACE, mappings["TRACE"]);
			toolTip1.SetToolTip(ZOOM, mappings["ZOOM"]);
			toolTip1.SetToolTip(WINDOW, mappings["WINDOW"]);
			toolTip1.SetToolTip(YEQUAL, mappings["Y"]);
			toolTip1.SetToolTip(SECOND, mappings["SECOND"]);
			toolTip1.SetToolTip(MODE, mappings["MODE"]);
			toolTip1.SetToolTip(DEL, mappings["DEL"]);
			toolTip1.SetToolTip(COMMA, mappings["COMMA"]);
			toolTip1.SetToolTip(SIN, mappings["SIN"]);
		}

		public void StopToolTips()
		{
			toolTip1.RemoveAll();
		}

		public void UpdateValues()
		{
			if (!(Global.Emulator is TI83))
			{
				Close();
			}
		}

		public void Restart()
		{
			if (!(Global.Emulator is TI83))
			{
				Close();
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SaveConfigSettings()
		{
			Global.Config.TI83KeyPadWndx = Location.X;
			Global.Config.TI83KeyPadWndy = Location.Y;
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
			Global.ClickyVirtualPadController.Click("ENTER");
		}

		private void button43_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DASH");
		}

		private void button39_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("2");
		}

		private void ONE_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("1");
		}

		private void THREE_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("3");
		}

		private void FOUR_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("4");
		}

		private void FIVE_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("5");
		}

		private void SIX_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("6");
		}

		private void SEVEN_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("7");
		}

		private void EIGHT_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("8");
		}

		private void NINE_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("9");
		}

		private void ON_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ON");
		}

		private void STO_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("STO");
		}

		private void PLUS_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PLUS");
		}

		private void LN_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LN");
		}

		private void MINUS_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MINUS");
		}

		private void LOG_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LOG");
		}

		private void MULTIPLY_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MULTIPLY");
		}

		private void button26_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("SQUARED");
		}

		private void button25_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("COMMA");
		}

		private void button24_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PARAOPEN");
		}

		private void button23_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PARACLOSE");
		}

		private void button22_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DIVIDE");
		}

		private void button17_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("NEG1");
		}

		private void button18_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("SIN");
		}

		private void button19_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("COS");
		}

		private void button20_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("TAN");
		}

		private void button21_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("EXP");
		}

		private void button12_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MATH");
		}

		private void button13_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MATRIX");
		}

		private void button14_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PRGM");
		}

		private void button15_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("VARS");
		}

		private void button16_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("CLEAR");
		}

		private void button11_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ALPHA");
		}

		private void button4_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("X");
		}

		private void button10_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("STAT");
		}

		private void button5_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("2ND");
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MODE");
		}

		private void button3_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DEL");
		}

		private void button47_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LEFT");
		}

		private void button49_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DOWN");
		}

		private void button48_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("RIGHT");
		}

		private void button50_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("UP");
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("Y");
		}

		private void button6_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("WINDOW");
		}

		private void button7_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ZOOM");
		}

		private void button8_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("TRACE");
		}

		private void button9_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("GRAPH");
		}

		private void PERIOD_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DOT");
		}

		private void showHotkToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83ToolTips ^= true;

			if (Global.Config.TI83ToolTips)
			{
				SetToolTips();
			}
			else
			{
				StopToolTips();
			}
		}

		private void ZERO_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("0");
		}
	}
}
