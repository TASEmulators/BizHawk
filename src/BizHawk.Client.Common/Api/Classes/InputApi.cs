using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
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
			var (pos, lmb, mmb, rmb, x1mb, x2mb) = _inputManager.GetMainFormMouseInfo();
			// TODO - need to specify whether in "emu" or "native" coordinate space.
			var p = _displayManager.UntransformPoint(pos);
			return new Dictionary<string, object>
			{
				["X"] = p.X,
				["Y"] = p.Y,
				["Left"] = lmb,
				["Middle"] = mmb,
				["Right"] = rmb,
				["XButton1"] = x1mb,
				["XButton2"] = x2mb,
				["Wheel"] = _mainForm.MouseWheelTracker
			};
		}
	}
}
