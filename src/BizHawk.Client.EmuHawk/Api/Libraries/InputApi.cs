using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputApi : IInputApi
	{
		private readonly IWindowCoordsTransformer _displayManager;

		private readonly InputManager _inputManager;

		private readonly IMainFormForApi _mainForm;

		public InputApi(IMainFormForApi mainForm, IWindowCoordsTransformer displayManager, InputManager inputManager)
		{
			_displayManager = displayManager;
			_inputManager = inputManager;
			_mainForm = mainForm;
		}

		public Dictionary<string, bool> Get()
		{
			var buttons = new Dictionary<string, bool>();
			foreach (var kvp in _inputManager.ControllerInputCoalescer.BoolButtons().Where(kvp => kvp.Value)) buttons[kvp.Key] = true;
			return buttons;
		}

		public Dictionary<string, object> GetMouse()
		{
			var buttons = new Dictionary<string, object>();
			// TODO - need to specify whether in "emu" or "native" coordinate space.
			var p = _displayManager.UntransformPoint(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			buttons["Wheel"] = _mainForm.MouseWheelTracker;
			return buttons;
		}
	}
}
