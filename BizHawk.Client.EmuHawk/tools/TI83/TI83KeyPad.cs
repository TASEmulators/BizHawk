using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TI83KeyPad : ToolFormBase, IToolForm
	{
		[RequiredService]
		public TI83 Emu { get; private set; }

		public TI83KeyPad()
		{
			InitializeComponent();
			TopMost = Global.Config.TI83KeypadSettings.TopMost;
			Closing += (o, e) =>
				{
					Global.Config.TI83KeypadSettings.Wndx = Location.X;
					Global.Config.TI83KeypadSettings.Wndy = Location.Y;
				};
		}

		private void TI83KeyPad_Load(object sender, EventArgs e)
		{
			if (Global.Config.TI83KeypadSettings.UseWindowPosition)
			{
				Location = Global.Config.TI83KeypadSettings.WindowPosition;
			}

			if (Global.Config.TI83ToolTips)
			{
				SetToolTips();
			}
		}

		#region Public API

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return false; } }

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			// Do nothing
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			// Do nothing
		}

		#endregion

		private void SetToolTips()
		{
			// Set button hotkey mapping into tooltips
			var mappings = Global.Config.AllTrollers["TI83 Controller"];
			KeyPadToolTips.SetToolTip(ZeroButton, mappings["0"]);
			KeyPadToolTips.SetToolTip(OneButton, mappings["1"]);
			KeyPadToolTips.SetToolTip(TwoButton, mappings["2"]);
			KeyPadToolTips.SetToolTip(ThreeButton, mappings["3"]);
			KeyPadToolTips.SetToolTip(FourButton, mappings["4"]);
			KeyPadToolTips.SetToolTip(FiveButton, mappings["5"]);
			KeyPadToolTips.SetToolTip(SixButton, mappings["6"]);
			KeyPadToolTips.SetToolTip(SevenButton, mappings["7"]);
			KeyPadToolTips.SetToolTip(EightButton, mappings["8"]);
			KeyPadToolTips.SetToolTip(NineButton, mappings["9"]);
			KeyPadToolTips.SetToolTip(PeriodButton, mappings["DOT"]);
			KeyPadToolTips.SetToolTip(OnButton, mappings["ON"]);
			KeyPadToolTips.SetToolTip(EnterButton, mappings["ENTER"]);
			KeyPadToolTips.SetToolTip(UpButton, mappings["UP"]);
			KeyPadToolTips.SetToolTip(DownButton, mappings["DOWN"]);
			KeyPadToolTips.SetToolTip(LeftButton, mappings["LEFT"]);
			KeyPadToolTips.SetToolTip(RightButton, mappings["RIGHT"]);
			KeyPadToolTips.SetToolTip(PlusButton, mappings["PLUS"]);
			KeyPadToolTips.SetToolTip(MinusButton, mappings["MINUS"]);
			KeyPadToolTips.SetToolTip(MultiplyButton, mappings["MULTIPLY"]);
			KeyPadToolTips.SetToolTip(DivideButton, mappings["DIVIDE"]);
			KeyPadToolTips.SetToolTip(ClearButton, mappings["CLEAR"]);
			KeyPadToolTips.SetToolTip(ExpButton, mappings["EXP"]);
			KeyPadToolTips.SetToolTip(DashButton, mappings["DASH"]);
			KeyPadToolTips.SetToolTip(ParaOpenButton, mappings["PARAOPEN"]);
			KeyPadToolTips.SetToolTip(ParaCloseButton, mappings["PARACLOSE"]);
			KeyPadToolTips.SetToolTip(TanButton, mappings["TAN"]);
			KeyPadToolTips.SetToolTip(VarsButton, mappings["VARS"]);
			KeyPadToolTips.SetToolTip(CosButton, mappings["COS"]);
			KeyPadToolTips.SetToolTip(PrgmButton, mappings["PRGM"]);
			KeyPadToolTips.SetToolTip(StatButton, mappings["STAT"]);
			KeyPadToolTips.SetToolTip(MatrixButton, mappings["MATRIX"]);
			KeyPadToolTips.SetToolTip(XButton, mappings["X"]);
			KeyPadToolTips.SetToolTip(StoButton, mappings["STO"]);
			KeyPadToolTips.SetToolTip(LnButton, mappings["LN"]);
			KeyPadToolTips.SetToolTip(LogButton, mappings["LOG"]);
			KeyPadToolTips.SetToolTip(SquaredButton, mappings["SQUARED"]);
			KeyPadToolTips.SetToolTip(Neg1Button, mappings["NEG1"]);
			KeyPadToolTips.SetToolTip(MathButton, mappings["MATH"]);
			KeyPadToolTips.SetToolTip(AlphaButton, mappings["ALPHA"]);
			KeyPadToolTips.SetToolTip(GraphButton, mappings["GRAPH"]);
			KeyPadToolTips.SetToolTip(TraceButton, mappings["TRACE"]);
			KeyPadToolTips.SetToolTip(ZoomButton, mappings["ZOOM"]);
			KeyPadToolTips.SetToolTip(WindowButton, mappings["WINDOW"]);
			KeyPadToolTips.SetToolTip(YButton, mappings["Y"]);
			KeyPadToolTips.SetToolTip(SecondButton, mappings["SECOND"]);
			KeyPadToolTips.SetToolTip(ModeButton, mappings["MODE"]);
			KeyPadToolTips.SetToolTip(DelButton, mappings["DEL"]);
			KeyPadToolTips.SetToolTip(CommaButton, mappings["COMMA"]);
			KeyPadToolTips.SetToolTip(SinButton, mappings["SIN"]);
		}

		private void StopToolTips()
		{
			KeyPadToolTips.RemoveAll();
		}

		#region Events

		#region Menu

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ShowHotkeysMenuItem.Checked = Global.Config.TI83ToolTips;
			SaveWindowPositionMenuItem.Checked = Global.Config.TI83KeypadSettings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.TI83KeypadSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.TI83KeypadSettings.FloatingWindow;
		}

		private void ShowHotkeysMenuItem_Click(object sender, EventArgs e)
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

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83KeypadSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83KeypadSettings.TopMost ^= true;
			TopMost = Global.Config.TI83KeypadSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83KeypadSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl(Global.Config.TI83KeypadSettings.FloatingWindow);
		}

		#endregion

		#region Dialog and Controls

		private void EnterButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ENTER");
		}

		private void DashButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DASH");
		}

		private void OneButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("1");
		}

		private void TwoButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("2");
		}

		private void ThreeButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("3");
		}

		private void FourButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("4");
		}

		private void FiveButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("5");
		}

		private void SixButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("6");
		}

		private void SevenButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("7");
		}

		private void EightButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("8");
		}

		private void NineButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("9");
		}

		private void OnButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ON");
		}

		private void StoButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("STO");
		}

		private void PlusButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PLUS");
		}

		private void LnButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LN");
		}

		private void MinusButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MINUS");
		}

		private void LogButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LOG");
		}

		private void MultiplyButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MULTIPLY");
		}

		private void SquaredButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("SQUARED");
		}

		private void CommaButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("COMMA");
		}

		private void ParaOpenButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PARAOPEN");
		}

		private void ParaCloseButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PARACLOSE");
		}

		private void DivideButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DIVIDE");
		}

		private void Neg1Button_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("NEG1");
		}

		private void SinButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("SIN");
		}

		private void CosButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("COS");
		}

		private void TanButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("TAN");
		}

		private void ExpButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("EXP");
		}

		private void MathButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MATH");
		}

		private void MaxtrixButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MATRIX");
		}

		private void PrgmButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("PRGM");
		}

		private void VarsButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("VARS");
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("CLEAR");
		}

		private void AlphaButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ALPHA");
		}

		private void XButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("X");
		}

		private void StatButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("STAT");
		}

		private void SecondButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("2ND");
		}

		private void ModeButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("MODE");
		}

		private void DelButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DEL");
		}

		private void LeftButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("LEFT");
		}

		private void DownButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DOWN");
		}

		private void RightButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("RIGHT");
		}

		private void UpButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("UP");
		}

		private void YButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("Y");
		}

		private void WindowButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("WINDOW");
		}

		private void ZoomButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("ZOOM");
		}

		private void TraceButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("TRACE");
		}

		private void GraphButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("GRAPH");
		}

		private void PeriodButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("DOT");
		}

		private void ZeroButton_Click(object sender, EventArgs e)
		{
			Global.ClickyVirtualPadController.Click("0");
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl(Global.Config.TI83KeypadSettings.FloatingWindow);
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
