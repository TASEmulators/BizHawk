using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;

using Vortice.XInput;

namespace BizHawk.Bizware.Input
{
	internal sealed class XGamepad
	{
		// ********************************** Static interface **********************************

		private static readonly object SyncObj = new();
		private static readonly List<XGamepad> Devices = new();
		private static readonly bool IsAvailable;

		// Vortice has some support for the unofficial API, but it has some issues
		// (e.g. the check for AllowUnofficialAPI is in static ctor (???), uses it regardless of it being available)
		// We'll just get the proc ourselves and use it
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate uint XInputGetStateExProcDelegate(int dwUserIndex, out State state);

		private static readonly XInputGetStateExProcDelegate XInputGetStateExProc;

		static XGamepad()
		{
			try
			{
				// some users won't even have xinput installed. in order to avoid spurious exceptions and possible instability, check for the library first
				IsAvailable = XInput.Version != XInputVersion.Invalid;
				if (IsAvailable)
				{
					var llManager = OSTailoredCode.LinkedLibManager;
					var libHandle = XInput.Version switch
					{
						XInputVersion.Version14 => llManager.LoadOrThrow("xinput1_4.dll"),
						XInputVersion.Version13 => llManager.LoadOrThrow("xinput1_3.dll"),
						_ => IntPtr.Zero // unofficial API isn't available for 9.1.0
					};

					if (libHandle != IntPtr.Zero)
					{
						var fptr = llManager.GetProcAddrOrZero(libHandle, "#100");
						if (fptr != IntPtr.Zero)
						{
							XInputGetStateExProc =
								Marshal.GetDelegateForFunctionPointer<XInputGetStateExProcDelegate>(fptr);
						}

						// nb: this doesn't actually free the library here, rather it will just decrement the reference count
						llManager.FreeByPtr(libHandle);
					}

					// don't remove this code. it's important to catch errors on systems with broken xinput installs.

					_ = XInputGetStateExProc?.Invoke(0, out _);
					_ = XInput.GetState(0, out _);
				}
			}
			catch
			{
				IsAvailable = false;
			}
		}

		public static void Initialize()
		{
			lock (SyncObj)
			{
				Devices.Clear();

				if (!IsAvailable)
					return;

				if (XInput.GetState(0, out _)) Devices.Add(new(0));
				if (XInput.GetState(1, out _)) Devices.Add(new(1));
				if (XInput.GetState(2, out _)) Devices.Add(new(2));
				if (XInput.GetState(3, out _)) Devices.Add(new(3));
			}
		}

		public static IEnumerable<XGamepad> EnumerateDevices()
		{
			lock (SyncObj)
			{
				foreach (var device in Devices)
				{
					yield return device;
				}
			}
		}

		public static void UpdateAll()
		{
			lock (SyncObj)
			{
				foreach (var device in Devices)
				{
					device.Update();
				}
			}
		}

		// ********************************** Instance Members **********************************

		private readonly int _index0;
		private State _state;

		public int PlayerNumber => _index0 + 1;
		public bool IsConnected => XInput.GetState(_index0, out _);
		public readonly string InputNamePrefix;

		private XGamepad(int index0)
		{
			_index0 = index0;
			InputNamePrefix = $"X{PlayerNumber} ";
			InitializeButtons();
			Update();
		}

		public void Update()
		{
			if (!IsConnected)
				return;

			_state = default;
			if (XInputGetStateExProc is not null)
			{
				XInputGetStateExProc(_index0, out _state);
			}
			else
			{
				XInput.GetState(_index0, out _state);
			}
		}

		public IEnumerable<(string AxisID, float Value)> GetAxes()
		{
			var g = _state.Gamepad;

			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;

			//since our whole input framework really only understands whole axes, let's make the triggers look like an axis
			var lTrig = g.LeftTrigger / 255.0f * 2 - 1;
			var rTrig = g.RightTrigger / 255.0f * 2 - 1;
			lTrig *= 10000;
			rTrig *= 10000;

			yield return ("LeftThumbX", g.LeftThumbX / f);
			yield return ("LeftThumbY", g.LeftThumbY / f);
			yield return ("RightThumbX", g.RightThumbX / f);
			yield return ("RightThumbY", g.RightThumbY / f);
			yield return ("LeftTrigger", lTrig);
			yield return ("RightTrigger", rTrig);
		}

		public int NumButtons { get; private set; }

		private readonly List<string> _names = new();
		private readonly List<Func<bool>> _actions = new();

		private void InitializeButtons()
		{
			const int dzp = 20000;
			const int dzn = -20000;
			const int dzt = 40;

			AddItem("A", () => (_state.Gamepad.Buttons & GamepadButtons.A) != 0);
			AddItem("B", () => (_state.Gamepad.Buttons & GamepadButtons.B) != 0);
			AddItem("X", () => (_state.Gamepad.Buttons & GamepadButtons.X) != 0);
			AddItem("Y", () => (_state.Gamepad.Buttons & GamepadButtons.Y) != 0);
			AddItem("Guide", () => (_state.Gamepad.Buttons & GamepadButtons.Guide) != 0);

			AddItem("Start", () => (_state.Gamepad.Buttons & GamepadButtons.Start) != 0);
			AddItem("Back", () => (_state.Gamepad.Buttons & GamepadButtons.Back) != 0);
			AddItem("LeftThumb", () => (_state.Gamepad.Buttons & GamepadButtons.LeftThumb) != 0);
			AddItem("RightThumb", () => (_state.Gamepad.Buttons & GamepadButtons.RightThumb) != 0);
			AddItem("LeftShoulder", () => (_state.Gamepad.Buttons & GamepadButtons.LeftShoulder) != 0);
			AddItem("RightShoulder", () => (_state.Gamepad.Buttons & GamepadButtons.RightShoulder) != 0);

			AddItem("DpadUp", () => (_state.Gamepad.Buttons & GamepadButtons.DPadUp) != 0);
			AddItem("DpadDown", () => (_state.Gamepad.Buttons & GamepadButtons.DPadDown) != 0);
			AddItem("DpadLeft", () => (_state.Gamepad.Buttons & GamepadButtons.DPadLeft) != 0);
			AddItem("DpadRight", () => (_state.Gamepad.Buttons & GamepadButtons.DPadRight) != 0);

			AddItem("LStickUp", () => _state.Gamepad.LeftThumbY >= dzp);
			AddItem("LStickDown", () => _state.Gamepad.LeftThumbY <= dzn);
			AddItem("LStickLeft", () => _state.Gamepad.LeftThumbX <= dzn);
			AddItem("LStickRight", () => _state.Gamepad.LeftThumbX >= dzp);

			AddItem("RStickUp", () => _state.Gamepad.RightThumbY >= dzp);
			AddItem("RStickDown", () => _state.Gamepad.RightThumbY <= dzn);
			AddItem("RStickLeft", () => _state.Gamepad.RightThumbX <= dzn);
			AddItem("RStickRight", () => _state.Gamepad.RightThumbX >= dzp);

			AddItem("LeftTrigger", () => _state.Gamepad.LeftTrigger > dzt);
			AddItem("RightTrigger", () => _state.Gamepad.RightTrigger > dzt);
		}

		private void AddItem(string name, Func<bool> pressed)
		{
			_names.Add(name);
			_actions.Add(pressed);
			NumButtons++;
		}

		public string ButtonName(int index) => _names[index];

		public bool Pressed(int index) => _actions[index]();

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			static ushort Conv(int i) => unchecked((ushort) ((i >> 15) & 0xFFFF));

			if (!XInput.SetVibration(_index0, new(Conv(left), Conv(right))))
			{
				// Ignored, most likely the controller disconnected
			}
		}
	}
}
