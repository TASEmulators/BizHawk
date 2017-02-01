using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
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
				"P1 Key 0", "P1 Key 1", "P1 Key 2", "P1 Key 3", "P1 Key 4", "P1 Key 5",
				"P1 Key 6", "P1 Key 7", "P1 Key 8", "P1 Key 9", "P1 Star", "P1 Pound",

				"P2 Up", "P2 Down", "P2 Left", "P2 Right",
				"P2 L", "P2 R",
				"P2 Key 0", "P2 Key 1", "P2 Key 2", "P2 Key 3", "P2 Key 4", "P2 Key 5",
				"P2 Key 6", "P2 Key 7", "P2 Key 8", "P2 Key 9", "P2 Star", "P2 Pound"
			}
		};

		public ControllerDefinition ControllerDefinition { get { return ColecoVisionControllerDefinition; } }
		public IController Controller { get; set; }

		enum InputPortMode { Left, Right }
		InputPortMode InputPortSelection;

		byte ReadController1()
		{
			_isLag = false;

			if (InputPortSelection == InputPortMode.Left)
			{
				byte retval = 0x7F;
				if (Controller.IsPressed("P1 Up")) retval &= 0xFE;
				if (Controller.IsPressed("P1 Right")) retval &= 0xFD;
				if (Controller.IsPressed("P1 Down")) retval &= 0xFB;
				if (Controller.IsPressed("P1 Left")) retval &= 0xF7;
				if (Controller.IsPressed("P1 L")) retval &= 0x3F;
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				byte retval = 0xF;

				//                                   0x00;
				if (Controller.IsPressed("P1 Key 8")) retval = 0x01;
				if (Controller.IsPressed("P1 Key 4")) retval = 0x02;
				if (Controller.IsPressed("P1 Key 5")) retval = 0x03;
				//                                             0x04;
				if (Controller.IsPressed("P1 Key 7")) retval = 0x05;
				if (Controller.IsPressed("P1 Pound")) retval = 0x06;
				if (Controller.IsPressed("P1 Key 2")) retval = 0x07;
				//                                             0x08;
				if (Controller.IsPressed("P1 Star")) retval = 0x09;
				if (Controller.IsPressed("P1 Key 0")) retval = 0x0A;
				if (Controller.IsPressed("P1 Key 9")) retval = 0x0B;
				if (Controller.IsPressed("P1 Key 3")) retval = 0x0C;
				if (Controller.IsPressed("P1 Key 1")) retval = 0x0D;
				if (Controller.IsPressed("P1 Key 6")) retval = 0x0E;

				if (Controller.IsPressed("P1 R") == false) retval |= 0x40;
				retval |= 0x30; // always set these bits
				return retval;
			}

			return 0x7F;
		}


		byte ReadController2()
		{
			_isLag = false;

			if (InputPortSelection == InputPortMode.Left)
			{
				byte retval = 0x7F;
				if (Controller.IsPressed("P2 Up")) retval &= 0xFE;
				if (Controller.IsPressed("P2 Right")) retval &= 0xFD;
				if (Controller.IsPressed("P2 Down")) retval &= 0xFB;
				if (Controller.IsPressed("P2 Left")) retval &= 0xF7;
				if (Controller.IsPressed("P2 L")) retval &= 0x3F;
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				byte retval = 0xF;

				//                                   0x00;
				if (Controller.IsPressed("P2 Key8")) retval = 0x01;
				if (Controller.IsPressed("P2 Key4")) retval = 0x02;
				if (Controller.IsPressed("P2 Key5")) retval = 0x03;
				//                                            0x04;
				if (Controller.IsPressed("P2 Key7")) retval = 0x05;
				if (Controller.IsPressed("P2 Pound")) retval = 0x06;
				if (Controller.IsPressed("P2 Key2")) retval = 0x07;
				//                                            0x08;
				if (Controller.IsPressed("P2 Star")) retval = 0x09;
				if (Controller.IsPressed("P2 Key0")) retval = 0x0A;
				if (Controller.IsPressed("P2 Key9")) retval = 0x0B;
				if (Controller.IsPressed("P2 Key3")) retval = 0x0C;
				if (Controller.IsPressed("P2 Key1")) retval = 0x0D;
				if (Controller.IsPressed("P2 Key6")) retval = 0x0E;

				if (Controller.IsPressed("P2 R") == false) retval |= 0x40;
				retval |= 0x30; // always set these bits
				return retval;
			}

			return 0x7F;
		}

		public int Frame { get { return frame; } set { frame = value; } }
		int frame;
	}
}
