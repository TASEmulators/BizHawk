#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	internal sealed class VirtualInputLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IVirtualInputLib, IJoypad
	{
		public VirtualInputLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		public IReadOnlyDictionary<string, object> Get(int? controller) => GetImpl(controller);

		[LegacyApiHawk]
		IDictionary<string, object> IJoypad.Get(int? controller) => GetImpl(controller);

		public IReadOnlyDictionary<string, object> GetImmediate(int? controller) => GetImmediateImpl(controller);

		[LegacyApiHawk]
		IDictionary<string, object> IJoypad.GetImmediate(int? controller) => GetImmediateImpl(controller);

		private Dictionary<string, object> GetImmediateImpl(int? controller = null)
			=> (Dictionary<string, object>) Env.GlobalInputManager.ActiveController.ToDictionary(controller);

		private Dictionary<string, object> GetImpl(int? controller = null)
			=> (Dictionary<string, object>) Env.GlobalInputManager.AutofireStickyXorAdapter.ToDictionary(controller);

		[LegacyApiHawk]
		public void Set(Dictionary<string, bool> buttons, int? controller = null) => ((IVirtualInputLib) this).Set(buttons, controller);

		public void Set(IReadOnlyDictionary<string, bool> buttons, int? controller = null)
		{
			foreach (var button in Env.GlobalInputManager.ActiveController.ToBoolButtonNameList(controller))
			{
				Set(button, buttons.TryGetValue(button, out var state) ? state : (bool?) null, controller);
			}
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var buttonToSet = controller == null ? button : $"P{controller} {button}";
				if (state == null) Env.GlobalInputManager.ButtonOverrideAdapter.UnSet(buttonToSet);
				else Env.GlobalInputManager.ButtonOverrideAdapter.SetButton(buttonToSet, state.Value);
				Env.GlobalInputManager.ActiveController.Overrides(Env.GlobalInputManager.ButtonOverrideAdapter);
			}
			catch
			{
				// ignored
			}
		}

		[LegacyApiHawk]
		public void SetAnalog(Dictionary<string, float> controls, object? controller)
		{
			foreach (var kvp in controls) SetAnalog(kvp.Key, kvp.Value, controller);
		}

		public void SetAnalog(IReadOnlyDictionary<string, int> controls, int? controller)
		{
			foreach (var kvp in controls) SetAnalog(kvp.Key, kvp.Value, controller);
		}

		[LegacyApiHawk]
		public void SetAnalog(string control, float? value, object? controller) => SetAnalog(
			control,
			value == null ? (int?) null : (int) value.Value,
			(int?) controller
		);

		public void SetAnalog(string control, int? value, int? controller)
		{
			try
			{
				Env.GlobalInputManager.StickyXorAdapter.SetAxis(controller == null ? control : $"P{controller} {control}", value);
			}
			catch
			{
				// ignored
			}
		}

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var controller = Env.GlobalMovieSession.GenerateMovieController();
			try
			{
				controller.SetFromMnemonic(inputLogEntry);
			}
			catch (Exception)
			{
				Env.LogCallback($"invalid mnemonic string: {inputLogEntry}");
				return;
			}
			foreach (var button in controller.Definition.BoolButtons)
			{
				Env.GlobalInputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
			}
			foreach (var axis in controller.Definition.AxisControls)
			{
				Env.GlobalInputManager.ButtonOverrideAdapter.SetAxis(axis, controller.AxisValue(axis));
			}
		}
	}
}
