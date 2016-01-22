namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class Motherboard
	{
		private readonly int[] joystickPressed = new int[10];
		private readonly int[] keyboardPressed = new int[64];

		private static readonly string[,] joystickMatrix = {
			{"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"},
			{"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"}
		};

		private static readonly string[,] keyboardMatrix = {
			{ "Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Key Cursor Up/Down" },
			{ "Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift" },
			{ "Key 5", "Key R", "Key D", "Key 6", "Key C", "Key F", "Key T", "Key X" },
			{ "Key 7", "Key Y", "Key G", "Key 8", "Key B", "Key H", "Key U", "Key V" },
			{ "Key 9", "Key I", "Key J", "Key 0", "Key M", "Key K", "Key O", "Key N" },
			{ "Key Plus", "Key P", "Key L", "Key Minus", "Key Period", "Key Colon", "Key At", "Key Comma" },
			{ "Key Pound", "Key Asterisk", "Key Semicolon", "Key Clear/Home", "Key Right Shift", "Key Equal", "Key Up Arrow", "Key Slash" },
			{ "Key 1", "Key Left Arrow", "Key Control", "Key 2", "Key Space", "Key Commodore", "Key Q", "Key Run/Stop" }
		};

		private static readonly byte[] inputBitMask = { 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F };
		private static readonly byte[] inputBitSelect = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };

        int cia0InputLatchA;
        int cia0InputLatchB;
		int pollIndex;

		public void PollInput()
		{
			_c64.InputCallbacks.Call();
			// scan joysticks
			pollIndex = 0;
			for (var j = 0; j < 5; j++)
			{
				for (var i = 0; i < 2; i++)
				{
					joystickPressed[pollIndex++] = Controller[joystickMatrix[i, j]] ? -1 : 0;
				}
			}

			// scan keyboard
			pollIndex = 0;
			for (var i = 0; i < 8; i++)
			{
				for (var j = 0; j < 8; j++)
				{
					keyboardPressed[pollIndex++] = Controller[keyboardMatrix[i, j]] ? -1 : 0;
				}
			}
		}

		private void WriteInputPort()
		{
			var portA = Cia0.PortAData;
			var portB = Cia0.PortBData;
			var resultA = 0xFF;
			var resultB = 0xFF;
			var joyA = 0xFF;
			var joyB = 0xFF;

			pollIndex = 0;
			for (var i = 0; i < 8; i++)
			{
				for (var j = 0; j < 8; j++)
				{
				    if (keyboardPressed[pollIndex++] != 0 &&
				        (((portA & inputBitSelect[i]) == 0) || ((portB & inputBitSelect[j]) == 0)))
				    {
				        resultA &= inputBitMask[i];
				        resultB &= inputBitMask[j];
				    }
				}
			}

			pollIndex = 0;
			for (var i = 0; i < 5; i++)
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
			Cia0.PortAMask = joyA;
		}
	}
}
