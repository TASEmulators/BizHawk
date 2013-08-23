using BizHawk.Emulation.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Motherboard
	{
		private int[] joystickPressed = new int[10];
		private int[] keyboardPressed = new int[64];

		static private string[,] joystickMatrix = new string[2, 5]
		{
			{"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"},
			{"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"}
		};

		static private string[,] keyboardMatrix = new string[8, 8]
		{
			{"Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Key Cursor Up/Down"},
			{"Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift"},
			{"Key 5", "Key R", "Key D", "Key 6", "Key C", "Key F", "Key T", "Key X"},
			{"Key 7", "Key Y", "Key G", "Key 8", "Key B", "Key H", "Key U", "Key V"},
			{"Key 9", "Key I", "Key J", "Key 0", "Key M", "Key K", "Key O", "Key N"},
			{"Key Plus", "Key P", "Key L", "Key Minus", "Key Period", "Key Colon", "Key At", "Key Comma"},
			{"Key Pound", "Key Asterisk", "Key Semicolon", "Key Clear/Home", "Key Right Shift", "Key Equal", "Key Up Arrow", "Key Slash"},
			{"Key 1", "Key Left Arrow", "Key Control", "Key 2", "Key Space", "Key Commodore", "Key Q", "Key Run/Stop"}
		};

		static private byte[] inputBitMask = new byte[] { 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F };
		static private byte[] inputBitSelect = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };

        protected byte cia0InputLatchA;
        protected byte cia0InputLatchB;
        protected int pollIndex;

		public void PollInput()
		{
			// scan joysticks
            pollIndex = 0;
            for (int j = 0; j < 5; j++)
			{
                for (int i = 0; i < 2; i++)
                {
                    joystickPressed[pollIndex++] = controller[joystickMatrix[i, j]] ? -1 : 0;
				}
			}

			// scan keyboard
            pollIndex = 0;
            for (int i = 0; i < 8; i++)
			{
                for (int j = 0; j < 8; j++)
				{
                    keyboardPressed[pollIndex++] = controller[keyboardMatrix[i, j]] ? -1 : 0;
				}
			}
		}

		private void WriteInputPort()
		{
            byte portA = cia0.PortAData;
            byte portB = cia0.PortBData;
			byte resultA = 0xFF;
			byte resultB = 0xFF;
			byte joyA = 0xFF;
			byte joyB = 0xFF;

            pollIndex = 0;
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
                    if (keyboardPressed[pollIndex++] != 0)
					{
						if (((portA & inputBitSelect[i]) == 0) || ((portB & inputBitSelect[j]) == 0))
						{
							resultA &= inputBitMask[i];
							resultB &= inputBitMask[j];
						}
					}
				}
			}

            pollIndex = 0;
			for (int i = 0; i < 5; i++)
			{
                if (joystickPressed[pollIndex++] != 0)
                    joyB &= inputBitMask[i];
                if (joystickPressed[pollIndex++] != 0)
					joyA &= inputBitMask[i];
			}

			resultA &= joyA;
			resultB &= joyB;

            cia0InputLatchA = resultA;
			cia0InputLatchB = resultB;

            // this joystick has special rules.
            cia0.PortAMask = joyA;
		}
	}
}
