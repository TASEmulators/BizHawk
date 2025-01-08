using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
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
			internal_frame = _emulator.Frame;
		}

		private readonly IEmulator _emulator;

		private readonly Dictionary<string, List<string>> _bindings = new();

		private Dictionary<string, bool> _buttons = new();

		private readonly Dictionary<string, int> _buttonStarts = new();

		public int On { get; set; }
		public int Off { get; set; }

		private int internal_frame;

		public ControllerDefinition Definition => _emulator.ControllerDefinition;

		public bool IsPressed(string button)
			=> _buttons.GetValueOrDefault(button)
				&& ((internal_frame - _buttonStarts.GetValueOrDefault(button)) % (On + Off)) < On;

		public void ClearStarts()
		{
			_buttonStarts.Clear();
		}

		public int AxisValue(string name)
#if DEBUG
			=> Definition.Axes[name].Neutral; // throw if no such axis
#else
			=> Definition.Axes.TryGetValue(name, out var axisSpec) ? axisSpec.Neutral : default;
#endif

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
			internal_frame = _emulator.Frame;
			var buttonStatesPrev = _buttons;
			_buttons = new(); //TODO swap A/B instead of allocating
			foreach (var (k, v) in _bindings)
			{
				var isPressed = false;
				foreach (var boundBtn in v)
				{
					if (controller.IsPressed(boundBtn))
					{
						isPressed = true;
						if (!buttonStatesPrev.GetValueOrDefault(k)) _buttonStarts[k] = internal_frame;
					}
				}
				_buttons[k] = isPressed;
			}
		}

		public void BindMulti(string button, string controlString)
		{
			if (!string.IsNullOrEmpty(controlString))
			{
				var controlBindings = controlString.Split(',');
				foreach (var control in controlBindings)
				{
					_bindings.GetValueOrPutNew(button).Add(control.Trim());
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
