using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieRecord
	{
		private byte[] _state = new byte[0];
		private Dictionary<string, bool> _boolButtons = new Dictionary<string, bool>();

		public MovieRecord(Dictionary<string, bool> buttons, bool captureState)
		{
			SetInput(buttons);
			if (captureState)
			{
				Lagged = Global.Emulator.IsLagFrame;
				_state = Global.Emulator.SaveStateBinary();
			}
		}

		public Dictionary<string, bool> Buttons
		{
			get { return _boolButtons; }
		}

		public bool Lagged { get; private set; }

		#region Input Api

		public bool IsPressed(string buttonName)
		{
			return _boolButtons[buttonName];
		}

		public void SetButton(string button, bool pressed)
		{
			InputChanged(new Dictionary<string, bool>() { { button, pressed } });
			_boolButtons[button] = pressed;
		}

		public void SetInput(Dictionary<string, bool> buttons)
		{
			InputChanged(buttons);
			_boolButtons.Clear();
			_boolButtons = buttons;
		}

		public void ClearInput()
		{
			InputChanged(_boolButtons);
			_boolButtons.Clear();
		}

		#endregion

		#region State API

		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool HasState
		{
			get { return State.Any(); }
		}

		public void ClearState()
		{
			_state = new byte[0];
		}

		#endregion

		#region Event Handling

		public class InputEventArgs
		{
			public InputEventArgs(Dictionary<string, bool> editedButtons)
			{
				EditedButtons = editedButtons;
			}

			public Dictionary<string, bool> EditedButtons { get; private set; }
		}

		public delegate void InputEventHandler(object sender, InputEventArgs e);
		public event InputEventHandler OnChanged;

		private void InputChanged(Dictionary <string, bool> editedButtons)
		{
			if (OnChanged != null) 
			{
				OnChanged(this, new InputEventArgs(editedButtons));
			}
		}

		#endregion
	}
}
