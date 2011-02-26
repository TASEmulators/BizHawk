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


        public string GetControllersAsMneumonic()
        {
            return "|........|0|"; //TODO: implement
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
