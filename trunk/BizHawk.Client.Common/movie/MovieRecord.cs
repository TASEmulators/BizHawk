using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieRecord
	{
		private readonly byte[] _state;
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

		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool IsPressed(string buttonName)
		{
			return _boolButtons[buttonName];
		}

		public void SetButton(string button, bool pressed)
		{
			_boolButtons[button] = pressed;
		}

		public void SetInput(Dictionary<string, bool> buttons)
		{
			_boolButtons.Clear();
			_boolButtons = buttons;
		}

		public void ClearInput()
		{
			_boolButtons.Clear();
		}

		public bool HasState
		{
			get { return State.Any(); }
		}
	}
}
