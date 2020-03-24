using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class JoypadApi : IJoypad
	{
		public JoypadApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public JoypadApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		public IDictionary<string, dynamic> Get(int? controller = null)
		{
			return Global.InputManager.AutofireStickyXorAdapter.ToDictionary(controller);
		}

		public IDictionary<string, dynamic> GetImmediate(int? controller = null)
		{
			return Global.InputManager.ActiveController.ToDictionary(controller);
		}

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var lg = Global.MovieSession.MovieControllerInstance();
			try
			{
				lg.SetControllersAsMnemonic(inputLogEntry);
			}
			catch (Exception)
			{
				LogCallback($"invalid mnemonic string: {inputLogEntry}");
				return;
			}
			foreach (var button in lg.Definition.BoolButtons) Global.InputManager.ButtonOverrideAdapter.SetButton(button, lg.IsPressed(button));
			foreach (var floatButton in lg.Definition.FloatControls) Global.InputManager.ButtonOverrideAdapter.SetFloat(floatButton, lg.GetFloat(floatButton));
		}

		public void Set(Dictionary<string, bool> buttons, int? controller = null)
		{
			foreach (var button in Global.InputManager.ActiveController.ToBoolButtonNameList(controller))
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : (bool?) null, controller);
			}
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var buttonToSet = controller == null ? button : $"P{controller} {button}";
				if (state == null) Global.InputManager.ButtonOverrideAdapter.UnSet(buttonToSet);
				else Global.InputManager.ButtonOverrideAdapter.SetButton(buttonToSet, state.Value);
				Global.InputManager.ActiveController.Overrides(Global.InputManager.ButtonOverrideAdapter);
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
				Global.InputManager.StickyXorAdapter.SetFloat(controller == null ? control : $"P{controller} {control}", value);
			}
			catch
			{
				// ignored
			}
		}
	}
}
