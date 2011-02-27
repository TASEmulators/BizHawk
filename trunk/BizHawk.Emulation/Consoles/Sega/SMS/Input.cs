namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class SMS
    {
        public static readonly ControllerDefinition SmsController = new ControllerDefinition
            {
                Name = "SMS Controller",
                BoolButtons =
                        {
                            "Reset", "Pause",
                            "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2",
                            "P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
                        }
            };

        public void SetControllersAsMnemonic(string mnemonic)
        {
            if (mnemonic.Length == 0) return;

            if (mnemonic[1] != '.')
                Controller.ForceButton("P1 Up");
            if (mnemonic[2] != '.')
                Controller.ForceButton("P1 Down");
            if (mnemonic[3] != '.')
                Controller.ForceButton("P1 Left");
            if (mnemonic[4] != '.')
                Controller.ForceButton("P1 Right");
            if (mnemonic[5] != '.')
                Controller.ForceButton("P1 B1");
            if (mnemonic[6] != '.')
                Controller.ForceButton("P1 B2");
            if (mnemonic[7] != '.')
                Controller.ForceButton("P2 Up");
            if (mnemonic[8] != '.')
                Controller.ForceButton("P2 Down");
            if (mnemonic[9] != '.')
                Controller.ForceButton("P2 Left");
            if (mnemonic[10] != '.')
                Controller.ForceButton("P2 Right");
            if (mnemonic[11] != '.')
                Controller.ForceButton("P2 B1");
            if (mnemonic[12] != '.')
                Controller.ForceButton("P2 B2");
            if (mnemonic[13] != '.')
                Controller.ForceButton("Pause");

            if (mnemonic[15] != '.' && mnemonic[15] != '0')
                Controller.ForceButton("Reset");

        }


        public string GetControllersAsMnemonic()
        {
            string input = "|";
            
            if (Controller.IsPressed("P1 Up")) input += "U";
            else input += ".";
            if (Controller.IsPressed("P1 Down")) input += "D";
            else input += ".";
            if (Controller.IsPressed("P1 Left")) input += "L";
            else input += ".";
            if (Controller.IsPressed("P1 Right")) input += "R";
            else input += ".";
            if (Controller.IsPressed("P1 B1")) input += "1";
            else input += ".";
            if (Controller.IsPressed("P1 B2")) input += "2";
            else input += ".";
            if (Controller.IsPressed("P2 Up")) input += "U";
            else input += ".";
            if (Controller.IsPressed("P2 Down")) input += "D";
            else input += ".";
            if (Controller.IsPressed("P2 Left")) input += "L";
            else input += ".";
            if (Controller.IsPressed("P2 Right")) input += "R";
            else input += ".";
            if (Controller.IsPressed("P2 B1")) input += "1";
            else input += ".";
            if (Controller.IsPressed("P2 B2")) input += "2";
            else input += ".";
            if (Controller.IsPressed("Pause")) input += "S";
            else input += ".";

            input += "|";

            if (Controller.IsPressed("Reset")) input += "R";
            else input += ".";

            input += "|";

            return input;
        }

        public ControllerDefinition ControllerDefinition { get { return SmsController;  } }
        public IController Controller { get; set; }

        private byte ReadControls1()
        {
            byte value = 0xFF;

            if (Controller["P1 Up"])    value &= 0xFE;
            if (Controller["P1 Down"])  value &= 0xFD;
            if (Controller["P1 Left"])  value &= 0xFB;
            if (Controller["P1 Right"]) value &= 0xF7;
            if (Controller["P1 B1"])    value &= 0xEF;
            if (Controller["P1 B2"])    value &= 0xDF;

            if (Controller["P2 Up"])    value &= 0xBF;
            if (Controller["P2 Down"])  value &= 0x7F;

            return value;
        }

        private byte ReadControls2()
        {
            byte value = 0xFF;

            if (Controller["P2 Left"])  value &= 0xFE;
            if (Controller["P2 Right"]) value &= 0xFD;
            if (Controller["P2 B1"])    value &= 0xFB;
            if (Controller["P2 B2"])    value &= 0xF7;

            if (Controller["Reset"])    value &= 0xEF;

            if ((Port3F & 0x0F) == 5)
            {
                if (region == "Japan")
                {
                    value &= 0x3F;
                }
                else // US / Europe
                {
                    if (Port3F >> 4 == 0x0F)
                        value |= 0xC0;
                    else
                        value &= 0x3F;
                }
            }

            return value;
        }

        private byte ReadPort0()
        {
            if (IsGameGear == false)
                return 0xFF;
            byte value = 0xFF;
            if (Controller["Pause"])
                value ^= 0x80;
            if (Region == "US")
                value ^= 0x40;
            return value;
        }
    }
}