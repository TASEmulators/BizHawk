using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.MultiClient
{
	public class Controller : IController
	{
		private ControllerDefinition type;
		private WorkingDictionary<string, List<string>> bindings = new WorkingDictionary<string, List<string>>();
		private WorkingDictionary<string, bool> buttons = new WorkingDictionary<string, bool>();

		private bool autofire = false;
		public bool Autofire { get { return false; } set { autofire = value; } }
		
		public Controller(ControllerDefinition definition)
		{
			type = definition;
		}

		public ControllerDefinition Type { get { return type; } }
		public bool this[string button] { get { return IsPressed(button); } }
		public bool IsPressed(string button)
		{
			if (autofire)
			{
				int a = Global.Emulator.Frame % 2;
				if (a == 1)
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
			buttons.Clear();
			foreach (var kvp in bindings)
			{
				buttons[kvp.Key] = false;
				foreach (var bound_button in kvp.Value)
				{
					if(controller[bound_button])
						buttons[kvp.Key] = true;
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

	}
}