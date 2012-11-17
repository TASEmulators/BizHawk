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
				"P1 L1", "P1 L2", "P1 R1", "P1 R2",
				"P1 Key1", "P1 Key2", "P1 Key3", "P1 Key4", "P1 Key5",
				"P1 Key6", "P1 Key7", "P1 Key8", "P1 Key9", "P1 Star", "P1 Pound",

                "P2 Up", "P2 Down", "P2 Left", "P2 Right",
				"P2 L1", "P2 L2", "P2 R1", "P2 R2",
				"P2 Key1", "P2 Key2", "P2 Key3", "P2 Key4", "P2 Key5",
				"P2 Key6", "P2 Key7", "P2 Key8", "P2 Key9", "P2 Star", "P2 Pound"
			}
        };

        public ControllerDefinition ControllerDefinition { get { return ColecoVisionControllerDefinition; } }
        public IController Controller { get; set; }

        public int Frame { get { return _frame; } /*set { _frame = value; }*/ }
        public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
        public bool IsLagFrame { get { return _islag; } }
        private bool _islag = true;
        private int _lagcount = 0;
        private int _frame = 0;
    }
}
