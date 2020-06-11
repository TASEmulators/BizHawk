using System;
using System.Collections.Generic;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class JoypadApi : IJoypadApi
	{
		public JoypadApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public JoypadApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		public IDictionary<string, object> Get(int? controller = null)
		{
			return GlobalWin.InputManager.AutofireStickyXorAdapter.ToDictionary(controller);
		}

		public IDictionary<string, object> GetImmediate(int? controller = null)
		{
			return GlobalWin.InputManager.ActiveController.ToDictionary(controller);
		}

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var controller = GlobalWin.MovieSession.GenerateMovieController();
			try
			{
				controller.SetFromMnemonic(inputLogEntry);
			}
			catch (Exception)
			{
				LogCallback($"invalid mnemonic string: {inputLogEntry}");
				return;
			}
			foreach (var button in controller.Definition.BoolButtons) GlobalWin.InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
			foreach (var axis in controller.Definition.AxisControls) GlobalWin.InputManager.ButtonOverrideAdapter.SetAxis(axis, controller.AxisValue(axis));
		}

		public void Set(Dictionary<string, bool> buttons, int? controller = null)
		{
			// If a controller is specified, we need to iterate over unique button names. If not, we iterate over
			// ALL button names with P{controller} prefixes
			foreach (var button in GlobalWin.InputManager.ActiveController.ToBoolButtonNameList(controller))
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : (bool?) null, controller);
			}
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var buttonToSet = controller == null ? button : $"P{controller} {button}";
				if (state == null) GlobalWin.InputManager.ButtonOverrideAdapter.UnSet(buttonToSet);
				else GlobalWin.InputManager.ButtonOverrideAdapter.SetButton(buttonToSet, state.Value);
				GlobalWin.InputManager.ActiveController.Overrides(GlobalWin.InputManager.ButtonOverrideAdapter);
			}
			catch
			{
				// ignored
			}
		}

		public void SetAnalog(Dictionary<string, float> controls, object controller = null)
		{
			foreach (var kvp in controls) SetAnalog(kvp.Key, kvp.Value, controller);
		}

		public void SetAnalog(string control, float? value = null, object controller = null)
		{
			try
			{
				GlobalWin.InputManager.StickyXorAdapter.SetAxis(controller == null ? control : $"P{controller} {control}", value == null ? (int?) null : (int) value.Value); // the time for changing the API will come --yoshi
			}
			catch
			{
				// ignored
			}
		}
	}
}
