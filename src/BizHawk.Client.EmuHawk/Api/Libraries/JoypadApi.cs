using System;
using System.Collections.Generic;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class JoypadApi : IJoypadApi
	{
		private readonly InputManager _inputManager;

		private readonly IMovieSession _movieSession;

		private readonly Action<string> LogCallback;

		public JoypadApi(Action<string> logCallback, InputManager inputManager, IMovieSession movieSession)
		{
			LogCallback = logCallback;
			_inputManager = inputManager;
			_movieSession = movieSession;
		}

		public IDictionary<string, object> Get(int? controller = null)
		{
			return _inputManager.AutofireStickyXorAdapter.ToDictionary(controller);
		}

		public IDictionary<string, object> GetImmediate(int? controller = null)
		{
			return _inputManager.ActiveController.ToDictionary(controller);
		}

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var controller = _movieSession.GenerateMovieController();
			try
			{
				controller.SetFromMnemonic(inputLogEntry);
			}
			catch (Exception)
			{
				LogCallback($"invalid mnemonic string: {inputLogEntry}");
				return;
			}
			foreach (var button in controller.Definition.BoolButtons) _inputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
			foreach (var axis in controller.Definition.Axes.Keys) _inputManager.ButtonOverrideAdapter.SetAxis(axis, controller.AxisValue(axis));
		}

		public void Set(IDictionary<string, bool> buttons, int? controller = null)
		{
			// If a controller is specified, we need to iterate over unique button names. If not, we iterate over
			// ALL button names with P{controller} prefixes
			foreach (var button in _inputManager.ActiveController.ToBoolButtonNameList(controller))
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : (bool?) null, controller);
			}
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var buttonToSet = controller == null ? button : $"P{controller} {button}";
				if (state == null) _inputManager.ButtonOverrideAdapter.UnSet(buttonToSet);
				else _inputManager.ButtonOverrideAdapter.SetButton(buttonToSet, state.Value);
				_inputManager.ActiveController.Overrides(_inputManager.ButtonOverrideAdapter);
			}
			catch
			{
				// ignored
			}
		}

		public void SetAnalog(IDictionary<string, int?> controls, object controller = null)
		{
			foreach (var kvp in controls) SetAnalog(kvp.Key, kvp.Value, controller);
		}

		public void SetAnalog(string control, int? value = null, object controller = null)
		{
			try
			{
				_inputManager.StickyXorAdapter.SetAxis(controller == null ? control : $"P{controller} {control}", value);
			}
			catch
			{
				// ignored
			}
		}
	}
}
