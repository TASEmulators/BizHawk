#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Bizware.OpenTK3
{
	public sealed class OpenTKInputAdapter : IHostInputAdapter
	{
		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		public void DeInitAll() {}

		public void FirstInitAll(IntPtr mainFormHandle)
		{
			OTK_Keyboard.Initialize();
			OTK_GamePad.Initialize();
		}

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
			=> OTK_GamePad.EnumerateDevices().ToDictionary(pad => pad.InputNamePrefix, pad => pad.HapticsChannels);

		public void ReInitGamepads(IntPtr mainFormHandle) {}

		public void PreprocessHostGamepads() => OTK_GamePad.UpdateAll();

		public void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			foreach (var pad in OTK_GamePad.EnumerateDevices())
			{
				foreach (var but in pad.buttonObjects) handleButton(pad.InputNamePrefix + but.ButtonName, but.ButtonAction(), ClientInputFocus.Pad);
				foreach (var (axisID, f) in pad.GetAxes()) handleAxis($"{pad.InputNamePrefix}{axisID} Axis", (int) f);
#if DEBUG // effectively no-op as OpenTK 3 doesn't seem to actually support haptic feedback
				foreach (var channel in pad.HapticsChannels)
				{
					if (!_lastHapticsSnapshot.TryGetValue(pad.InputNamePrefix + channel, out var strength))
					{
						pad.SetVibration(0, 0);
						continue;
					}
					switch (channel)
					{
						case "Mono":
							pad.SetVibration(strength, strength);
							break;
						case "Left": // presence of left channel implies presence of right channel, so we'll use it here...
							pad.SetVibration(strength, _lastHapticsSnapshot[pad.InputNamePrefix + "Right"]);
							break;
						case "Right": // ...and ignore it here
							break;
						default:
							Console.WriteLine(nameof(OTK_GamePad) + " has a new kind of haptic channel? (Dev forgot to update this file too?)");
							break;
					}
				}
#endif
			}
		}

		public IEnumerable<KeyEvent> ProcessHostKeyboards() => OTK_Keyboard.Update();

		public void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot)
#if DEBUG // effectively no-op as OpenTK 3 doesn't seem to actually support haptic feedback
			=> _lastHapticsSnapshot = hapticsSnapshot.ToDictionary(tuple => tuple.Name, tuple => tuple.Strength);
#else
		{}
#endif

		public void UpdateConfig(Config config) {}
	}
}
