namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        public static readonly ControllerDefinition PCEngineController =
            new ControllerDefinition
            {
                Name = "PC Engine Controller",
                BoolButtons = { "Up", "Down", "Left", "Right", "II", "I", "Select", "Run" }
            };

        public ControllerDefinition ControllerDefinition { get { return PCEngineController;  } }
        public IController Controller { get; set; }

        public void SetControllersAsMnemonic(string mnemonic)
        {
            return; //TODO
        }

        public string GetControllersAsMnemonic()
        {
            //TODO: Implement all controllers
            
            string input = "";
            if (Controller.IsPressed("Up")) input += "U";
            else input += " ";
            if (Controller.IsPressed("Down")) input += "D";
            else input += " ";
            if (Controller.IsPressed("Left")) input += "L";
            else input += " ";
            if (Controller.IsPressed("Right")) input += "R";
            else input += " ";
            if (Controller.IsPressed("I")) input += "1";
            else input += " ";
            if (Controller.IsPressed("II")) input += "2";
            else input += " ";
            if (Controller.IsPressed("Select")) input += "S";
            else input += " ";
            if (Controller.IsPressed("Run")) input += "R";
            else input += " ";

            input += "|.|"; //TODO: Add commands like reset here

            return input;
        }

        private byte inputSelector;
        public bool SEL { get { return ((inputSelector & 1) != 0) ;} }

        private void WriteInput(byte value)
        {
            inputSelector = value;
        }
    
        private byte ReadInput()
        {
            byte value = 0xBF; 
            if (SEL == false) // return buttons
            {
                if (Controller["I"])      value &= 0xFE;
                if (Controller["II"])     value &= 0xFD;
                if (Controller["Select"]) value &= 0xFB;
                if (Controller["Run"])    value &= 0xF7;
            } else { //return directions
                if (Controller["Up"])     value &= 0xFE;
                if (Controller["Right"])  value &= 0xFD;
                if (Controller["Down"])   value &= 0xFB;
                if (Controller["Left"])   value &= 0xF7;
            }

            if (Region == "Japan") value |= 0x40;

            return value;
        }
    }
}
