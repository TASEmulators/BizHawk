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

            Button Up = new Button();
            Button Down = new Button();
        }

        private void DoPCE()
        {
            this.Text = ControllerStr + "PCEjin / SGX";
        }

        private void DoGen()
        {
            this.Text = ControllerStr + "Sega Genesis";
        }

        private void DoTI83()
        {
            this.Text = ControllerStr + "TI-83";
        }

        private void DoGameBoy()
        {
            this.Text = ControllerStr + "Gameboy";
        }

        private void InputConfig_Load(object sender, EventArgs e)
        {
            //Determine the System currently loaded, and set that one up first, if null emulator set, what is the default?
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
