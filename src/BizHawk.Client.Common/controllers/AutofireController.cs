using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class AutofireController : IController
	{
		public AutofireController(IEmulator emulator, int on, int off)
		{
			On = on < 1 ? 0 : on;
			Off = off < 1 ? 0 : off;
			_emulator = emulator;
		}

		private readonly IEmulator _emulator;

		private readonly WorkingDictionary<string, List<string>> _bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> _buttons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _buttonStarts = new WorkingDictionary<string, int>();

		public int On { get; set; }
		public int Off { get; set; }

		public ControllerDefinition Definition => _emulator.ControllerDefinition;

		public bool IsPressed(string button)
		{
			var a = (_emulator.Frame - _buttonStarts[button]) % (On + Off);
			return a < On && _buttons[button];
		}

		public void ClearStarts()
		{
			_buttonStarts.Clear();
		}

		/// <exception cref="NotImplementedException">always</exception>
		public int AxisValue(string name)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
			foreach (var kvp in _bindings)
			{
				foreach (var boundBtn in kvp.Value)
				{
					if (_buttons[kvp.Key] == false && controller.IsPressed(boundBtn))
					{
						_buttonStarts[kvp.Key] = _emulator.Frame;
					}
				}
			}
			
			_buttons.Clear();
			foreach (var kvp in _bindings)
			{
				_buttons[kvp.Key] = false;
				foreach (var button in kvp.Value)
				{
					if (controller.IsPressed(button))
					{
						_buttons[kvp.Key] = true;
					}
				}
			}
		}

		public void BindMulti(string button, string controlString)
		{
			if (!string.IsNullOrEmpty(controlString))
			{
				var controlBindings = controlString.Split(',');
				foreach (var control in controlBindings)
				{
					_bindings[button].Add(control.Trim());
				}
			}
		}

		public void IncrementStarts()
		{
			foreach (var key in _buttonStarts.Keys.ToArray())
			{
				_buttonStarts[key]++;
			}
		}

		public List<string> PressedButtons => _buttons
			.Where(kvp => kvp.Value)
			.Select(kvp => kvp.Key)
			.ToList();
	}
}
