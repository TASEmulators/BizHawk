namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		public enum InputPortMode { Left, Right }
		private InputPortMode InputPortSelection;

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

		public int Frame
		{
			get { return frame; }
			private set { frame = value; }
		}

		private int frame;
	}
}
