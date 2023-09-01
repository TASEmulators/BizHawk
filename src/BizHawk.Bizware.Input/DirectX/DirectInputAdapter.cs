#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Bizware.Input
{
	public sealed class DirectInputAdapter : IHostInputAdapter
	{
		private static readonly IReadOnlyCollection<string> XINPUT_HAPTIC_CHANNEL_NAMES = new[] { "Left", "Right" }; // doesn't seem to be a way to detect this via XInput, so assuming x360/xbone will be good enough

		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		private Config? _config;

		public string Desc => "DirectInput+XInput";

		public void DeInitAll()
		{
			DKeyInput.Cleanup();
			DGamepad.Cleanup();
		}

		public void FirstInitAll(IntPtr mainFormHandle)
		{
			DKeyInput.Initialize(mainFormHandle);
			IPCKeyInput.Initialize();
			ReInitGamepads(mainFormHandle);
		}

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
			=> XGamepad.EnumerateDevices().ToDictionary(pad => pad.InputNamePrefix, _ => XINPUT_HAPTIC_CHANNEL_NAMES);

		public void ReInitGamepads(IntPtr mainFormHandle)
		{
			DGamepad.Initialize(mainFormHandle);
			XGamepad.Initialize();
		}

		public void PreprocessHostGamepads()
		{
			DGamepad.UpdateAll();
			XGamepad.UpdateAll();
		}

		public void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			foreach (var pad in XGamepad.EnumerateDevices())
			{
				if (!pad.IsConnected)
					continue;
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(pad.InputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(pad.InputNamePrefix + axisName, (int) f);
				int leftStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Left");
				int rightStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Right");
				pad.SetVibration(leftStrength, rightStrength); // values will be 0 if not found
			}
			foreach (var pad in DGamepad.EnumerateDevices())
			{
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(pad.InputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(pad.InputNamePrefix + axisName, (int) f);
			}
		}

		public IEnumerable<KeyEvent> ProcessHostKeyboards() => DKeyInput.Update(_config ?? throw new(nameof(ProcessHostKeyboards) + " called before the global config was passed"))
			.Concat(IPCKeyInput.Update());

		public void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot)
			=> _lastHapticsSnapshot = hapticsSnapshot.ToDictionary(tuple => tuple.Name, tuple => tuple.Strength);

		public void UpdateConfig(Config config) => _config = config;
	}
}
