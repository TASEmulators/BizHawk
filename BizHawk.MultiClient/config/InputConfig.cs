using System;
using System.Collections.Generic;
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
        public InputConfig()
        {
            InitializeComponent();
        }

        private void DoSMS()
        {
            this.Text = ControllerStr + "SMS / GG / SG-1000";
            ControllerImage.Image = BizHawk.MultiClient.Properties.Resources.SMSController;
            SystemComboBox.SelectedIndex = 0;

            Label UpLabel = new Label();
            UpLabel.Text = "Up";
            UpLabel.Location = new Point(8, 20);
            TextBox Up = new TextBox();
            Up.Location = new Point(48, 20);
            Up.Text = Global.Config.SmsP1Up;


            Label DownLabel = new Label();
            DownLabel.Text = "Down";
            DownLabel.Location = new Point(8, 44);
            TextBox Down = new TextBox();
            Down.Location = new Point(48, 44);
            Down.Text = Global.Config.SmsP1Down;

            Label LeftLabel = new Label();
            LeftLabel.Text = "Left";
            LeftLabel.Location = new Point(8, 68);
            TextBox Left = new TextBox();
            Left.Location = new Point(48, 68);
            Left.Text = Global.Config.SmsP1Left;

            Label RightLabel = new Label();
            RightLabel.Text = "Right";
            RightLabel.Location = new Point(8, 92);
            TextBox Right = new TextBox();
            Right.Location = new Point(48, 92);
            Right.Text = Global.Config.SmsP1Right;

            Label IButtonLabel = new Label();
            IButtonLabel.Text = "I";
            IButtonLabel.Location = new Point(8, 140);
            TextBox IButton = new TextBox();
            IButton.Location = new Point(48, 140);
            IButton.Text = Global.Config.SmsP1B1;

            Label IIButtonLabel = new Label();
            IIButtonLabel.Text = "II";
            IIButtonLabel.Location = new Point(8, 164);
            TextBox IIButton = new TextBox();
            IIButton.Location = new Point(48, 164);
            IIButton.Text = Global.Config.SmsP1B2;

            ButtonsGroupBox.Controls.Add(Up);
            ButtonsGroupBox.Controls.Add(UpLabel);
            ButtonsGroupBox.Controls.Add(Down);
            ButtonsGroupBox.Controls.Add(DownLabel);
            ButtonsGroupBox.Controls.Add(Left);
            ButtonsGroupBox.Controls.Add(LeftLabel);
            ButtonsGroupBox.Controls.Add(Right);
            ButtonsGroupBox.Controls.Add(RightLabel);
            ButtonsGroupBox.Controls.Add(IButton);
            ButtonsGroupBox.Controls.Add(IButtonLabel);
            ButtonsGroupBox.Controls.Add(IIButton);
            ButtonsGroupBox.Controls.Add(IIButtonLabel);
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
            //Determine the System currently loaded, and set that one up first, if null emulator set, what is the default?
            ControllComboBox.SelectedIndex = 0;
            if (!(Global.Emulator is NullEmulator))
            {
                switch (Global.Game.System)
                {
                    case "SMS":
                    case "SG":
                    case "GG":
                        DoSMS();
                        break;
                    case "PCE":
                    case "SGX":
                        DoPCE();
                        break;
                    case "GEN":
                        DoGen();
                        break;
                    case "TI83":
                        DoTI83();
                        break;
                    case "GB":
                        DoGameBoy();
                        break;
                    default:
                        DoSMS();
                        break;
                }
            }
            else
                DoSMS();
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
