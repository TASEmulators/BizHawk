using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class Controller : IController
	{
		private ControllerDefinition type;
		private readonly WorkingDictionary<string, List<string>> bindings = new WorkingDictionary<string, List<string>>();
		private readonly WorkingDictionary<string, bool> buttons = new WorkingDictionary<string, bool>();

		private readonly WorkingDictionary<string, float> FloatButtons = new WorkingDictionary<string, float>();

		private readonly Dictionary<string, ControllerDefinition.FloatRange> FloatRanges = new WorkingDictionary<string, ControllerDefinition.FloatRange>();

		private readonly Dictionary<string, string> FloatBinds = new Dictionary<string, string>();

		public Controller(ControllerDefinition definition)
		{
			type = definition;
			for (int i = 0; i < type.FloatControls.Count; i++)
			{
				FloatButtons[type.FloatControls[i]] = type.FloatRanges[i].Mid;
				FloatRanges[type.FloatControls[i]] = type.FloatRanges[i];
			}
			FloatBinds.Add("J5 X", "P1 X Axis");
			FloatBinds.Add("J5 Y", "P1 Y Axis");
		}

		public ControllerDefinition Type { get { return type; } }
		/// <summary>don't do this</summary>
		public void ForceType(ControllerDefinition newtype) { type = newtype; }
		public bool this[string button] { get { return IsPressed(button); } }
		public bool IsPressed(string button)
		{
			return buttons[button];
		}

		public float GetFloat(string name) { return FloatButtons[name]; }
		public void UpdateControls(int frame) { }

		//look for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button)
		{
			var ret = new List<string>();
			foreach (var kvp in bindings)
			{
				foreach (var bound_button in kvp.Value)
				{
					if (bound_button == button)
						ret.Add(kvp.Key);
				}
			}
			return ret;
		}

		//Searches bindings for the controller and returns true if this binding is mapped somewhere in this controller
		public bool HasBinding(string button)
		{
			foreach (var kvp in bindings)
			{
				foreach (var bound_button in kvp.Value)
				{
					if (bound_button == button)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
			buttons.Clear();
			foreach (var kvp in bindings)
			{
				buttons[kvp.Key] = false;
				foreach (var bound_button in kvp.Value)
				{
					if (controller[bound_button])
						buttons[kvp.Key] = true;
				}
			}
			foreach (var kvp in FloatBinds)
			{
				float input = controller.GetFloat(kvp.Key);
				string outkey = kvp.Value;
				ControllerDefinition.FloatRange range;
				if (FloatRanges.TryGetValue(outkey, out range))
				{
					// input range is assumed to be -10000,0,10000
					// this is where deadzone, axis flip, sensitivity would be implemented
					FloatButtons[outkey] = (input + 10000.0f) * (range.Max - range.Min) / 20000.0f + range.Min;
				}
			}
		}

		/// <summary>
		/// merges pressed logical buttons from the supplied controller, effectively ORing it with the current state
		/// </summary>
		public void OR_FromLogical(IController controller)
		{
			// change: or from each button that the other input controller has
			//foreach (string button in type.BoolButtons)
			if (controller.Type != null)
				foreach (string button in controller.Type.BoolButtons)
				{
					if (controller.IsPressed(button))
					{
						buttons[button] = true;
						//Console.WriteLine(button);
					}
				}
		}

		public void BindButton(string button, string control)
		{
			bindings[button].Add(control);
		}

		public void BindMulti(string button, string controlString)
		{
			if (string.IsNullOrEmpty(controlString))
				return;
			string[] controlbindings = controlString.Split(',');
			foreach (string control in controlbindings)
				bindings[button].Add(control.Trim());
		}

		/// <summary>
		/// Returns a list of all keys mapped and the name of the button they are mapped to
		/// </summary>
		/// <returns></returns>
		public List<KeyValuePair<string, string>> MappingList()
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

			foreach (KeyValuePair<string, List<string>> key in bindings)
			{
				foreach (string binding in key.Value)
				{
					list.Add(new KeyValuePair<string, string>(binding, key.Key));
				}
			}

			return list;
		}

		public List<string> PressedButtons
		{
			get
			{
				return (from button in buttons where button.Value select button.Key).ToList();
			}
		}
	}

	public class AutofireController : IController
	{
		private ControllerDefinition type;
		private WorkingDictionary<string, List<string>> bindings = new WorkingDictionary<string, List<string>>();
		private WorkingDictionary<string, bool> buttons = new WorkingDictionary<string, bool>();
		public WorkingDictionary<string, int> buttonStarts = new WorkingDictionary<string, int>();

		private bool autofire = true;
		public bool Autofire { get { return false; } set { autofire = value; } }
		public int On { get; set; }
		public int Off { get; set; }

		public AutofireController(ControllerDefinition definition)
		{
			
			On = Global.Config.AutofireOn < 1 ? 0 : Global.Config.AutofireOn;
			Off = Global.Config.AutofireOff < 1 ? 0 : Global.Config.AutofireOff;
			type = definition;
		}

		public ControllerDefinition Type { get { return type; } }
		public bool this[string button] { get { return IsPressed(button); } }
		public bool IsPressed(string button)
		{
			if (autofire)
			{
				int a = (Global.Emulator.Frame - buttonStarts[button]) % (On + Off);
				if (a < On)
					return buttons[button];
				else
					return false;
			}
			else
				return buttons[button];
		}


		public float GetFloat(string name) { throw new NotImplementedException(); }
		public void UpdateControls(int frame) { }

		//look for bindings which are activated by the supplied physical button.
		public List<string> SearchBindings(string button)
		{
			var ret = new List<string>();
			foreach (var kvp in bindings)
			{
				foreach (var bound_button in kvp.Value)
				{
					if (bound_button == button)
						ret.Add(kvp.Key);
				}
			}
			return ret;
		}

		/// <summary>
		/// uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding).
		/// this will clobber any existing data (use OR_* or other functions to layer in additional input sources)
		/// </summary>
		public void LatchFromPhysical(IController controller)
		{
			foreach (var kvp in bindings)
			{
				foreach (var bound_button in kvp.Value)
				{
					if (buttons[kvp.Key] == false && controller[bound_button])
						buttonStarts[kvp.Key] = Global.Emulator.Frame;
				}
			}
			
			buttons.Clear();
			foreach (var kvp in bindings)
			{
				buttons[kvp.Key] = false;
				foreach (var bound_button in kvp.Value)
				{
					if (controller[bound_button])
					{
						buttons[kvp.Key] = true;
					}
				}
			}
		}

		/// <summary>
		/// merges pressed logical buttons from the supplied controller, effectively ORing it with the current state
		/// </summary>
		public void OR_FromLogical(IController controller)
		{
			foreach (string button in type.BoolButtons)
			{
				if (controller.IsPressed(button))
				{
					buttons[button] = true;
					Console.WriteLine(button);
				}
			}
		}

		public void BindButton(string button, string control)
		{
			bindings[button].Add(control);
		}

		public void BindMulti(string button, string controlString)
		{
			if (string.IsNullOrEmpty(controlString))
				return;
			string[] controlbindings = controlString.Split(',');
			foreach (string control in controlbindings)
				bindings[button].Add(control.Trim());
		}

		public void IncrementStarts()
		{
			foreach (var key in buttonStarts.Keys.ToArray()) buttonStarts[key]++;
		}

		public List<string> PressedButtons
		{
			get
			{
				return (from button in buttons where button.Value select button.Key).ToList();
			}
		}
	}
}