#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class OpenTKInputAdapter : IHostInputAdapter
	{
		public void DeInitAll() {}

		public void FirstInitAll(IntPtr mainFormHandle)
		{
			OTK_Keyboard.Initialize();
			OTK_GamePad.Initialize();
		}

		public void ReInitGamepads(IntPtr mainFormHandle) {}

		public void PreprocessHostGamepads() => OTK_GamePad.UpdateAll();

		public void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			foreach (var pad in OTK_GamePad.EnumerateDevices())
			{
				foreach (var but in pad.buttonObjects) handleButton(pad.InputNamePrefix + but.ButtonName, but.ButtonAction(), ClientInputFocus.Pad);
				foreach (var (axisID, f) in pad.GetAxes()) handleAxis($"{pad.InputNamePrefix}{axisID} Axis", (int) f);
			}
		}

		public IEnumerable<KeyEvent> ProcessHostKeyboards() => OTK_Keyboard.Update();

		public void UpdateConfig(Config config) {}
	}
}
