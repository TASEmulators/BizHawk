using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Calculators.TI83;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed partial class TI83KeyPad : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Resources.CalculateIcon;

		[RequiredService]
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		public TI83Common Emu { get; private set; }

		protected override string WindowTitleStatic => "TI-83 Virtual KeyPad";

		public TI83KeyPad()
		{
			InitializeComponent();
			Icon = ToolIcon;
			LeftButton.Image = Resources.WhiteTriLeft;
			RightButton.Image = Resources.WhiteTriRight;
			DownButton.Image = Resources.WhiteTriDown;
			UpButton.Image = Resources.WhiteTriUp;
			if (OSTailoredCode.IsUnixHost) MinimumSize = (MaximumSize += new Size(48, 32)); // also updates current size
		}

		[ConfigPersist]
		public bool TI83ToolTips { get; set; } = true;

		private void TI83KeyPad_Load(object sender, EventArgs e)
		{
			if (TI83ToolTips)
			{
				SetToolTips();
			}
		}

		private void KeyClick(string name)
		{
			InputManager.ClickyVirtualPadController.Click(name);
		}

		private void SetToolTips()
		{
			// Set button hotkey mapping into tooltips
			var mappings = Config.AllTrollers["TI83 Controller"];
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

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ShowHotkeysMenuItem.Checked = TI83ToolTips;
		}

		private void ShowHotkeysMenuItem_Click(object sender, EventArgs e)
		{
			TI83ToolTips = !TI83ToolTips;
			if (TI83ToolTips)
			{
				SetToolTips();
			}
			else
			{
				StopToolTips();
			}
		}

		private void EnterButton_Click(object sender, EventArgs e) => KeyClick("ENTER");
		private void DashButton_Click(object sender, EventArgs e) => KeyClick("DASH");
		private void OneButton_Click(object sender, EventArgs e) => KeyClick("1");
		private void TwoButton_Click(object sender, EventArgs e) => KeyClick("2");
		private void ThreeButton_Click(object sender, EventArgs e) => KeyClick("3");

		private void FourButton_Click(object sender, EventArgs e) => KeyClick("4");

		private void FiveButton_Click(object sender, EventArgs e) => KeyClick("5");
		private void SixButton_Click(object sender, EventArgs e) => KeyClick("6");
		private void SevenButton_Click(object sender, EventArgs e) => KeyClick("7");
		private void EightButton_Click(object sender, EventArgs e) => KeyClick("8");
		private void NineButton_Click(object sender, EventArgs e) => KeyClick("9");
		private void OnButton_Click(object sender, EventArgs e) => KeyClick("ON");
		private void StoButton_Click(object sender, EventArgs e) => KeyClick("STO");
		private void PlusButton_Click(object sender, EventArgs e) => KeyClick("PLUS");
		private void LnButton_Click(object sender, EventArgs e) => KeyClick("LN");
		private void MinusButton_Click(object sender, EventArgs e) => KeyClick("MINUS");
		private void LogButton_Click(object sender, EventArgs e) => KeyClick("LOG");
		private void MultiplyButton_Click(object sender, EventArgs e) => KeyClick("MULTIPLY");
		private void SquaredButton_Click(object sender, EventArgs e) => KeyClick("SQUARED");
		private void CommaButton_Click(object sender, EventArgs e) => KeyClick("COMMA");
		private void ParaOpenButton_Click(object sender, EventArgs e) => KeyClick("PARAOPEN");
		private void ParaCloseButton_Click(object sender, EventArgs e) => KeyClick("PARACLOSE");
		private void DivideButton_Click(object sender, EventArgs e) => KeyClick("DIVIDE");
		private void Neg1Button_Click(object sender, EventArgs e) => KeyClick("NEG1");
		private void SinButton_Click(object sender, EventArgs e) => KeyClick("SIN");
		private void CosButton_Click(object sender, EventArgs e) => KeyClick("COS");
		private void TanButton_Click(object sender, EventArgs e) => KeyClick("TAN");
		private void ExpButton_Click(object sender, EventArgs e) => KeyClick("EXP");
		private void MathButton_Click(object sender, EventArgs e) => KeyClick("MATH");
		private void MatrixButton_Click(object sender, EventArgs e) => KeyClick("MATRIX");
		private void ProgamButton_Click(object sender, EventArgs e) => KeyClick("PRGM");
		private void VarsButton_Click(object sender, EventArgs e) => KeyClick("VARS");
		private void ClearButton_Click(object sender, EventArgs e) => KeyClick("CLEAR");
		private void AlphaButton_Click(object sender, EventArgs e) => KeyClick("ALPHA");
		private void XButton_Click(object sender, EventArgs e) => KeyClick("X");
		private void StatButton_Click(object sender, EventArgs e) => KeyClick("STAT");
		private void SecondButton_Click(object sender, EventArgs e) => KeyClick("2ND");
		private void ModeButton_Click(object sender, EventArgs e) => KeyClick("MODE");
		private void DelButton_Click(object sender, EventArgs e) => KeyClick("DEL");
		private void LeftButton_Click(object sender, EventArgs e) => KeyClick("LEFT");
		private void DownButton_Click(object sender, EventArgs e) => KeyClick("DOWN");
		private void RightButton_Click(object sender, EventArgs e) => KeyClick("RIGHT");
		private void UpButton_Click(object sender, EventArgs e) => KeyClick("UP");
		private void YButton_Click(object sender, EventArgs e) => KeyClick("Y");
		private void WindowButton_Click(object sender, EventArgs e) => KeyClick("WINDOW");
		private void ZoomButton_Click(object sender, EventArgs e) => KeyClick("ZOOM");
		private void TraceButton_Click(object sender, EventArgs e) => KeyClick("TRACE");
		private void GraphButton_Click(object sender, EventArgs e) => KeyClick("GRAPH");
		private void PeriodButton_Click(object sender, EventArgs e) => KeyClick("DOT");
		private void ZeroButton_Click(object sender, EventArgs e) => KeyClick("0");
	}
}
