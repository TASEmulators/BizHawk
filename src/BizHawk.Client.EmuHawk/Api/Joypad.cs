using System;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This class holds a joypad for any type of console
	/// </summary>
	public sealed class Joypad
	{
		private JoypadButton _pressedButtons;
		private float _analogX;
		private float _analogY;
		private int _player;

		/// <summary>
		/// Initialize a new instance of <see cref="Joypad"/>
		/// </summary>
		/// <param name="system">What <see cref="SystemInfo"/> this <see cref="Joypad"/> is used for</param>
		/// <param name="player">Which player this controller is assigned to</param>
		/// <exception cref="IndexOutOfRangeException"><paramref name="player"/> not in range <c>1..max</c> where <c>max</c> is <paramref name="system"/>.<see cref="SystemInfo.MaxControllers"/></exception>
		internal Joypad(SystemInfo system, int player)
		{
			if (!1.RangeTo(system.MaxControllers).Contains(player))
			{
				throw new InvalidOperationException($"{player} is invalid for {system.DisplayName}");
			}

			System = system;
			_player = player;
		}

		/// <summary>
		/// Add specified input to current ones
		/// </summary>
		/// <param name="input">Input to add</param>
		public void AddInput(JoypadButton input)
		{
			input &= System.AvailableButtons;
			_pressedButtons |= input;
		}

		/// <summary>
		/// Clear inputs
		/// </summary>
		public void ClearInputs()
		{
			_pressedButtons = 0;
		}

		/// <summary>
		/// Remove specified input to current ones
		/// </summary>
		/// <param name="input">Input to remove</param>
		public void RemoveInput(JoypadButton input)
		{
			_pressedButtons ^= input;
		}

		/// <summary>
		/// Gets or sets X value for Analog stick
		/// </summary>
		/// <remarks>The value you get will always be rounded to 0 decimal</remarks>
		public float AnalogX
		{
			get => (float)Math.Round(_analogX, 0);
			set => _analogX = value;
		}

		/// <summary>
		/// Gets or sets Y value for Analog stick
		/// </summary>
		/// <remarks>The value you get will always be rounded to 0 decimal</remarks>
		public float AnalogY
		{
			get => (float)Math.Round(_analogY, 0);
			set => _analogY = value;
		}

		/// <summary>
		/// Gets or sets inputs
		/// If you pass inputs unavailable for current system, they'll be removed
		/// </summary>
		/// <remarks>It overrides all existing inputs</remarks>
		public JoypadButton Inputs
		{
			get => _pressedButtons;
			set
			{
				value &= System.AvailableButtons;
				_pressedButtons = value;
			}
		}

		/// <summary>
		/// Gets <see cref="SystemInfo"/> for current <see cref="Joypad"/>
		/// </summary>
		public SystemInfo System { get; }
	}
}
