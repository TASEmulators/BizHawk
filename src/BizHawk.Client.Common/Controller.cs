using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Controller : IController
	{
		public Controller(ControllerDefinition definition)
		{
			Definition = definition;
			foreach (var (k, v) in Definition.Axes)
			{
				_axes[k] = v.Neutral;
				_axisRanges[k] = v;
			}
			foreach (var channel in Definition.HapticsChannels) _haptics[channel] = 0;
		}

		public ControllerDefinition Definition { get; }

		public bool IsPressed(string button)
			=> _buttons.GetValueOrDefault(button);

		public int AxisValue(string name)
			=> _axes.GetValueOrDefault(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot()
			=> _haptics.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

		public void SetHapticChannelStrength(string name, int strength) => _haptics[name] = strength;

		private readonly Dictionary<string, List<string>> _bindings = new();

		private readonly Dictionary<string, bool> _buttons = new();

		private readonly Dictionary<string, int> _axes = new();

		private readonly Dictionary<string, AxisSpec> _axisRanges = new();

		private readonly Dictionary<string, AnalogBind> _axisBindings = new Dictionary<string, AnalogBind>();

		private readonly Dictionary<string, int> _haptics = new();

		private readonly Dictionary<string, FeedbackBind> _feedbackBindings = new Dictionary<string, FeedbackBind>();

		public bool this[string button] => IsPressed(button);

		// Looks for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button)
			=> _bindings.Where(b => b.Value.Contains(button)).Select(static b => b.Key).ToList();

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
			
			foreach (var (k, v) in _bindings)
			{
				_buttons[k] = false;
				foreach (var button in v)
				{
					if (finalHostController.IsPressed(button))
					{
						_buttons[k] = true;
						break;
					}
				}
			}

			foreach (var (k, v) in _axisBindings)
			{
				// values from finalHostController are ints in -10000..10000 (or 0..10000), so scale to -1..1, using floats to keep fractional part
				var value = finalHostController.AxisValue(v.Value) / 10000.0f;

				// apply deadzone (and scale diminished range back up to -1..1)
				var deadzone = v.Deadzone;
				if (value < -deadzone) value += deadzone;
				else if (value < deadzone) value = 0.0f;
				else value -= deadzone;
				value /= 1.0f - deadzone;

				// scale by user-set multiplier (which is -2..2, therefore value is now in -2..2)
				value *= v.Mult;

				// -1..1 -> -A..A (where A is the larger "side" of the range e.g. a range of 0..50, neutral=10 would give A=40, and thus a value in -40..40)
				var range = _axisRanges[k]; // this was `GetValueOrPutNew`, but I really hope it was always found, since `new AxisSpec()` isn't valid --yoshi
				value *= Math.Max(range.Neutral - range.Min, range.Max - range.Neutral);

				// shift the midpoint, so a value of 0 becomes range.Neutral (and, assuming >=1x multiplier, all values in range are reachable)
				value += range.Neutral;

				// finally, constrain to range
				_axes[k] = ((int)Math.Round(value)).ConstrainWithin(range.Range);
			}
		}

		public void PrepareHapticsForHost(SimpleController finalHostController)
		{
			foreach (var (k, v) in _feedbackBindings)
			{
				if (_haptics.TryGetValue(k, out var strength))
				{
					foreach (var hostChannel in v.Channels!.Split('+'))
					{
						const double S32_MAX_AS_F64 = int.MaxValue;
						finalHostController.SetHapticChannelStrength(
							v.GamepadPrefix + hostChannel,
							(v.Prescale * (double) strength).Clamp(min: 0.0, max: S32_MAX_AS_F64).RoundToInt());
					}
				}
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

			foreach (var button in controller.InversedButtons) _buttons[button] = !_buttons.GetValueOrDefault(button);
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
				_bindings.GetValueOrPutNew(button).Add(control.Trim());
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
