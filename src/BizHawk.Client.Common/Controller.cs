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
			foreach (var kvp in Definition.Axes)
			{
				_axes[kvp.Key] = kvp.Value.Neutral;
				_axisRanges[kvp.Key] = kvp.Value;
			}
		}

		public ControllerDefinition Definition { get; private set; }

		public bool IsPressed(string button) => _buttons[button];

		public int AxisValue(string name) => _axes[name];

		private readonly WorkingDictionary<string, List<string>> _bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> _buttons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _axes = new WorkingDictionary<string, int>();
		private readonly Dictionary<string, AxisSpec> _axisRanges = new WorkingDictionary<string, AxisSpec>();
		private readonly Dictionary<string, AnalogBind> _axisBindings = new Dictionary<string, AnalogBind>();
		private readonly Dictionary<string, FeedbackBind> _feedbackBindings = new Dictionary<string, FeedbackBind>();

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

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController finalHostController)
		{
			_buttons.Clear();
			
			foreach (var kvp in _bindings)
			{
				_buttons[kvp.Key] = false;
				foreach (var button in kvp.Value)
				{
					if (finalHostController.IsPressed(button))
					{
						_buttons[kvp.Key] = true;
					}
				}
			}

			foreach (var kvp in _axisBindings)
			{
				// values from finalHostController are ints in -10000..10000 (or 0..10000), so scale to -1..1, using floats to keep fractional part
				var value = finalHostController.AxisValue(kvp.Value.Value) / 10000.0f;

				// apply deadzone (and scale diminished range back up to -1..1)
				var deadzone = kvp.Value.Deadzone;
				if (value < -deadzone) value += deadzone;
				else if (value < deadzone) value = 0.0f;
				else value -= deadzone;
				value /= 1.0f - deadzone;

				// scale by user-set multiplier (which is 0..1, i.e. value can only shrink and is therefore still in -1..1)
				value *= kvp.Value.Mult;

				// -1..1 -> range
				var range = _axisRanges[kvp.Key];
				value *= Math.Max(range.Neutral - range.Min, range.Max - range.Neutral);
				value += range.Neutral;

				// finally, constrain to range again in case the original value was unexpectedly large, or the deadzone and scale made it so, or the axis is lopsided
				_axes[kvp.Key] = ((int) value).ConstrainWithin(range.Range);
			}
		}

		public void PrepareHapticsForHost(SimpleController finalHostController, int debug)
		{
			foreach (var kvp in _feedbackBindings)
			{
				finalHostController.SetHapticChannelStrength(kvp.Value.GamepadPrefix + kvp.Value.Channel, (int) ((double) debug * kvp.Value.Prescale));
			}
		}

		public void ApplyAxisConstraints(string constraintClass)
			=> Definition.ApplyAxisConstraints(constraintClass, _axes);

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

			foreach (var button in controller.AxisOverrides)
			{
				_axes[button] = controller.AxisValue(button);
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

		public void BindAxis(string button, AnalogBind bind)
		{
			_axisBindings[button] = bind;
		}

		public void BindFeedbackChannel(string channel, FeedbackBind binding) => _feedbackBindings[channel] = binding;

		public List<string> PressedButtons => _buttons
			.Where(kvp => kvp.Value)
			.Select(kvp => kvp.Key)
			.ToList();
	}
}