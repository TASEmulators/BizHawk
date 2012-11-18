namespace BizHawk.Emulation.Consoles.Coleco
{
    public partial class ColecoVision
    {
        public static readonly ControllerDefinition ColecoVisionControllerDefinition = new ControllerDefinition
        {
            Name = "ColecoVision Basic Controller",
            BoolButtons = 
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right",
				"P1 L", "P1 R",
				"P1 Key0", "P1 Key1", "P1 Key2", "P1 Key3", "P1 Key4", "P1 Key5",
				"P1 Key6", "P1 Key7", "P1 Key8", "P1 Key9", "P1 Star", "P1 Pound",

				"P2 Up", "P2 Down", "P2 Left", "P2 Right",
				"P2 L", "P2 R",
				"P2 Key0", "P2 Key1", "P2 Key2", "P2 Key3", "P2 Key4", "P2 Key5",
				"P2 Key6", "P2 Key7", "P2 Key8", "P2 Key9", "P2 Star", "P2 Pound"
			}
        };

        public ControllerDefinition ControllerDefinition { get { return ColecoVisionControllerDefinition; } }
        public IController Controller { get; set; }

        enum InputPortMode { Left, Right }
        InputPortMode InputPortSelection;

        byte ReadController1()
        {
            IsLagFrame = false;

            if (InputPortSelection == InputPortMode.Left)
            {
                byte retval = 0x7F;
                if (Controller["P1 Up"])    retval &= 0xFE;
                if (Controller["P1 Right"]) retval &= 0xFD;
                if (Controller["P1 Down"])  retval &= 0xFB;
                if (Controller["P1 Left"])  retval &= 0xF7;
                if (Controller["P1 L"])     retval &= 0x3F;

                return retval;
            }

            if (InputPortSelection == InputPortMode.Right)
            {
                byte retval = 0x0F;

                //                                   0x00;
                if (Controller["P1 Key8"])  retval = 0x01;
                if (Controller["P1 Key4"])  retval = 0x02;
                if (Controller["P1 Key5"])  retval = 0x03;
                //                                   0x04;
                if (Controller["P1 Key7"])  retval = 0x05;
                if (Controller["P1 Pound"]) retval = 0x06;
                if (Controller["P1 Key2"])  retval = 0x07;
                //                                   0x08;
                if (Controller["P1 Star"])  retval = 0x09;
                if (Controller["P1 Key0"])  retval = 0x0A;
                if (Controller["P1 Key9"])  retval = 0x0B;
                if (Controller["P1 Key3"])  retval = 0x0C;
                if (Controller["P1 Key1"])  retval = 0x0D;
                if (Controller["P1 Key6"])  retval = 0x0E;

                if (Controller["P1 R"] == false) retval |= 0x40;
                return retval;
            }

            return 0xFF;
        }


        byte ReadController2()
        {
            IsLagFrame = false;
            // TODO copy/paste from player 1 but.... debugging some things first
            return 0xFF;
        }


        public int Frame { get; set; }
        public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
        public bool IsLagFrame { get; private set; }
        private int _lagcount = 0;
    }
}
