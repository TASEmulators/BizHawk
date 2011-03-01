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
    public partial class InputConfig : Form
    {
        const string ControllerStr = "Configure Controllers - ";
        public static string[] SMSControlList = new string[] { "Up", "Down", "Left", "Right", "B1", "B2", "Pause", "Reset" };
        public static string[] PCEControlList = new string[] { "Up", "Down", "Left", "Right", "I", "II", "Run", "Select" };
        public static string[] GenesisControlList = new string[] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start", "X", "Y", "Z" };
        public static string[] NESControlList = new string[] { "Up", "Down", "Left", "Right", "A", "B", "Start", "Select" };
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

        private string TruncateButtonMapping(string button)
        {
            //all config button mappings have the name followed by a comma & space, then key mapping, remove up through the space
            int x = button.LastIndexOf(',');
            if (x != -1)
                return button.Substring(x + 2, button.Length - (x + 2));
            else
                return "";
        }
        private string AppendButtonMapping(string button, string oldmap)
        {
            int x = oldmap.LastIndexOf(',');
            if (x != -1)
                return oldmap.Substring(0, x + 2) + button;
            else
                return button;
        }

        private void DoSMS()
        {
            Label TempLabel;
            InputWidget TempTextBox;
            this.Text = ControllerStr + "SMS / GG / SG-1000";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SMSController;
            this.SystemComboBox.SelectedIndex = 0;
            int jpad = this.ControllComboBox.SelectedIndex;
            string[] ButtonMappings = new string[SMSControlList.Length];
            ButtonMappings[0] = TruncateButtonMapping(Global.Config.SMSController[jpad].Up);
            ButtonMappings[1] = TruncateButtonMapping(Global.Config.SMSController[jpad].Down);
            ButtonMappings[2] = TruncateButtonMapping(Global.Config.SMSController[jpad].Left);
            ButtonMappings[3] = TruncateButtonMapping(Global.Config.SMSController[jpad].Right);
            ButtonMappings[4] = TruncateButtonMapping(Global.Config.SMSController[jpad].B1);
            ButtonMappings[5] = TruncateButtonMapping(Global.Config.SMSController[jpad].B2);
            ButtonMappings[6] = TruncateButtonMapping(Global.Config.SmsPause);
            ButtonMappings[7] = TruncateButtonMapping(Global.Config.SmsReset);
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
                TempTextBox.Text = ButtonMappings[i];
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
            ButtonMappings[0] = TruncateButtonMapping(Global.Config.PCEController[jpad].Up);
            ButtonMappings[1] = TruncateButtonMapping(Global.Config.PCEController[jpad].Down);
            ButtonMappings[2] = TruncateButtonMapping(Global.Config.PCEController[jpad].Left);
            ButtonMappings[3] = TruncateButtonMapping(Global.Config.PCEController[jpad].Right);
            ButtonMappings[4] = TruncateButtonMapping(Global.Config.PCEController[jpad].I);
            ButtonMappings[5] = TruncateButtonMapping(Global.Config.PCEController[jpad].II);
            ButtonMappings[6] = TruncateButtonMapping(Global.Config.PCEController[jpad].Run);
            ButtonMappings[7] = TruncateButtonMapping(Global.Config.PCEController[jpad].Select);
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
                TempTextBox.Text = ButtonMappings[i];
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
            this.Text = ControllerStr + "TI-83";
        }

        private void DoGameBoy()
        {
            Label TempLabel;
            InputWidget TempTextBox;
            this.Text = ControllerStr + "Gameboy";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GBController;
            string[] ButtonMappings = new string[NESControlList.Length];
            ButtonMappings[0] = TruncateButtonMapping(Global.Config.GameBoyController.Up);
            ButtonMappings[1] = TruncateButtonMapping(Global.Config.GameBoyController.Down);
            ButtonMappings[2] = TruncateButtonMapping(Global.Config.GameBoyController.Left);
            ButtonMappings[3] = TruncateButtonMapping(Global.Config.GameBoyController.Right);
            ButtonMappings[4] = TruncateButtonMapping(Global.Config.GameBoyController.A);
            ButtonMappings[5] = TruncateButtonMapping(Global.Config.GameBoyController.B);
            ButtonMappings[6] = TruncateButtonMapping(Global.Config.GameBoyController.Start);
            ButtonMappings[7] = TruncateButtonMapping(Global.Config.GameBoyController.Select);
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
                TempTextBox.Text = ButtonMappings[i];
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
            ButtonMappings[0] = TruncateButtonMapping(Global.Config.NESController[jpad].Up);
            ButtonMappings[1] = TruncateButtonMapping(Global.Config.NESController[jpad].Down);
            ButtonMappings[2] = TruncateButtonMapping(Global.Config.NESController[jpad].Left);
            ButtonMappings[3] = TruncateButtonMapping(Global.Config.NESController[jpad].Right);
            ButtonMappings[4] = TruncateButtonMapping(Global.Config.NESController[jpad].A);
            ButtonMappings[5] = TruncateButtonMapping(Global.Config.NESController[jpad].B);
            ButtonMappings[6] = TruncateButtonMapping(Global.Config.NESController[jpad].Start);
            ButtonMappings[7] = TruncateButtonMapping(Global.Config.NESController[jpad].Select);
            IDX_CONTROLLERENABLED.Checked = Global.Config.NESController[jpad].Enabled;
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
                TempTextBox.Text = ButtonMappings[i];
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
            Global.Config.NESController[prev].Start = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Start);
            TempBox.Dispose();
            TempBox = TextBoxes[7] as InputWidget;
            Global.Config.NESController[prev].Select = AppendButtonMapping(TempBox.Text, Global.Config.NESController[prev].Select);
            Global.Config.NESController[prev].Enabled = IDX_CONTROLLERENABLED.Checked;

            TempBox.Dispose();
            for (int i = 0; i < NESControlList.Length; i++)
            {
                TempLabel = Labels[i] as Label;
                TempLabel.Dispose();
            }
        }
        private void InputConfig_Load(object sender, EventArgs e)
        {
            switch (Global.Game.System)
            {
                case "SMS":
                case "SG":
                case "GG":
                    this.SystemComboBox.SelectedIndex = 0;
                    break;
                case "PCE":
                case "SGX":
                    this.SystemComboBox.SelectedIndex = 1;
                    break;
                case "GEN":
                    this.SystemComboBox.SelectedIndex = 2;
                    break;
                case "TI83":
                    this.SystemComboBox.SelectedIndex = 3;
                    break;
                case "GB":
                    this.SystemComboBox.SelectedIndex = 4;
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
                    break;
                case "PC Engine / SGX":
                    joypads = 5;
                    break;
                case "Gameboy":
                    joypads = 1;
                    break;
                case "Sega Genesis":
                    joypads = 8;
                    break;
                case "TI-83":
                    joypads = 1;
                    break;
                case "NES":
                    joypads = 4;
                    break;
            }
            ControllComboBox.Items.Clear();
            for (int i = 0; i < joypads; i++)
            {
                ControllComboBox.Items.Add(string.Format("Joypad {0}", i + 1));
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
                    //Update TI-83();
                    break;
                case "NES":
                    UpdateNES(CurSelectController);
                    break;
            }
            Changed = false;
        }
    }

}

