using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class InputConfig : Form
	{
		int prevWidth;
		int prevHeight;
		const string ControllerStr = "Configure Controllers - ";
		public static readonly Dictionary<string, string[]> CONTROLS = new Dictionary<string, string[]>()
		{
			{"Atari", new string[5] { "Up", "Down", "Left", "Right", "Button" } },
			{"AtariConsoleButtons", new string[2] { "Reset", "Select" } },
			{"Gameboy", new string[8] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" } },
			{"NES", new string[8] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" } },
			{"SNES", new string[] { "Up", "Down", "Left", "Right", "B", "A", "X", "Y", "L", "R", "Select", "Start" } },
			{"PC Engine / SuperGrafx", new string[8] { "Up", "Down", "Left", "Right", "I", "II", "Run", "Select" } },
			{"Sega Genesis", new string[8] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start" } },
			{"SMS / GG / SG-1000", new string[8] { "Up", "Down", "Left", "Right", "B1", "B2", "Pause", "Reset" } },
			
			{
				// TODO: display shift / alpha names too, Also order these like on the calculator
				"TI-83", new string[50] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "ON",
				"ENTER", "Up", "Down", "Left", "Right", "+", "-", "Multiply", "Divide", "CLEAR", "^", "-", "(", ")", "TAN",
				"VARS", "COS", "PRGM", "STAT", "Matrix", "X", "STO->", "LN", "LOG", "^2", "^-1", "MATH", "ALPHA", "GRAPH",
				"TRACE", "ZOOM", "WINDOW", "Y", "2nd", "MODE", "Del", ",", "SIN" }
			}
		};

		public static readonly string[] TI83CONTROLS = new string[50] {
			"_0", "_1", "_2", "_3", "_4", "_5", "_6", "_7", "_8", "_9", "DOT", "ON", "ENTER", "UP", "DOWN", "LEFT", "RIGHT",
			"PLUS", "MINUS", "MULTIPLY", "DIVIDE", "CLEAR", "EXP", "DASH", "PARAOPEN", "PARACLOSE", "TAN", "VARS", "COS",
			"PRGM", "STAT", "MATRIX", "X", "STO", "LN", "LOG", "SQUARED", "NEG1", "MATH", "ALPHA", "GRAPH", "TRACE", "ZOOM",
			"WINDOW", "Y", "SECOND", "MODE", "DEL", "COMMA", "SIN"
		};

		public static readonly Dictionary<string, int> PADS = new Dictionary<string, int>()
		{
			{"Atari", 2}, {"Gameboy", 1}, {"NES", 4}, {"PC Engine / SuperGrafx", 5}, {"Sega Genesis", 1}, {"SMS / GG / SG-1000", 2}, 
			{"SNES", 4},
			{"TI-83", 1}
		};

		private List<KeyValuePair<string, string>> HotkeyMappingList = new List<KeyValuePair<string, string>>(); //A list of all button mappings and the hotkey they are assigned to

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

		private void Do(string platform)
		{
			Label TempLabel;
			InputWidget TempTextBox;
			this.Text = ControllerStr + platform;
			object[] controller = null;
			object[] mainController = null;
			object[] autoController = null;
			switch (platform)
			{
				case "Atari":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.atari_controller;
					controller = Global.Config.Atari2600Controller;
					autoController = Global.Config.Atari2600AutoController;
					break;
				case "Gameboy":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GBController;
					controller = Global.Config.GBController;
					autoController = Global.Config.GBAutoController;
					break;
				case "NES":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.NES_Controller;
					controller = Global.Config.NESController;
					autoController = Global.Config.NESAutoController;
					break;
				case "PC Engine / SuperGrafx":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.PCEngineController;
					controller = Global.Config.PCEController;
					autoController = Global.Config.PCEAutoController;
					break;
				case "Sega Genesis":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GENController;
					controller = Global.Config.GenesisController;
					autoController = Global.Config.GenesisAutoController;
					break;
				case "SMS / GG / SG-1000":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SMSController;
					controller = Global.Config.SMSController;
					autoController = Global.Config.SMSAutoController;
					break;
				case "TI-83":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.TI83_Controller;
					controller = Global.Config.TI83Controller;
					break;
				case "SNES":
					ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SNES_Controller;
					controller = Global.Config.SNESController;
					autoController = Global.Config.SNESAutoController;
					break;
				default:
					return;
			}
			mainController = controller;
			int jpad = this.ControllComboBox.SelectedIndex;
			if (jpad >= PADS[platform] * 2) //Not joypad or auto-joypad, must be special category (adelikat: I know this is hacky but so is the rest of this file)
			{
				switch (platform)
				{
					case "Atari":
						IDX_CONTROLLERENABLED.Checked = Global.Config.Atari2600ConsoleButtons[0].Enabled;
						controller = Global.Config.Atari2600ConsoleButtons;
						break;
				}
				platform += "ConsoleButtons";
			}
			else
			{
				if (jpad >= PADS[platform])
				{
					jpad -= PADS[platform];
					controller = autoController;
				}
				switch (platform)
				{
					case "Atari":
						IDX_CONTROLLERENABLED.Checked = ((Atari2600ControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "Gameboy":
						IDX_CONTROLLERENABLED.Checked = ((GBControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "NES":
						IDX_CONTROLLERENABLED.Checked = ((NESControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "SNES":
						IDX_CONTROLLERENABLED.Checked = ((SNESControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "PC Engine / SuperGrafx":
						IDX_CONTROLLERENABLED.Checked = ((PCEControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "Sega Genesis":
						IDX_CONTROLLERENABLED.Checked = ((GenControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "SMS / GG / SG-1000":
						IDX_CONTROLLERENABLED.Checked = ((SMSControllerTemplate)mainController[jpad]).Enabled;
						break;
					case "TI-83":
						IDX_CONTROLLERENABLED.Checked = ((TI83ControllerTemplate)mainController[jpad]).Enabled;
						break;
				}
			}
			Labels.Clear();
			TextBoxes.Clear();
			int row = 0;
			int col = 0;
			for (int button = 0; button < CONTROLS[platform].Length; button++)
			{
				TempLabel = new Label();
				TempLabel.Text = CONTROLS[platform][button];
				int xoffset = (col * 156);
				int yoffset = (row * 24);
				TempLabel.Location = new Point(8 + xoffset, 20 + yoffset);
				Labels.Add(TempLabel);
				TempTextBox = new InputWidget(HotkeyMappingList);
				TempTextBox.Location = new Point(64 + xoffset, 20 + yoffset);
				TextBoxes.Add(TempTextBox);
				object field = null;
				string fieldName = CONTROLS[platform][button];
				switch (platform)
				{
					case "AtariConsoleButtons":
						Atari2600ConsoleButtonsTemplate o = (Atari2600ConsoleButtonsTemplate)controller[0];
						field = o.GetType().GetField(fieldName).GetValue(o);
						break;
					case "Atari":
					{
						Atari2600ControllerTemplate obj = (Atari2600ControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "Gameboy":
					{
						GBControllerTemplate obj = (GBControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "NES":
					{
						NESControllerTemplate obj = (NESControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "SNES":
					{
						SNESControllerTemplate obj = (SNESControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "PC Engine / SuperGrafx":
					{
						PCEControllerTemplate obj = (PCEControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "Sega Genesis":
					{
						GenControllerTemplate obj = (GenControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(fieldName).GetValue(obj);
						break;
					}
					case "SMS / GG / SG-1000":
					{
						if (button < 6)
						{
							SMSControllerTemplate obj = (SMSControllerTemplate)controller[jpad];
							field = obj.GetType().GetField(fieldName).GetValue(obj);
						}
						else if (button == 6)
							field = Global.Config.SMSConsoleButtons.Pause;
						else
							field = Global.Config.SMSConsoleButtons.Reset;
						break;
					}
					case "TI-83":
					{
						TI83ControllerTemplate obj = (TI83ControllerTemplate)controller[jpad];
						field = obj.GetType().GetField(TI83CONTROLS[button]).GetValue(obj);
						break;
					}
				}
				TempTextBox.SetBindings((string)field);
				ButtonsGroupBox.Controls.Add(TempTextBox);
				ButtonsGroupBox.Controls.Add(TempLabel);
				row++;
				if (row > 16)
				{
					row = 0;
					col++;
				}
			}
			Changed = true;
		}

		private void Update(int prev, string platform)
		{
			if (platform == "Atari" && prev == 4) //adelikat: very hacky  I know
				platform += "ConsoleButtons";
			ButtonsGroupBox.Controls.Clear();
			object[] controller = null;
			object[] mainController = null;
			object[] autoController = null;
			switch (platform)
			{
				case "AtariConsoleButtons":
					controller = Global.Config.Atari2600ConsoleButtons;
					break;
				case "Atari":
					controller = Global.Config.Atari2600Controller;
					autoController = Global.Config.Atari2600AutoController;
					break;
				case "Gameboy":
					controller = Global.Config.GBController;
					autoController = Global.Config.GBAutoController;
					break;
				case "NES":
					controller = Global.Config.NESController;
					autoController = Global.Config.NESAutoController;
					break;
				case "SNES":
					controller = Global.Config.SNESController;
					autoController = Global.Config.SNESAutoController;
					break;
				case "PC Engine / SuperGrafx":
					controller = Global.Config.PCEController;
					autoController = Global.Config.PCEAutoController;
					break;
				case "Sega Genesis":
					controller = Global.Config.GenesisController;
					autoController = Global.Config.GenesisAutoController;
					break;
				case "SMS / GG / SG-1000":
					controller = Global.Config.SMSController;
					autoController = Global.Config.SMSAutoController;
					break;
				case "TI-83":
					controller = Global.Config.TI83Controller;
					break;
				default:
					return;
			}
			mainController = controller;
			if (platform == "AtariConsoleButtons")
			{
				prev = 0;
			}
			else if (prev >= PADS[platform])
			{
				prev -= PADS[platform];
				controller = autoController;
			}
			switch (platform)
			{
				case "AtariConsoleButtons":
					((Atari2600ConsoleButtonsTemplate)mainController[0]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "Atari":
					((Atari2600ControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "Gameboy":
					((GBControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "NES":
					((NESControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "SNES":
					((SNESControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "PC Engine / SuperGrafx":
					((PCEControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "Sega Genesis":
					((GenControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "SMS / GG / SG-1000":
					((SMSControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
				case "TI-83":
					((TI83ControllerTemplate)mainController[prev]).Enabled = IDX_CONTROLLERENABLED.Checked;
					break;
			}
			for (int button = 0; button < CONTROLS[platform].Length; button++)
			{
				InputWidget TempBox = TextBoxes[button] as InputWidget;
				object field = null;
				string fieldName = CONTROLS[platform][button];
				switch (platform)
				{
					case "AtariConsoleButtons":
						Atari2600ConsoleButtonsTemplate o = (Atari2600ConsoleButtonsTemplate)controller[0];
						FieldInfo buttonF = o.GetType().GetField(fieldName);
						field = buttonF.GetValue(o);
						buttonF.SetValue(o, TempBox.Text);
						break;
					case "Atari":
					{
						Atari2600ControllerTemplate obj = (Atari2600ControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "Gameboy":
					{
						GBControllerTemplate obj = (GBControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "NES":
					{
						NESControllerTemplate obj = (NESControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "SNES":
					{
						SNESControllerTemplate obj = (SNESControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "PC Engine / SuperGrafx":
					{
						PCEControllerTemplate obj = (PCEControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "Sega Genesis":
					{
						GenControllerTemplate obj = (GenControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(fieldName);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
					case "SMS / GG / SG-1000":
					{
						if (button < 6)
						{
							SMSControllerTemplate obj = (SMSControllerTemplate)controller[prev];
							FieldInfo buttonField = obj.GetType().GetField(fieldName);
							field = buttonField.GetValue(obj);
							buttonField.SetValue(obj, TempBox.Text);
						}
						else if (button == 6)
							Global.Config.SMSConsoleButtons.Pause = TempBox.Text;
						else
							Global.Config.SMSConsoleButtons.Reset = TempBox.Text;
						break;
					}
					case "TI-83":
					{
						TI83ControllerTemplate obj = (TI83ControllerTemplate)controller[prev];
						FieldInfo buttonField = obj.GetType().GetField(TI83CONTROLS[button]);
						field = buttonField.GetValue(obj);
						buttonField.SetValue(obj, TempBox.Text);
						break;
					}
				}
				TempBox.Dispose();
				Label TempLabel = Labels[button] as Label;
				TempLabel.Dispose();
			}

			Global.OSD.AddMessage("Controller settings saved");
		}

		private void InputConfig_Load(object sender, EventArgs e)
		{
			SystemComboBox.Items.Add("Atari"); //TODO: add this to the designer instead

			HotkeyMappingList = Global.ClientControls.MappingList();

			AutoTab.Checked = Global.Config.InputConfigAutoTab;
			SetAutoTab();
			prevWidth = Size.Width;
			prevHeight = Size.Height;
			AllowLR.Checked = Global.Config.AllowUD_LR;

			if (Global.Game != null)
			{
				Dictionary<string, string> systems = new Dictionary<string, string>()
				{
					{"A26", "Atari"}, {"GB", "Gameboy"}, {"GEN", "Sega Genesis"}, {"GG", "SMS / GG / SG-1000"}, {"NES", "NES"},
					{"SNES", "SNES"}, {"GBC", "Gameboy"},
					{"PCE", "PC Engine / SuperGrafx"}, {"SG", "SMS / GG / SG-1000"}, {"SGX", "PC Engine / SuperGrafx"},
					{"SMS", "SMS / GG / SG-1000"}, {"TI83", "TI-83"}
				};
				if (systems.ContainsKey(Global.Game.System))
					this.SystemComboBox.SelectedIndex = SystemComboBox.Items.IndexOf(systems[Global.Game.System]);
				else
					this.SystemComboBox.SelectedIndex = 0;
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
			Global.OSD.AddMessage("Controller config aborted");
			this.Close();
		}

		private void SystemComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Changed)
			{
				UpdateAll();
			}
			int joypads = PADS[this.SystemComboBox.SelectedItem.ToString()];
			if (this.SystemComboBox.SelectedItem.ToString() != "TI-83")
			{
				this.Width = prevWidth;
				this.Height = prevHeight;
			}
			else
			{
				if (this.Width < 700)
					this.Width = 700;
				if (this.Height < 580)
					this.Height = 580;
			}
			ControllComboBox.Items.Clear();
			for (int i = 0; i < joypads; i++)
			{
				ControllComboBox.Items.Add(string.Format("Joypad {0}", i + 1));
			}
			for (int i = 0; i < joypads; i++)
			{
				if (this.SystemComboBox.SelectedItem.ToString() != "TI-83")
					ControllComboBox.Items.Add(string.Format("Autofire Joypad {0}", i + 1));
			}
			if (this.SystemComboBox.SelectedItem.ToString() == "Atari")
			{
				ControllComboBox.Items.Add("Console");
			}
			ControllComboBox.SelectedIndex = 0;
			CurSelectConsole = this.SystemComboBox.SelectedItem.ToString();
			CurSelectController = 0;
			SetFocus();
			SetAutoTab();
		}
		private void ControllComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Changed)
			{
				UpdateAll();
			}
			Do(SystemComboBox.SelectedItem.ToString());
			CurSelectController = ControllComboBox.SelectedIndex;
			SetFocus();
			SetAutoTab();
		}
		private void UpdateAll()
		{
			Update(CurSelectController, CurSelectConsole);
			Changed = false;
		}

		private void AutoTab_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.HotkeyConfigAutoTab = AutoTab.Checked;
			SetAutoTab();
		}

		private void SetFocus()
		{
			for (int x = 0; x < ButtonsGroupBox.Controls.Count; x++)
			{
				if (ButtonsGroupBox.Controls[x] is InputWidget)
				{
					ButtonsGroupBox.Controls[x].Focus();
					return;
				}
			}
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

		private void clearMappingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < ButtonsGroupBox.Controls.Count; i++)
			{
				if (ButtonsGroupBox.Controls[i] is InputWidget)
				{
					InputWidget w = ButtonsGroupBox.Controls[i] as InputWidget;
					w.EraseMappings();
				}
			}
		}

		private void InputConfig_Shown(object sender, EventArgs e)
		{
			SetFocus();
		}
	}
}