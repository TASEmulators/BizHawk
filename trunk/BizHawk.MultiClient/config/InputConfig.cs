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
        public static string[] SMSList = new string[] { "Up", "Down", "Left", "Right", "B1", "B2", "Pause", "Reset" };
        public static string[] GenesisList = new string[] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start", "X", "Y", "Z" };
        private ArrayList Labels;
        private ArrayList TextBoxes;
        public InputConfig()
        {
            InitializeComponent();
            Labels = new ArrayList();
            TextBoxes = new ArrayList();
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
                return oldmap + ", " + button;
        }
        private void DoSMS()
        {
            Label TempLabel;
            InputWidget TempTextBox;
            this.Text = ControllerStr + "SMS / GG / SG-1000";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SMSController;
            this.SystemComboBox.SelectedIndex = 0;
            int jpad = this.ControllComboBox.SelectedIndex;
            string[] ButtonMappings = new string[SMSList.Length];
            ButtonMappings[0] = TruncateButtonMapping(Global.Config.SMSController[jpad].Up);
            ButtonMappings[1] = TruncateButtonMapping(Global.Config.SMSController[jpad].Down);
            ButtonMappings[2] = TruncateButtonMapping(Global.Config.SMSController[jpad].Left);
            ButtonMappings[3] = TruncateButtonMapping(Global.Config.SMSController[jpad].Right);
            ButtonMappings[4] = TruncateButtonMapping(Global.Config.SMSController[jpad].B1);
            ButtonMappings[5] = TruncateButtonMapping(Global.Config.SMSController[jpad].B2);
            ButtonMappings[6] = TruncateButtonMapping(Global.Config.SmsPause);
            ButtonMappings[7] = TruncateButtonMapping(Global.Config.SmsReset);


            Labels.Clear();
            for (int i = 0; i < SMSList.Length; i++)
            {
                TempLabel = new Label();
                TempLabel.Text = SMSList[i];
                TempLabel.Location = new Point(8, 20 + (i * 24));
                Labels.Add(TempLabel);
                TempTextBox = new InputWidget();
                TempTextBox.Location = new Point(48, 20 + (i * 24));
                TextBoxes.Add(TempTextBox);
                TempTextBox.Text = ButtonMappings[i];
                ButtonsGroupBox.Controls.Add(TempTextBox);
                ButtonsGroupBox.Controls.Add(TempLabel);
            }
        }
        private void DoPCE()
        {
            this.Text = ControllerStr + "PCEjin / SGX";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.PCEngineController;
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
            this.Text = ControllerStr + "Gameboy";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.GBController;
        }

        private void InputConfig_Load(object sender, EventArgs e)
        {            
            //SystemComboBox = new ComboBox();            
            //Determine the System currently loaded, and set that one up first, if null emulator set, what is the default?            
            /*
            if (!(Global.Emulator is NullEmulator))
            {
                switch (Global.Game.System)
                {
                    case "SMS":
                    case "SG":
                    case "GG":
                        joypads = 2;
                        break;
                    case "PCE":
                    case "SGX":
                        joypads = 5;
                        break;
                    case "GEN":
                        joypads = 8;
                        break;
                    case "TI83":
                        joypads = 1;
                        break;
                    case "GB":
                        joypads = 1;
                        break;
                    default:
                        joypads = 2;
                        break;
                }
            }
            else
            {
                joypads = 2;
            }

            ControllComboBox.Items.Clear();
            for (int i = 0; i < joypads; i++)
            {
                ControllComboBox.Items.Add(string.Format("Joypad {0}", i + 1));
            }
            ControllComboBox.SelectedIndex = 0;*/
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SystemComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            }
            ControllComboBox.Items.Clear();
            for (int i = 0; i < joypads; i++)
            {
                ControllComboBox.Items.Add(string.Format("Joypad {0}", i + 1));
            }
            ControllComboBox.SelectedIndex = 0;
        }
        private void ControllComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {           
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
                }
            }
        }
    }

