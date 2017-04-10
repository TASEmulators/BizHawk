namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
/*		public static readonly ControllerDefinition ColecoVisionControllerDefinition = new ControllerDefinition
		{
			Name = "ColecoVision Basic Controller",
			BoolButtons = 
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right",
				"P1 L", "P1 R",
				"P1 Key 0", "P1 Key 1", "P1 Key 2", "P1 Key 3", "P1 Key 4", "P1 Key 5",
				"P1 Key 6", "P1 Key 7", "P1 Key 8", "P1 Key 9", "P1 Star", "P1 Pound",

				"P2 Up", "P2 Down", "P2 Left", "P2 Right",
				"P2 L", "P2 R",
				"P2 Key 0", "P2 Key 1", "P2 Key 2", "P2 Key 3", "P2 Key 4", "P2 Key 5",
				"P2 Key 6", "P2 Key 7", "P2 Key 8", "P2 Key 9", "P2 Star", "P2 Pound"
			}
		};
		*/
		public enum InputPortMode { Left, Right }
		InputPortMode InputPortSelection;

		private byte ReadController1()
		{
			_isLag = false;
			byte retval;
			if (InputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort1(Controller, true, false);
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort1(Controller, false, false);
				return retval;
			}
			return 0x7F;
		}

		private byte ReadController2()
		{
			_isLag = false;
			byte retval;
			if (InputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort2(Controller, true, false);
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort2(Controller, false, false);
				return retval;
			}
			return 0x7F;
		}

		public int Frame { get { return frame; } set { frame = value; } }
		private int frame;
	}
}
