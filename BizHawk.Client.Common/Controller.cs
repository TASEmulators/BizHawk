using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Controller : IController
	{
		public Controller(ControllerDefinition definition)
		{
			Definition = definition;
			for (int i = 0; i < Definition.FloatControls.Count; i++)
			{
				_floatButtons[Definition.FloatControls[i]] = Definition.FloatRanges[i].Mid;
				_floatRanges[Definition.FloatControls[i]] = Definition.FloatRanges[i];
			}
		}

		public ControllerDefinition Definition { get; private set; }

		public bool IsPressed(string button) => _buttons[button];

		public float GetFloat(string name) => _floatButtons[name];

		private readonly WorkingDictionary<string, List<string>> _bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> _buttons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _floatButtons = new WorkingDictionary<string, float>();
		private readonly Dictionary<string, ControllerDefinition.AxisRange> _floatRanges = new WorkingDictionary<string, ControllerDefinition.AxisRange>();
		private readonly Dictionary<string, AnalogBind> _floatBinds = new Dictionary<string, AnalogBind>();

		/// <summary>don't do this</summary>
		public void ForceType(ControllerDefinition newType) => Definition = newType;

		public bool this[string button] => IsPressed(button);

		// Looks for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button) =>
			_bindings
				.Where(b => b.Value.Any(v => v == button))
				.Select(b => b.Key)
				.ToList();

		// Searches bindings for the controller and returns true if this binding is mapped somewhere in this controller
		public bool HasBinding(string button) =>
			_bindings
				.SelectMany(kvp => kvp.Value)
				.Any(boundButton => boundButton == button);

		public void NormalizeFloats(IController controller)
		{
			foreach (var kvp in _floatBinds)
			{
				var input = _floatButtons[kvp.Key];
				string outKey = kvp.Key;
				float multiplier = kvp.Value.Mult;
				float deadZone = kvp.Value.Deadzone;
				if (_floatRanges.TryGetValue(outKey, out var range))
				{
					// input range is assumed to be -10000,0,10000

					// first, modify for deadZone
					float absInput = Math.Abs(input);
					float zeroPoint = deadZone * 10000.0f;
					if (absInput < zeroPoint)
					{
						input = 0.0f;
					}
					else
					{
						absInput -= zeroPoint;
						absInput *= 10000.0f;
						absInput /= 10000.0f - zeroPoint;
						input = absInput * Math.Sign(input);
					}

					// zero 09-mar-2015 - not sure if adding + 1 here is correct.. but... maybe?
					float output;

					if (range.IsReversed)
					{
						output = (((input * multiplier) + 10000.0f) * (range.Min - range.Max + 1) / 20000.0f) + range.Max;
					}
					else
					{
						output = (((input * multiplier) + 10000.0f) * (range.Max - range.Min + 1) / 20000.0f) + range.Min;
					}

					// zero 09-mar-2015 - at this point, we should only have integers, since that's all 100% of consoles ever see
					// if this becomes a problem we can add flags to the range and update GUIs to be able to display floats

					_floatButtons[outKey] = output.ConstrainWithin(range.FloatRange);
				}
			}
		}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
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

			foreach (var kvp in _floatBinds)
			{
				var input = controller.GetFloat(kvp.Value.Value);
				string outKey = kvp.Key;
				if (_floatRanges.ContainsKey(outKey))
				{
					_floatButtons[outKey] = input;
				}
			}

			// it's not sure where this should happen, so for backwards compatibility.. do it every time
			NormalizeFloats(controller);
		}

		public void ApplyAxisConstraints(string constraintClass)
			=> Definition.ApplyAxisConstraints(constraintClass, _floatButtons);

		/// <summary>
		/// merges pressed logical buttons from the supplied controller, effectively ORing it with the current state
		/// </summary>
		public void OR_FromLogical(IController controller)
		{
			// change: or from each button that the other input controller has
			// foreach (string button in type.BoolButtons)
			if (controller.Definition != null)
			{
				foreach (var button in controller.Definition.BoolButtons)
				{
					if (controller.IsPressed(button))
					{
						_buttons[button] = true;
					}
				}
			}
		}

		public void Overrides(OverrideAdapter controller)
		{
			foreach (var button in controller.Overrides)
			{
				_buttons[button] = controller.IsPressed(button);
			}

			foreach (var button in controller.FloatOverrides)
			{
				_floatButtons[button] = controller.GetFloat(button);
			}

			foreach (var button in controller.InversedButtons)
			{
				_buttons[button] ^= true;
			}
		}

		public void BindMulti(string button, string controlString)
		{
			if (string.IsNullOrEmpty(controlString))
			{
				return;
			}

			var controlBindings = controlString.Split(',');
			foreach (var control in controlBindings)
			{
				_bindings[button].Add(control.Trim());
			}
		}

		public void BindFloat(string button, AnalogBind bind)
		{
			_floatBinds[button] = bind;
		}

		public List<string> PressedButtons => _buttons
			.Where(kvp => kvp.Value)
			.Select(kvp => kvp.Key)
			.ToList();
	}
}