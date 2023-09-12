using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	public class KempstonJoystick : IJoystick
	{
		private readonly SpectrumBase _machine;

		public KempstonJoystick(SpectrumBase machine, int playerNumber)
		{
			_machine = machine;
			JoyLine = 0;
			PlayerNumber = playerNumber;

			ButtonCollection = new List<string>
			{
				"P" + PlayerNumber + " Right",
				"P" + PlayerNumber + " Left",
				"P" + PlayerNumber + " Down",
				"P" + PlayerNumber + " Up",
				"P" + PlayerNumber + " Button",
			}.ToArray();
		}

		public JoystickType JoyType => JoystickType.Kempston;

		public string[] ButtonCollection { get; set; }
		public int PlayerNumber { get; set; }

		/// <summary>
		/// Sets the joystick line based on key pressed
		/// </summary>
		public void SetJoyInput(string key, bool isPressed)
		{
			int pos = GetBitPos(key);
			if (isPressed)
				JoyLine |= (1 << pos);
			else
				JoyLine &= ~(1 << pos);
		}

		/// <summary>
		/// Gets the state of a particular joystick binding
		/// </summary>
		public bool GetJoyInput(string key)
		{
			int pos = GetBitPos(key);
			return (JoyLine & (1 << pos)) != 0;
		}

		/// <summary>
		/// Active bits high
		/// 0 0 0 F U D L R
		/// </summary>
		public int JoyLine { get; set; }

		/// <summary>
		/// Gets the bit position of a particular joystick binding from the matrix
		/// </summary>
		public int GetBitPos(string key)
		{
			int index = Array.IndexOf(ButtonCollection, key);
			return index;
		}


		/*
       public readonly string[] _bitPos = new string[]
       {
           "P1 Right",
           "P1 Left",
           "P1 Down",
           "P1 Up",
           "P1 Button"
       };
       */
	}
}
