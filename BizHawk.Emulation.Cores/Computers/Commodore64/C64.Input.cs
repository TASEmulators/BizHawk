namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class Motherboard
	{
		private readonly int[] _joystickPressed = new int[10];
		private readonly int[] _keyboardPressed = new int[64];

		private static readonly string[,] JoystickMatrix = {
			{"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"},
			{"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"}
		};

		private static readonly string[,] KeyboardMatrix = {
			{ "Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Key Cursor Up/Down" },
			{ "Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift" },
			{ "Key 5", "Key R", "Key D", "Key 6", "Key C", "Key F", "Key T", "Key X" },
			{ "Key 7", "Key Y", "Key G", "Key 8", "Key B", "Key H", "Key U", "Key V" },
			{ "Key 9", "Key I", "Key J", "Key 0", "Key M", "Key K", "Key O", "Key N" },
			{ "Key Plus", "Key P", "Key L", "Key Minus", "Key Period", "Key Colon", "Key At", "Key Comma" },
			{ "Key Pound", "Key Asterisk", "Key Semicolon", "Key Clear/Home", "Key Right Shift", "Key Equal", "Key Up Arrow", "Key Slash" },
			{ "Key 1", "Key Left Arrow", "Key Control", "Key 2", "Key Space", "Key Commodore", "Key Q", "Key Run/Stop" }
		};

	    [SaveState.DoNotSave] int _pollIndex;

		public void PollInput()
		{
			_c64.InputCallbacks.Call();
			// scan joysticks
			_pollIndex = 0;
			for (var j = 0; j < 2; j++)
			{
				for (var i = 0; i < 5; i++)
				{
					_joystickPressed[_pollIndex++] = Controller[JoystickMatrix[j, i]] ? -1 : 0;
				}
			}

			// scan keyboard
			_pollIndex = 0;
			for (var i = 0; i < 8; i++)
			{
				for (var j = 0; j < 8; j++)
				{
					_keyboardPressed[_pollIndex++] = Controller[KeyboardMatrix[i, j]] ? -1 : 0;
				}
			}
		}
	}
}
