#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Bizware.DirectX
{
	public sealed class DirectInputAdapter : HostInputAdapter
	{
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
				var inputNamePrefix = $"X{pad.PlayerNumber} ";
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(inputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(inputNamePrefix + axisName, (int) f);
			}
			foreach (var pad in GamePad.EnumerateDevices())
			{
				var inputNamePrefix = $"J{pad.PlayerNumber} ";
				for (int b = 0, n = pad.NumButtons; b < n; b++) handleButton(inputNamePrefix + pad.ButtonName(b), pad.Pressed(b), ClientInputFocus.Pad);
				foreach (var (axisName, f) in pad.GetAxes()) handleAxis(inputNamePrefix + axisName, (int) f);
			}
		}

		public IEnumerable<KeyEvent> ProcessHostKeyboards() => KeyInput.Update(_config ?? throw new NullReferenceException("o noes"))
			.Concat(IPCKeyInput.Update());

		public void UpdateConfig(Config config) => _config = config;
	}
}
