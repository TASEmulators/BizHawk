using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	//TODO: 
	//Remove AppendMapping and TruncateMapping functions

	public partial class InputConfig : Form
	{
		int prevWidth;
		int prevHeight;
		const string ControllerStr = "Configure Controllers - ";
		public static string[] SMSControlList = new string[] { "Up", "Down", "Left", "Right", "B1", "B2", "Pause", "Reset" };
		public static string[] PCEControlList = new string[] { "Up", "Down", "Left", "Right", "I", "II", "Run", "Select" };
		public static string[] GenesisControlList = new string[] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start", "X,T,0", "Y=", "Z" };
		public static string[] NESControlList = new string[] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" };
		public static string[] TI83ControlList = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "ON", 
			"ENTER", "Up", "Down", "Left", "Right", "+", "-", "Multiply", "Divide", "CLEAR", "^", "-", "(", ")", "TAN", "VARS", 
			"COS", "PRGM", "STAT", "Matrix", "X", "STO->", "LN", "LOG", "^2", "^-1", "MATH", "ALPHA", "GRAPH", "TRACE", "ZOOM", "WINDOW",
			"Y", "2nd", "MODE", "Del", ",", "SIN"}; //TODO: display shift / alpha names too, Also order these like on the calculator
		private ArrayList Labels;
		private ArrayList TextBoxes;
		private string CurSelectConsole;
		private int CurSelectController;
		private bool Changed;
		public InputConfig()
		{
			InitializeComponent();
			Labels = new ArrayList();
			TextBoxes = new ArrayList();
			Changed = false;
		}


		protected override void OnShown(EventArgs e)
		{
			Input.Instance.EnableIgnoreModifiers = true;
			base.OnShown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Input.Instance.EnableIgnoreModifiers = false;
		}


		private string AppendButtonMapping(string button, string oldmap)
		{
			//adelikat: Another relic, remove this
			//int x = oldmap.LastIndexOf(',');
			//if (x != -1)
			//    return oldmap.Substring(0, x + 2) + button;
			//else
			return button;
		}

		private void DoSMS()
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + "SMS / GG / SG-1000";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SMSController;

			int jpad = this.ControllComboBox.SelectedIndex;
			string[] ButtonMappings = new string[SMSControlList.Length];
			ButtonMappings[0] = Global.Config.SMSController[jpad].Up;
			ButtonMappings[1] = Global.Config.SMSController[jpad].Down;
			ButtonMappings[2] = Global.Config.SMSController[jpad].Left;
			ButtonMappings[3] = Global.Config.SMSController[jpad].Right;
			ButtonMappings[4] = Global.Config.SMSController[jpad].B1;
			ButtonMappings[5] = Global.Config.SMSController[jpad].B2;
			ButtonMappings[6] = Global.Config.SmsPause;
			ButtonMappings[7] = Global.Config.SmsReset;
			IDX_CONTROLLERENABLED.Checked = Global.Config.SMSController[jpad].Enabled;
			Changed = true;
			Labels.Clear();
			TextBoxes.Clear();
			for (int i = 0; i < SMSControlList.Length; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = SMSControlList[i];
				TempLabel.Location = new Point(8, 20 + (i * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(48, 20 + (i * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
			}
			Changed = true;
		}
		private void UpdateSMS(int prev)
		{
			ButtonsGroupBox.Controls.Clear();
			InputWidget TempBox;
			Label TempLabel;
			TempBox = TextBoxes[0] as InputWidget;
			Global.Config.SMSController[prev].Up = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].Up);
			TempBox.Dispose();
			TempBox = TextBoxes[1] as InputWidget;
			Global.Config.SMSController[prev].Down = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].Down);
			TempBox.Dispose();
			TempBox = TextBoxes[2] as InputWidget;
			Global.Config.SMSController[prev].Left = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].Left);
			TempBox.Dispose();
			TempBox = TextBoxes[3] as InputWidget;
			Global.Config.SMSController[prev].Right = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].Right);
			TempBox.Dispose();
			TempBox = TextBoxes[4] as InputWidget;
			Global.Config.SMSController[prev].B1 = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].B1);
			TempBox.Dispose();
			TempBox = TextBoxes[5] as InputWidget;
			Global.Config.SMSController[prev].B2 = AppendButtonMapping(TempBox.Text, Global.Config.SMSController[prev].B2);
			TempBox.Dispose();
			TempBox = TextBoxes[6] as InputWidget;
			Global.Config.SmsPause = AppendButtonMapping(TempBox.Text, Global.Config.SmsPause);
			TempBox.Dispose();
			TempBox = TextBoxes[7] as InputWidget;
			Global.Config.SmsReset = AppendButtonMapping(TempBox.Text, Global.Config.SmsReset);
			Global.Config.SMSController[prev].Enabled = IDX_CONTROLLERENABLED.Checked;
			TempBox.Dispose();
			for (int i = 0; i < SMSControlList.Length; i++)
			{
				TempLabel = Labels[i] as Label;
				TempLabel.Dispose();
			}
		}
		private void DoPCE()
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + "PCEjin / SGX";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.PCEngineController;
			int jpad = this.ControllComboBox.SelectedIndex;
			string[] ButtonMappings = new string[PCEControlList.Length];
			ButtonMappings[0] = Global.Config.PCEController[jpad].Up;
			ButtonMappings[1] = Global.Config.PCEController[jpad].Down;
			ButtonMappings[2] = Global.Config.PCEController[jpad].Left;
			ButtonMappings[3] = Global.Config.PCEController[jpad].Right;
			ButtonMappings[4] = Global.Config.PCEController[jpad].I;
			ButtonMappings[5] = Global.Config.PCEController[jpad].II;
			ButtonMappings[6] = Global.Config.PCEController[jpad].Run;
			ButtonMappings[7] = Global.Config.PCEController[jpad].Select;
			IDX_CONTROLLERENABLED.Checked = Global.Config.PCEController[jpad].Enabled;
			Labels.Clear();
			TextBoxes.Clear();
			for (int i = 0; i < PCEControlList.Length; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = PCEControlList[i];
				TempLabel.Location = new Point(8, 20 + (i * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(48, 20 + (i * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
			}
			Changed = true;
		}
		private void UpdatePCE(int prev)
		{
			ButtonsGroupBox.Controls.Clear();
			InputWidget TempBox;
			Label TempLabel;
			TempBox = TextBoxes[0] as InputWidget;
			Global.Config.PCEController[prev].Up = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Up);
			TempBox.Dispose();
			TempBox = TextBoxes[1] as InputWidget;
			Global.Config.PCEController[prev].Down = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Down);
			TempBox.Dispose();
			TempBox = TextBoxes[2] as InputWidget;
			Global.Config.PCEController[prev].Left = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Left);
			TempBox.Dispose();
			TempBox = TextBoxes[3] as InputWidget;
			Global.Config.PCEController[prev].Right = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Right);
			TempBox.Dispose();
			TempBox = TextBoxes[4] as InputWidget;
			Global.Config.PCEController[prev].I = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].I);
			TempBox.Dispose();
			TempBox = TextBoxes[5] as InputWidget;
			Global.Config.PCEController[prev].II = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].II);
			TempBox.Dispose();
			TempBox = TextBoxes[6] as InputWidget;
			Global.Config.PCEController[prev].Run = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Run);
			TempBox.Dispose();
			TempBox = TextBoxes[7] as InputWidget;
			Global.Config.PCEController[prev].Select = AppendButtonMapping(TempBox.Text, Global.Config.PCEController[prev].Select);
			TempBox.Dispose();
			Global.Config.PCEController[prev].Enabled = IDX_CONTROLLERENABLED.Checked;
			for (int i = 0; i < PCEControlList.Length; i++)
			{
				TempLabel = Labels[i] as Label;
				TempLabel.Dispose();
			}
		}
		private void DoGen()
		{
			this.Text = ControllerStr + "Sega Genesis";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GENController;
		}

		private void DoTI83()
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + "TI-83";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.TI83CalculatorCrop;
			int jpad = this.ControllComboBox.SelectedIndex;
			string[] ButtonMappings = new string[TI83ControlList.Length];
			ButtonMappings[0] = Global.Config.TI83Controller[jpad]._0;
			ButtonMappings[1] = Global.Config.TI83Controller[jpad]._1;
			ButtonMappings[2] = Global.Config.TI83Controller[jpad]._2;
			ButtonMappings[3] = Global.Config.TI83Controller[jpad]._3;
			ButtonMappings[4] = Global.Config.TI83Controller[jpad]._4;
			ButtonMappings[5] = Global.Config.TI83Controller[jpad]._5;
			ButtonMappings[6] = Global.Config.TI83Controller[jpad]._6;
			ButtonMappings[7] = Global.Config.TI83Controller[jpad]._7;
			ButtonMappings[8] = Global.Config.TI83Controller[jpad]._8;
			ButtonMappings[9] = Global.Config.TI83Controller[jpad]._9;
			ButtonMappings[10] = Global.Config.TI83Controller[jpad].DOT;
			ButtonMappings[11] = Global.Config.TI83Controller[jpad].ON;
			ButtonMappings[12] = Global.Config.TI83Controller[jpad].ENTER;
			ButtonMappings[13] = Global.Config.TI83Controller[jpad].UP;
			ButtonMappings[14] = Global.Config.TI83Controller[jpad].DOWN;
			ButtonMappings[15] = Global.Config.TI83Controller[jpad].LEFT;
			ButtonMappings[16] = Global.Config.TI83Controller[jpad].RIGHT;
			ButtonMappings[17] = Global.Config.TI83Controller[jpad].PLUS;
			ButtonMappings[18] = Global.Config.TI83Controller[jpad].MINUS;
			ButtonMappings[19] = Global.Config.TI83Controller[jpad].MULTIPLY;
			ButtonMappings[20] = Global.Config.TI83Controller[jpad].DIVIDE;
			ButtonMappings[21] = Global.Config.TI83Controller[jpad].CLEAR;
			ButtonMappings[22] = Global.Config.TI83Controller[jpad].EXP;
			ButtonMappings[23] = Global.Config.TI83Controller[jpad].DASH;
			ButtonMappings[24] = Global.Config.TI83Controller[jpad].PARACLOSE;
			ButtonMappings[25] = Global.Config.TI83Controller[jpad].PARAOPEN;
			ButtonMappings[26] = Global.Config.TI83Controller[jpad].TAN;
			ButtonMappings[27] = Global.Config.TI83Controller[jpad].VARS;
			ButtonMappings[28] = Global.Config.TI83Controller[jpad].COS;
			ButtonMappings[29] = Global.Config.TI83Controller[jpad].PRGM;
			ButtonMappings[30] = Global.Config.TI83Controller[jpad].STAT;
			ButtonMappings[31] = Global.Config.TI83Controller[jpad].MATRIX;
			ButtonMappings[32] = Global.Config.TI83Controller[jpad].X;
			ButtonMappings[33] = Global.Config.TI83Controller[jpad].STO;
			ButtonMappings[34] = Global.Config.TI83Controller[jpad].LN;
			ButtonMappings[35] = Global.Config.TI83Controller[jpad].LOG;
			ButtonMappings[36] = Global.Config.TI83Controller[jpad].SQUARED;
			ButtonMappings[37] = Global.Config.TI83Controller[jpad].NEG1;
			ButtonMappings[38] = Global.Config.TI83Controller[jpad].MATH;
			ButtonMappings[39] = Global.Config.TI83Controller[jpad].ALPHA;
			ButtonMappings[40] = Global.Config.TI83Controller[jpad].GRAPH;
			ButtonMappings[41] = Global.Config.TI83Controller[jpad].TRACE;
			ButtonMappings[42] = Global.Config.TI83Controller[jpad].ZOOM;
			ButtonMappings[43] = Global.Config.TI83Controller[jpad].WINDOW;
			ButtonMappings[44] = Global.Config.TI83Controller[jpad].Y;
			ButtonMappings[45] = Global.Config.TI83Controller[jpad].SECOND;
			ButtonMappings[46] = Global.Config.TI83Controller[jpad].MODE;
			ButtonMappings[47] = Global.Config.TI83Controller[jpad].DEL;
			ButtonMappings[48] = Global.Config.TI83Controller[jpad].COMMA;
			ButtonMappings[49] = Global.Config.TI83Controller[jpad].SIN;
			IDX_CONTROLLERENABLED.Checked = Global.Config.TI83Controller[jpad].Enabled;
			Changed = true;
			Labels.Clear();
			TextBoxes.Clear();

			//NOTE: Uses a hard coded 50 buttons (but it isn't likely that a TI-83 will magically get more buttons
			for (int i = 0; i < 17; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = TI83ControlList[i];
				TempLabel.Location = new Point(8, 20 + (i * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(48, 20 + (i * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
			}
			int c = 0;
			for (int i = 17; i < 34; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = TI83ControlList[i];
				TempLabel.Location = new Point(150, 20 + (c * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(190, 20 + (c * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
				c++;
			}
			c = 0;
			for (int i = 34; i < 50; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = TI83ControlList[i];
				TempLabel.Location = new Point(292, 20 + (c * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(348, 20 + (c * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
				c++;
			}
			Changed = true;
		}

		private void UpdateTI83()
		{
			ButtonsGroupBox.Controls.Clear();
			InputWidget TempBox;
			Label TempLabel;
			TempBox = TextBoxes[0] as InputWidget;
			Global.Config.TI83Controller[0]._0 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._0);
			TempBox.Dispose();
			TempBox = TextBoxes[1] as InputWidget;
			Global.Config.TI83Controller[0]._1 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._1);
			TempBox.Dispose();
			TempBox = TextBoxes[2] as InputWidget;
			Global.Config.TI83Controller[0]._2 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._2);
			TempBox.Dispose();
			TempBox = TextBoxes[3] as InputWidget;
			Global.Config.TI83Controller[0]._3 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._3);
			TempBox.Dispose();
			TempBox = TextBoxes[4] as InputWidget;
			Global.Config.TI83Controller[0]._4 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._4);
			TempBox.Dispose();
			TempBox = TextBoxes[5] as InputWidget;
			Global.Config.TI83Controller[0]._5 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._5);
			TempBox.Dispose();
			TempBox = TextBoxes[6] as InputWidget;
			Global.Config.TI83Controller[0]._6 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._6);
			TempBox.Dispose();
			TempBox = TextBoxes[7] as InputWidget;
			Global.Config.TI83Controller[0]._7 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._7);
			TempBox.Dispose();
			TempBox = TextBoxes[8] as InputWidget;
			Global.Config.TI83Controller[0]._8 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._8);
			TempBox.Dispose();
			TempBox = TextBoxes[9] as InputWidget;
			Global.Config.TI83Controller[0]._9 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0]._9);
			TempBox.Dispose();
			TempBox = TextBoxes[10] as InputWidget;
			Global.Config.TI83Controller[0].DOT = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].DOT);
			TempBox.Dispose();
			TempBox = TextBoxes[11] as InputWidget;
			Global.Config.TI83Controller[0].ON = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].ON);
			TempBox.Dispose();
			TempBox = TextBoxes[12] as InputWidget;
			Global.Config.TI83Controller[0].ENTER = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].ENTER);
			TempBox.Dispose();
			TempBox = TextBoxes[13] as InputWidget;
			Global.Config.TI83Controller[0].UP = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].UP);
			TempBox.Dispose();
			TempBox = TextBoxes[14] as InputWidget;
			Global.Config.TI83Controller[0].DOWN = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].DOWN);
			TempBox.Dispose();
			TempBox = TextBoxes[15] as InputWidget;
			Global.Config.TI83Controller[0].LEFT = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].LEFT);
			TempBox.Dispose();
			TempBox = TextBoxes[16] as InputWidget;
			Global.Config.TI83Controller[0].RIGHT = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].RIGHT);
			TempBox.Dispose();
			TempBox = TextBoxes[17] as InputWidget;
			Global.Config.TI83Controller[0].PLUS = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].PLUS);
			TempBox.Dispose();
			TempBox = TextBoxes[18] as InputWidget;
			Global.Config.TI83Controller[0].MINUS = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].MINUS);
			TempBox.Dispose();
			TempBox = TextBoxes[19] as InputWidget;
			Global.Config.TI83Controller[0].MULTIPLY = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].MULTIPLY);
			TempBox.Dispose();
			TempBox = TextBoxes[20] as InputWidget;
			Global.Config.TI83Controller[0].DIVIDE = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].DIVIDE);
			TempBox.Dispose();
			TempBox = TextBoxes[21] as InputWidget;
			Global.Config.TI83Controller[0].CLEAR = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].CLEAR);
			TempBox.Dispose();
			TempBox = TextBoxes[22] as InputWidget;
			Global.Config.TI83Controller[0].EXP = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].EXP);
			TempBox.Dispose();
			TempBox = TextBoxes[23] as InputWidget;
			Global.Config.TI83Controller[0].DASH = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].DASH);
			TempBox.Dispose();
			TempBox = TextBoxes[24] as InputWidget;
			Global.Config.TI83Controller[0].PARACLOSE = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].PARACLOSE);
			TempBox.Dispose();
			TempBox = TextBoxes[25] as InputWidget;
			Global.Config.TI83Controller[0].PARAOPEN = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].PARAOPEN);
			TempBox.Dispose();
			TempBox = TextBoxes[26] as InputWidget;
			Global.Config.TI83Controller[0].TAN = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].TAN);
			TempBox.Dispose();
			TempBox = TextBoxes[27] as InputWidget;
			Global.Config.TI83Controller[0].VARS = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].VARS);
			TempBox.Dispose();
			TempBox = TextBoxes[28] as InputWidget;
			Global.Config.TI83Controller[0].COS = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].COS);
			TempBox.Dispose();
			TempBox = TextBoxes[29] as InputWidget;
			Global.Config.TI83Controller[0].PRGM = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].PRGM);
			TempBox.Dispose();
			TempBox = TextBoxes[30] as InputWidget;
			Global.Config.TI83Controller[0].STAT = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].STAT);
			TempBox.Dispose();
			TempBox = TextBoxes[31] as InputWidget;
			Global.Config.TI83Controller[0].MATRIX = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].MATRIX);
			TempBox.Dispose();
			TempBox = TextBoxes[32] as InputWidget;
			Global.Config.TI83Controller[0].X = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].X);
			TempBox.Dispose();
			TempBox = TextBoxes[33] as InputWidget;
			Global.Config.TI83Controller[0].STO = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].STO);
			TempBox.Dispose();
			TempBox = TextBoxes[34] as InputWidget;
			Global.Config.TI83Controller[0].LN = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].LN);
			TempBox.Dispose();
			TempBox = TextBoxes[35] as InputWidget;
			Global.Config.TI83Controller[0].LOG = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].LOG);
			TempBox.Dispose();
			TempBox = TextBoxes[36] as InputWidget;
			Global.Config.TI83Controller[0].SQUARED = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].SQUARED);
			TempBox.Dispose();
			TempBox = TextBoxes[37] as InputWidget;
			Global.Config.TI83Controller[0].NEG1 = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].NEG1);
			TempBox.Dispose();
			TempBox = TextBoxes[38] as InputWidget;
			Global.Config.TI83Controller[0].MATH = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].MATH);
			TempBox.Dispose();
			TempBox = TextBoxes[39] as InputWidget;
			Global.Config.TI83Controller[0].ALPHA = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].ALPHA);
			TempBox.Dispose();
			TempBox = TextBoxes[40] as InputWidget;
			Global.Config.TI83Controller[0].GRAPH = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].GRAPH);
			TempBox.Dispose();
			TempBox = TextBoxes[41] as InputWidget;
			Global.Config.TI83Controller[0].TRACE = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].TRACE);
			TempBox.Dispose();
			TempBox = TextBoxes[42] as InputWidget;
			Global.Config.TI83Controller[0].ZOOM = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].ZOOM);
			TempBox.Dispose();
			TempBox = TextBoxes[43] as InputWidget;
			Global.Config.TI83Controller[0].WINDOW = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].WINDOW);
			TempBox.Dispose();
			TempBox = TextBoxes[44] as InputWidget;
			Global.Config.TI83Controller[0].Y = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].Y);
			TempBox.Dispose();
			TempBox = TextBoxes[45] as InputWidget;
			Global.Config.TI83Controller[0].SECOND = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].SECOND);
			TempBox.Dispose();
			TempBox = TextBoxes[46] as InputWidget;
			Global.Config.TI83Controller[0].MODE = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].MODE);
			TempBox.Dispose();
			TempBox = TextBoxes[47] as InputWidget;
			Global.Config.TI83Controller[0].DEL = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].DEL);
			TempBox.Dispose();
			TempBox = TextBoxes[48] as InputWidget;
			Global.Config.TI83Controller[0].COMMA = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].COMMA);
			TempBox.Dispose();
			TempBox = TextBoxes[49] as InputWidget;
			Global.Config.TI83Controller[0].SIN = AppendButtonMapping(TempBox.Text, Global.Config.TI83Controller[0].SIN);
			TempBox.Dispose();

			for (int i = 0; i < TI83ControlList.Length; i++)
			{
				TempLabel = Labels[i] as Label;
				TempLabel.Dispose();
			}
			IDX_CONTROLLERENABLED.Enabled = true;

		}

		private void DoGameBoy()
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + "Gameboy";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GBController;
			string[] ButtonMappings = new string[NESControlList.Length];
			ButtonMappings[0] = Global.Config.GameBoyController.Up;
			ButtonMappings[1] = Global.Config.GameBoyController.Down;
			ButtonMappings[2] = Global.Config.GameBoyController.Left;
			ButtonMappings[3] = Global.Config.GameBoyController.Right;
			ButtonMappings[4] = Global.Config.GameBoyController.A;
			ButtonMappings[5] = Global.Config.GameBoyController.B;
			ButtonMappings[6] = Global.Config.GameBoyController.Start;
			ButtonMappings[7] = Global.Config.GameBoyController.Select;
			IDX_CONTROLLERENABLED.Enabled = false;
			Changed = true;
			Labels.Clear();
			TextBoxes.Clear();
			for (int i = 0; i < NESControlList.Length; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = NESControlList[i];
				TempLabel.Location = new Point(8, 20 + (i * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(48, 20 + (i * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
			}
			Changed = true;
		}
		private void UpdateGameBoy()
		{
			ButtonsGroupBox.Controls.Clear();
			InputWidget TempBox;
			Label TempLabel;
			TempBox = TextBoxes[0] as InputWidget;
			Global.Config.GameBoyController.Up = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Up);
			TempBox.Dispose();
			TempBox = TextBoxes[1] as InputWidget;
			Global.Config.GameBoyController.Down = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Down);
			TempBox.Dispose();
			TempBox = TextBoxes[2] as InputWidget;
			Global.Config.GameBoyController.Left = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Left);
			TempBox.Dispose();
			TempBox = TextBoxes[3] as InputWidget;
			Global.Config.GameBoyController.Right = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Right);
			TempBox.Dispose();
			TempBox = TextBoxes[4] as InputWidget;
			Global.Config.GameBoyController.A = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.A);
			TempBox.Dispose();
			TempBox = TextBoxes[5] as InputWidget;
			Global.Config.GameBoyController.B = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.B);
			TempBox.Dispose();
			TempBox = TextBoxes[6] as InputWidget;
			Global.Config.GameBoyController.Start = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Start);
			TempBox.Dispose();
			TempBox = TextBoxes[7] as InputWidget;
			Global.Config.GameBoyController.Select = AppendButtonMapping(TempBox.Text, Global.Config.GameBoyController.Select);
			TempBox.Dispose();
			for (int i = 0; i < NESControlList.Length; i++)
			{
				TempLabel = Labels[i] as Label;
				TempLabel.Dispose();
			}
			IDX_CONTROLLERENABLED.Enabled = true;
		}

		private void DoNES()
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + "NES";
			ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.NESController;
			int jpad = this.ControllComboBox.SelectedIndex;
			string[] ButtonMappings = new string[NESControlList.Length];

			if (jpad < 4)
			{
				ButtonMappings[0] = Global.Config.NESController[jpad].Up;
				ButtonMappings[1] = Global.Config.NESController[jpad].Down;
				ButtonMappings[2] = Global.Config.NESController[jpad].Left;
				ButtonMappings[3] = Global.Config.NESController[jpad].Right;
				ButtonMappings[4] = Global.Config.NESController[jpad].A;
				ButtonMappings[5] = Global.Config.NESController[jpad].B;
				ButtonMappings[6] = Global.Config.NESController[jpad].Select;
				ButtonMappings[7] = Global.Config.NESController[jpad].Start;
				IDX_CONTROLLERENABLED.Checked = Global.Config.NESController[jpad].Enabled;
			}

			else
			{
				ButtonMappings[0] = Global.Config.NESAutoController[jpad - 4].Up;
				ButtonMappings[1] = Global.Config.NESAutoController[jpad - 4].Down;
				ButtonMappings[2] = Global.Config.NESAutoController[jpad - 4].Left;
				ButtonMappings[3] = Global.Config.NESAutoController[jpad - 4].Right;
				ButtonMappings[4] = Global.Config.NESAutoController[jpad - 4].A;
				ButtonMappings[5] = Global.Config.NESAutoController[jpad - 4].B;
				ButtonMappings[6] = Global.Config.NESAutoController[jpad - 4].Select;
				ButtonMappings[7] = Global.Config.NESAutoController[jpad - 4].Start;
				IDX_CONTROLLERENABLED.Checked = Global.Config.NESController[jpad - 4].Enabled;
			}
			
			
			Changed = true;
			Labels.Clear();
			TextBoxes.Clear();
			for (int i = 0; i < NESControlList.Length; i++)
			{
				TempLabel = new Label();
				TempLabel.Text = NESControlList[i];
				TempLabel.Location = new Point(8, 20 + (i * 24));
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget();
				TempTextBox.Location = new Point(48, 20 + (i * 24));
				TextBoxes.Add(TempTextBox);
				TempTextBox.SetBindings(ButtonMappings[i]);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
			}
			Changed = true;
		}
		private void UpdateNES(int prev)
		{
			ButtonsGroupBox.Controls.Clear();
			InputWidget TempBox;
			Label TempLabel;
			TempBox = TextBoxes[0] as InputWidget;

			if (prev < 4)
			{
				Global.Config.NESController[prev].Up = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Up);
				TempBox.Dispose();
				TempBox = TextBoxes[1] as InputWidget;
				Global.Config.NESController[prev].Down = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Down);
				TempBox.Dispose();
				TempBox = TextBoxes[2] as InputWidget;
				Global.Config.NESController[prev].Left = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Left);
				TempBox.Dispose();
				TempBox = TextBoxes[3] as InputWidget;
				Global.Config.NESController[prev].Right = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Right);
				TempBox.Dispose();
				TempBox = TextBoxes[4] as InputWidget;
				Global.Config.NESController[prev].A = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].A);
				TempBox.Dispose();
				TempBox = TextBoxes[5] as InputWidget;
				Global.Config.NESController[prev].B = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].B);
				TempBox.Dispose();
				TempBox = TextBoxes[6] as InputWidget;
				Global.Config.NESController[prev].Select = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Select);
				TempBox.Dispose();
				TempBox = TextBoxes[7] as InputWidget;
				Global.Config.NESController[prev].Start = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Start);
				TempBox.Dispose();

				Global.Config.NESController[prev].Enabled = IDX_CONTROLLERENABLED.Checked;
			}
			else
			{
				Global.Config.NESAutoController[prev - 4].Up = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Up);
				TempBox.Dispose();
				TempBox = TextBoxes[1] as InputWidget;
				Global.Config.NESAutoController[prev - 4].Down = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Down);
				TempBox.Dispose();
				TempBox = TextBoxes[2] as InputWidget;
				Global.Config.NESAutoController[prev - 4].Left = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Left);
				TempBox.Dispose();
				TempBox = TextBoxes[3] as InputWidget;
				Global.Config.NESAutoController[prev - 4].Right = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Right);
				TempBox.Dispose();
				TempBox = TextBoxes[4] as InputWidget;
				Global.Config.NESAutoController[prev - 4].A = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].A);
				TempBox.Dispose();
				TempBox = TextBoxes[5] as InputWidget;
				Global.Config.NESAutoController[prev - 4].B = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].B);
				TempBox.Dispose();
				TempBox = TextBoxes[6] as InputWidget;
				Global.Config.NESAutoController[prev - 4].Select = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Select);
				TempBox.Dispose();
				TempBox = TextBoxes[7] as InputWidget;
				Global.Config.NESAutoController[prev - 4].Start = AppendButtonMapping(TempBox.Text, Global.Config.NESAutoController[prev - 4].Start);
				TempBox.Dispose();

				Global.Config.NESController[prev - 4].Enabled = IDX_CONTROLLERENABLED.Checked;
			}

			TempBox.Dispose();
			for (int i = 0; i < NESControlList.Length; i++)
			{
				TempLabel = Labels[i] as Label;
				TempLabel.Dispose();
			}
		}
		private void InputConfig_Load(object sender, EventArgs e)
		{
			AutoTab.Checked = Global.Config.InputConfigAutoTab;
			SetAutoTab();
			prevWidth = Size.Width;
			prevHeight = Size.Height;
			AllowLR.Checked = Global.Config.AllowUD_LR;

			if (Global.Game != null)
				switch (Global.Game.System)
				{
					case "SMS":
					case "SG":
					case "GG":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("SMS / GG / SG-1000");
						break;
					case "PCE":
					case "SGX":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("PC Engine / SGX");
						break;
					case "GB":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("Gameboy");
						break;
					case "GEN":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("Sega Genesis");
						break;
					case "TI83":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("TI-83");
						break;

					case "NES":
						this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf("NES");
						break;
					default:
						this.SystemComboBox.SelectedIndex = 0;
						break;
				}
		}
		private void OK_Click(object sender, EventArgs e)
		{
			if (Changed)
			{
				UpdateAll();
			}
			this.DialogResult = DialogResult.OK;
			Global.Config.AllowUD_LR = AllowLR.Checked;
			this.Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void SystemComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Changed)
			{
				UpdateAll();
			}
			int joypads = 0;
			switch (this.SystemComboBox.SelectedItem.ToString())
			{
				case "SMS / GG / SG-1000":
					joypads = 2;
					this.Width = prevWidth;
					this.Height = prevHeight;
					break;
				case "PC Engine / SGX":
					joypads = 5;
					this.Width = prevWidth;
					this.Height = prevHeight;
					break;
				case "Gameboy":
					joypads = 1;
					this.Width = prevWidth;
					this.Height = prevHeight;
					break;
				case "Sega Genesis":
					joypads = 8;
					this.Width = prevWidth;
					this.Height = prevHeight;
					break;
				case "TI-83":
					joypads = 1;
					if (this.Width < 690)
						this.Width = 690;
					if (this.Height < 556)
						this.Height = 556;
					break;
				case "NES":
					joypads = 4;
					this.Width = prevWidth;
					this.Height = prevHeight;
					break;
			}
			ControllComboBox.Items.Clear();
			for (int i = 0; i < joypads; i++)
			{
				ControllComboBox.Items.Add(string.Format("Joypad {0}", i + 1));
			}
			for (int i = 0; i < joypads; i++)
			{
				ControllComboBox.Items.Add(string.Format("Autofire Joypad {0}", i + 1));
			}
			ControllComboBox.SelectedIndex = 0;
			CurSelectConsole = this.SystemComboBox.SelectedItem.ToString();
			CurSelectController = 0;
		}
		private void ControllComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Changed)
			{
				UpdateAll();
			}
			switch (SystemComboBox.SelectedItem.ToString())
			{
				case "SMS / GG / SG-1000":
					DoSMS();
					break;
				case "PC Engine / SGX":
					DoPCE();
					break;
				case "Gameboy":
					DoGameBoy();
					break;
				case "Sega Genesis":
					DoGen();
					break;
				case "TI-83":
					DoTI83();
					break;
				case "NES":
					DoNES();
					break;
			}
			CurSelectController = ControllComboBox.SelectedIndex;
		}
		private void UpdateAll()
		{
			switch (CurSelectConsole)
			{
				case "SMS / GG / SG-1000":
					UpdateSMS(CurSelectController);
					break;
				case "PC Engine / SGX":
					UpdatePCE(CurSelectController);
					break;
				case "Gameboy":
					UpdateGameBoy();
					break;
				case "Sega Genesis":
					//UpdateGenesis();
					break;
				case "TI-83":
					UpdateTI83();
					break;
				case "NES":
					UpdateNES(CurSelectController);
					break;
			}
			Changed = false;
		}

		private void AutoTab_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.HotkeyConfigAutoTab = AutoTab.Checked;
			SetAutoTab();
		}

		private void SetAutoTab()
		{
			for (int x = 0; x < ButtonsGroupBox.Controls.Count; x++)
			{
				if (ButtonsGroupBox.Controls[x] is InputWidget)
				{
					InputWidget w = ButtonsGroupBox.Controls[x] as InputWidget;
					w.AutoTab = AutoTab.Checked;
				}
			}
		}
	}

}

