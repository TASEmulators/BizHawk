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
			_type = definition;
			for (int i = 0; i < _type.FloatControls.Count; i++)
			{
				_floatButtons[_type.FloatControls[i]] = _type.FloatRanges[i].Mid;
				_floatRanges[_type.FloatControls[i]] = _type.FloatRanges[i];
			}
		}

		public ControllerDefinition Definition
		{
			get { return _type; }
		}

		public bool IsPressed(string button)
		{
			return _buttons[button];
		}

		public float GetFloat(string name)
		{
			return _floatButtons[name];
		}

		private readonly WorkingDictionary<string, List<string>> _bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> _buttons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _floatButtons = new WorkingDictionary<string, float>();
		private readonly Dictionary<string, ControllerDefinition.FloatRange> _floatRanges = new WorkingDictionary<string, ControllerDefinition.FloatRange>();
		private readonly Dictionary<string, Config.AnalogBind> _floatBinds = new Dictionary<string, Config.AnalogBind>();

		private ControllerDefinition _type;

		/// <summary>don't do this</summary>
		public void ForceType(ControllerDefinition newtype) { _type = newtype; }

		public bool this[string button]
		{
			get { return IsPressed(button); }
		}

		public bool AnyPressed
		{
			get
			{
				if (_buttons.Any(x => x.Value))
				{
					return true;
				}

				return _floatButtons.Any(x => x.Value != 0);
			}
		}

		// Looks for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button)
		{
			return (from kvp in _bindings from bound_button in kvp.Value where bound_button == button select kvp.Key).ToList();
		}

		// Searches bindings for the controller and returns true if this binding is mapped somewhere in this controller
		public bool HasBinding(string button)
		{
			return _bindings.SelectMany(kvp => kvp.Value).Any(boundButton => boundButton == button);
		}

		public void NormalizeFloats(IController controller)
		{
			foreach (var kvp in _floatBinds)
			{
				var input = _floatButtons[kvp.Key];
				string outkey = kvp.Key;
				float multiplier = kvp.Value.Mult;
				float deadzone = kvp.Value.Deadzone;
				ControllerDefinition.FloatRange range;
				if (_floatRanges.TryGetValue(outkey, out range))
				{
					// input range is assumed to be -10000,0,10000

					// first, modify for deadzone
					{
						float absinput = Math.Abs(input);
						float zeropoint = deadzone * 10000.0f;
						if (absinput < zeropoint)
						{
							input = 0.0f;
						}
						else
						{
							absinput -= zeropoint;
							absinput *= 10000.0f;
							absinput /= 10000.0f - zeropoint;
							input = absinput * Math.Sign(input);
						}
					}

					//zero 09-mar-2015 - not sure if adding + 1 here is correct.. but... maybe?
					var output = (input * multiplier + 10000.0f) * (range.Max - range.Min + 1) / 20000.0f + range.Min;

					//zero 09-mar-2015 - at this point, we should only have integers, since thats all 100% of consoles ever see
					//if this becomes a problem we can add flags to the range and update GUIs to be able to display floats
					output = (int)output;

					float lbound = Math.Min(range.Min, range.Max);
					float ubound = Math.Max(range.Min, range.Max);

					if (output < lbound)
					{
						output = lbound;
					}

					if (output > ubound)
					{
						output = ubound;
					}

					_floatButtons[outkey] = output;
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
				foreach (var bound_button in kvp.Value)
				{
					if (controller.IsPressed(bound_button))
					{
						_buttons[kvp.Key] = true;
					}
				}
			}

			foreach (var kvp in _floatBinds)
			{
				var input = controller.GetFloat(kvp.Value.Value);
				string outkey = kvp.Key;
				if (_floatRanges.ContainsKey(outkey))
				{
					_floatButtons[outkey] = input;
				}
			}

			//it's not sure where this should happen, so for backwards compatibility.. do it every time
			NormalizeFloats(controller);
		}

		public void ApplyAxisConstraints(string constraintClass)
		{
			_type.ApplyAxisConstraints(constraintClass,_floatButtons);
		}

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

		public void Overrides(OverrideAdaptor controller)
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

		public void BindButton(string button, string control)
		{
			_bindings[button].Add(control);
		}

		public void BindMulti(string button, string controlString)
		{
			if (string.IsNullOrEmpty(controlString))
			{
				return;
			}

			var controlbindings = controlString.Split(',');
			foreach (var control in controlbindings)
			{
				_bindings[button].Add(control.Trim());
			}
		}

		public void BindFloat(string button, Config.AnalogBind bind)
		{
			_floatBinds[button] = bind;
		}

		/// <summary>
		/// Returns a list of all keys mapped and the name of the button they are mapped to
		/// </summary>
		public List<KeyValuePair<string, string>> MappingList()
		{
			return (from key in _bindings from binding in key.Value select new KeyValuePair<string, string>(binding, key.Key)).ToList();
		}

		public List<string> PressedButtons
		{
			get
			{
				return (from button in _buttons where button.Value select button.Key).ToList();
			}
		}
	}

	public class AutofireController : IController
	{
		public AutofireController(ControllerDefinition definition, IEmulator emulator)
		{
			On = Global.Config.AutofireOn < 1 ? 0 : Global.Config.AutofireOn;
			Off = Global.Config.AutofireOff < 1 ? 0 : Global.Config.AutofireOff;
			_type = definition;
			_emulator = emulator;
		}

		private readonly IEmulator _emulator;

		private readonly ControllerDefinition _type;
		private readonly WorkingDictionary<string, List<string>> _bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> _buttons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _buttonStarts = new WorkingDictionary<string, int>();

		private bool _autofire = true;

		public bool Autofire { get { return false; } set { _autofire = value; } }
		public int On { get; set; }
		public int Off { get; set; }

		public ControllerDefinition Definition { get { return _type; } }

		public bool IsPressed(string button)
		{
			if (_autofire)
			{
				var a = (_emulator.Frame - _buttonStarts[button]) % (On + Off);
				return a < On && _buttons[button];
			}

			return _buttons[button];
		}

		public void ClearStarts()
		{
			_buttonStarts.Clear();
		}

		public float GetFloat(string name) { throw new NotImplementedException(); }

		// look for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button)
		{
			return (from kvp in _bindings from bound_button in kvp.Value where bound_button == button select kvp.Key).ToList();
		}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
			foreach (var kvp in _bindings)
			{
				foreach (var bound_button in kvp.Value)
				{
					if (_buttons[kvp.Key] == false && controller.IsPressed(bound_button))
					{
						_buttonStarts[kvp.Key] = _emulator.Frame;
					}
				}
			}
			
			_buttons.Clear();
			foreach (var kvp in _bindings)
			{
				_buttons[kvp.Key] = false;
				foreach (var bound_button in kvp.Value)
				{
					if (controller.IsPressed(bound_button))
					{
						_buttons[kvp.Key] = true;
					}
				}
			}
		}

		/// <summary>
		/// merges pressed logical buttons from the supplied controller, effectively ORing it with the current state
		/// </summary>
		public void OR_FromLogical(IController controller)
		{
			foreach (var button in _type.BoolButtons.Where(controller.IsPressed))
			{
				_buttons[button] = true;
				Console.WriteLine(button);
			}
		}

		public void BindButton(string button, string control)
		{
			_bindings[button].Add(control);
		}

		public void BindMulti(string button, string controlString)
		{
			if (!string.IsNullOrEmpty(controlString))
			{
				var controlbindings = controlString.Split(',');
				foreach (var control in controlbindings)
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

		public List<string> PressedButtons
		{
			get
			{
				return (from button in _buttons where button.Value select button.Key).ToList();
			}
		}
	}
}