using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class JoypadApi : IJoypad
	{
		private readonly Action<string> _logCallback;

		public JoypadApi(Action<string> logCallback)
		{
			_logCallback = logCallback;
		}

		public JoypadApi() : this(Console.WriteLine) {}

		public IDictionary<string, dynamic> Get(int? controller) => Global.AutofireStickyXORAdapter.ToDictionary(controller);

		public IDictionary<string, dynamic> GetImmediate(int? controller) => Global.ActiveController.ToDictionary(controller);

		public void Set(IDictionary<string, bool> buttons, int? controller)
		{
			foreach (var button in Global.ActiveController.Definition.BoolButtons)
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : (bool?) null, controller);
			}
		}

		public void Set(string button, bool? state, int? controller)
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

		public void SetAnalog(IDictionary<string, float> controls, object controller)
		{
			foreach (var kvp in controls) SetAnalog(kvp.Key, kvp.Value, controller);
		}

		public void SetAnalog(string control, float? value, object controller)
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

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var lg = Global.MovieSession.MovieControllerInstance();
			try
			{
				lg.SetControllersAsMnemonic(inputLogEntry);
			}
			catch (Exception)
			{
				_logCallback($"invalid mnemonic string: {inputLogEntry}");
				return;
			}
			foreach (var button in lg.Definition.BoolButtons) Global.ButtonOverrideAdaptor.SetButton(button, lg.IsPressed(button));
			foreach (var floatButton in lg.Definition.FloatControls) Global.ButtonOverrideAdaptor.SetFloat(floatButton, lg.GetFloat(floatButton));
		}
	}
}
