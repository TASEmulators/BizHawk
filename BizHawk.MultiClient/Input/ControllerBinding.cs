using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.MultiClient
{
	public class Controller : IController
	{
		private ControllerDefinition type;
		private Dictionary<string, List<string>> bindings = new Dictionary<string, List<string>>();
		private WorkingDictionary<string, bool> stickyButtons = new WorkingDictionary<string, bool>();
		private List<string> unpressedButtons = new List<string>();
		private List<string> forcePressedButtons = new List<string>();
		private List<string> removeFromForcePressedButtons = new List<string>();
		private List<string> programmaticallyPressedButtons = new List<string>();

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

		//uses the bindings to latch our own logical button state from the source controller's button state (which are assumed to be the physical side of the binding)
		public void LatchFromPhysical(IController controller)
		{
			foreach (var kvp in bindings)
			{
				stickyButtons[kvp.Key] = false;
				foreach (var bound_button in kvp.Value)
				{
					if(controller[bound_button])
						stickyButtons[kvp.Key] = true;
				}
			}
		}

		public Controller(ControllerDefinition definition)
		{
			type = definition;

			foreach (var b in type.BoolButtons)
			{
				bindings[b] = new List<string>();
			}

			foreach (var f in type.FloatControls)
				bindings[f] = new List<string>();
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

		public ControllerDefinition Type
		{
			get { return type; }
		}

		public bool this[string button]
		{
			get { return IsPressed(button); }
		}

		public bool IsPressed(string button)
		{
			//if (forcePressedButtons.Contains(button))
			//{
			//    removeFromForcePressedButtons.Add(button);
			//    return true;
			//}
			//if (unpressedButtons.Contains(button))
			//{
			//    if (IsPressedActually(button) == false)
			//        unpressedButtons.Remove(button);

			//    return false;
			//}

			////zeromus - TODO - this is gross!!! 
			//if (Global.Config.AllowUD_LR == false)
			//{
			//    string prefix;

			//    if (button.Contains("Down"))
			//    {
			//        prefix = button.GetPrecedingString("Down");
			//        if (IsPressed(prefix + "Up"))
			//            return false;
			//    }
			//    if (button.Contains("Right"))
			//    {
			//        prefix = button.GetPrecedingString("Right");
			//        if (IsPressed(prefix + "Left"))
			//            return false;
			//    }
			//}

			//return IsPressedActually(button);
			return stickyButtons[button];
		}

		//public bool IsPressedActually(string button)
		//{
		//    bool sticky = stickyButtons[button];

		//    foreach (var control in bindings[button])
		//        if (Input.Instance.IsPressed(control))
		//            return sticky ? false : true;

		//    return sticky ? true : false;
		//}

		public float GetFloat(string name)
		{
			throw new NotImplementedException();
		}

		public void UnpressButton(string name)
		{
			unpressedButtons.Add(name);
		}

		private int frameNumber;

		public void UpdateControls(int frame)
		{
			//if (frame != frameNumber)
			//{
			//    // update
			//    unpressedButtons.RemoveAll(button => IsPressedActually(button) == false);
			//    forcePressedButtons.RemoveAll(button => removeFromForcePressedButtons.Contains(button));
			//    removeFromForcePressedButtons.Clear();
			//}
			//frameNumber = frame;
		}

		public void SetSticky(string button, bool sticky)
		{
			stickyButtons[button] = sticky;
		}

		public bool IsSticky(string button)
		{
			return stickyButtons[button];
		}

		public void ForceButton(string button)
		{
			forcePressedButtons.Add(button);
		}

	
	}
}