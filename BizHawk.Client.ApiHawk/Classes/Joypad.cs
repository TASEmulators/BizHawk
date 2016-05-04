using System;
using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class holds a joypad for any type of console
	/// </summary>
	public sealed class Joypad
	{
		#region Fields

		private SystemInfo _System;
		private JoypadButton _PressedButtons;
		private float _AnalogX;
		private float _AnalogY;
		private int _Player;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="Joypad"/>
		/// </summary>
		/// <param name="system">What <see cref="SystemInfo"/> this <see cref="Joypad"/> is used for</param>
		/// <param name="player">Which player this controller is assigned to</param>
		internal Joypad(SystemInfo system, int player)
		{
			if (player < 1 || player > system.MaxControllers)
			{
				throw new InvalidOperationException(string.Format("{0} is invalid for {1}", player, system.DisplayName));
			}

			_System = system;
			_Player = player;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Add specified input to current ones
		/// </summary>
		/// <param name="input">Input to add</param>
		public void AddInput(JoypadButton input)
		{
			input &= _System.AvailableButtons;
			_PressedButtons |= input;
		}


		/// <summary>
		/// Clear inputs
		/// </summary>
		public void ClearInputs()
		{
			_PressedButtons = 0;
		}


		/// <summary>
		/// Remove specified input to current ones
		/// </summary>
		/// <param name="input">Input to remove</param>
		public void RemoveInput(JoypadButton input)
		{
			_PressedButtons ^= input;
		}

		#endregion


		#region Properties

		/// <summary>
		/// Gets or sets X value for Analog stick
		/// </summary>
		/// <remarks>The value you get will aways be rounded to 0 decimal</remarks>
		public float AnalogX
		{
			get
			{
				return (float)Math.Round(_AnalogX, 0);
			}
			set
			{
				_AnalogX = value;
			}
		}

		/// <summary>
		/// Gets or sets Y value for Analog stick
		/// </summary>
		/// <remarks>The value you get will aways be rounded to 0 decimal</remarks>
		public float AnalogY
		{
			get
			{
				return (float)Math.Round(_AnalogY, 0);
			}
			set
			{
				_AnalogY = value;
			}
		}

		/// <summary>
		/// Gets or sets inputs
		/// If you pass inputs unavailable for current system, they'll be removed
		/// </summary>
		/// <remarks>It overrides all existing inputs</remarks>
		public JoypadButton Inputs
		{
			get
			{
				return _PressedButtons;
			}
			set
			{
				value &= _System.AvailableButtons;
				_PressedButtons = value;
			}
		}

		/// <summary>
		/// Gets <see cref="SystemInfo"/> for current <see cref="Joypad"/>
		/// </summary>
		public SystemInfo System
		{
			get
			{
				return _System;
			}
		}

		#endregion
	}
}
