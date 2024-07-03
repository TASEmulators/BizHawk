using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Sinclair Joystick LEFT
	/// Just maps to the standard keyboard and is read the same (from port 0xf7fe)
	/// </summary>
	public class SinclairJoystick1 : IJoystick
	{
		//private int _joyLine;
		private readonly SpectrumBase _machine;

		public SinclairJoystick1(SpectrumBase machine, int playerNumber)
		{
			_machine = machine;
			//_joyLine = 0;
			_playerNumber = playerNumber;

			ButtonCollection = new List<string>
			{
				"P" + _playerNumber + " Left",
				"P" + _playerNumber + " Right",
				"P" + _playerNumber + " Down",
				"P" + _playerNumber + " Up",
				"P" + _playerNumber + " Button",
			}.ToArray();
		}

		private readonly List<string> btnLookups = new List<string>
		{
			"Key 1",    // left
            "Key 2",    // right
            "Key 3",    // down
            "Key 4",    // up
            "Key 5",    // fire
        };

		public JoystickType JoyType => JoystickType.SinclairLEFT;

		public string[] ButtonCollection { get; set; }

		private int _playerNumber;
		public int PlayerNumber
		{
			get => _playerNumber;
			set => _playerNumber = value;
		}

		/// <summary>
		/// Sets the joystick line based on key pressed
		/// </summary>
		public void SetJoyInput(string key, bool isPressed)
		{
			var pos = GetBitPos(key);

			if (isPressed)
			{
				_machine.KeyboardDevice.SetKeyStatus(btnLookups[pos], true);
			}
			else
			{
				if (_machine.KeyboardDevice.GetKeyStatus(btnLookups[pos]))
				{
					// key is already pressed elswhere - leave it as is
				}
				else
				{
					// key is safe to unpress
					_machine.KeyboardDevice.SetKeyStatus(btnLookups[pos], false);
				}
			}
		}

		/// <summary>
		/// Gets the state of a particular joystick binding
		/// </summary>
		public bool GetJoyInput(string key)
		{
			var pos = GetBitPos(key);
			if (_machine == null)
				return false;

			return _machine.KeyboardDevice.GetKeyStatus(btnLookups[pos]);
		}

		/// <summary>
		/// Gets the bit position of a particular joystick binding from the matrix
		/// </summary>
		public int GetBitPos(string key)
		{
			int index = Array.IndexOf(ButtonCollection, key);
			return index;
		}
	}
}
