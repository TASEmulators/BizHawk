using System;
using System.Collections.Generic;

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

		public Dictionary<string, dynamic> Get(int? controller = null)
		{
			var adapter = Global.AutofireStickyXORAdapter;
			var buttons = new Dictionary<string, dynamic>();
			foreach (var button in adapter.Source.Definition.BoolButtons)
			{
				if (controller == null) buttons[button] = adapter.IsPressed(button);
				else if (button.Length > 2 && button.Substring(0, 2) == $"P{controller}")
				{
					var sub = button.Substring(3);
					buttons[sub] = adapter.IsPressed($"P{controller} {sub}");
				}
			}
			foreach (var button in adapter.Source.Definition.FloatControls)
			{
				if (controller == null) buttons[button] = adapter.GetFloat(button);
				else if (button.Length > 2 && button.Substring(0, 2) == $"P{controller}")
				{
					var sub = button.Substring(3);
					buttons[sub] = adapter.GetFloat($"P{controller} {sub}");
				}
			}
			return buttons;
		}

		public Dictionary<string, dynamic> GetImmediate()
		{
			var adapter = Global.ActiveController;
			var buttons = new Dictionary<string, dynamic>();
			foreach (var button in adapter.Definition.BoolButtons) buttons[button] = adapter.IsPressed(button);
			foreach (var button in adapter.Definition.FloatControls) buttons[button] = adapter.GetFloat(button);
			return buttons;
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
			foreach (var button in lg.Definition.BoolButtons) Global.ButtonOverrideAdaptor.SetButton(button, lg.IsPressed(button));
			foreach (var floatButton in lg.Definition.FloatControls) Global.ButtonOverrideAdaptor.SetFloat(floatButton, lg.GetFloat(floatButton));
		}

		public void Set(Dictionary<string, bool> buttons, int? controller = null)
		{
			foreach (var button in Global.ActiveController.Definition.BoolButtons)
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : default, controller);
			}
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var buttonToSet = controller == null ? button : $"P{controller} {button}";
				if (state == null) Global.ButtonOverrideAdaptor.UnSet(buttonToSet);
				else Global.ButtonOverrideAdaptor.SetButton(buttonToSet, state.Value);
				Global.ActiveController.Overrides(Global.ButtonOverrideAdaptor);
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
				Global.StickyXORAdapter.SetFloat(controller == null ? control : $"P{controller} {control}", value);
			}
			catch
			{
				// ignored
			}
		}
	}
}
