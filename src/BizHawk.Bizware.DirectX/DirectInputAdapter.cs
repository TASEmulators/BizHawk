#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Bizware.DirectX
{
	public sealed class DirectInputAdapter : IHostInputAdapter
	{
		private static readonly IReadOnlyCollection<string> XINPUT_HAPTIC_CHANNEL_NAMES = new[] { "Left", "Right" }; // doesn't seem to be a way to detect this via XInput, so assuming x360/xbone will be good enough

		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		private Config? _config;

		public void DeInitAll()
		{
			KeyInput.Cleanup();
			GamePad.Cleanup();
		}

		public void FirstInitAll(IntPtr mainFormHandle)
		{
			KeyInput.Initialize(mainFormHandle);
			IPCKeyInput.Initialize();
			ReInitGamepads(mainFormHandle);
		}

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
			=> GamePad360.EnumerateDevices().ToDictionary(pad => pad.InputNamePrefix, _ => XINPUT_HAPTIC_CHANNEL_NAMES);

		public void ReInitGamepads(IntPtr mainFormHandle)
		{
			GamePad.Initialize(mainFormHandle);
			GamePad360.Initialize();
		}

		public void PreprocessHostGamepads()
		{
			GamePad.UpdateAll();
			GamePad360.UpdateAll();
		}

		public void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			foreach (var pad in GamePad360.EnumerateDevices())
			{
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(pad.InputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(pad.InputNamePrefix + axisName, (int) f);
				_lastHapticsSnapshot.TryGetValue(pad.InputNamePrefix + "Left", out var leftStrength);
				_lastHapticsSnapshot.TryGetValue(pad.InputNamePrefix + "Right", out var rightStrength);
				pad.SetVibration(leftStrength, rightStrength); // values will be 0 if not found
			}
			foreach (var pad in GamePad.EnumerateDevices())
			{
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(pad.InputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(pad.InputNamePrefix + axisName, (int) f);
			}
		}

		public IEnumerable<KeyEvent> ProcessHostKeyboards() => KeyInput.Update(_config ?? throw new Exception(nameof(ProcessHostKeyboards) + " called before the global config was passed"))
			.Concat(IPCKeyInput.Update());

		public void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot)
			=> _lastHapticsSnapshot = hapticsSnapshot.ToDictionary(tuple => tuple.Name, tuple => tuple.Strength);

		public void UpdateConfig(Config config) => _config = config;
	}
}
