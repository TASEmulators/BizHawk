using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using SlimDX.XInput;

namespace BizHawk.Client.EmuHawk
{
	public class GamePad360
	{
		// ********************************** Static interface **********************************

		private static readonly object SyncObj = new object();
		private static readonly List<GamePad360> Devices = new List<GamePad360>();
		private static readonly bool IsAvailable;

		delegate uint XInputGetStateExProcDelegate(uint dwUserIndex, out XINPUT_STATE state);

		private static readonly XInputGetStateExProcDelegate XInputGetStateExProc;

		private struct XINPUT_GAMEPAD
		{
			public ushort wButtons;
			public byte bLeftTrigger;
			public byte bRightTrigger;
			public short sThumbLX;
			public short sThumbLY;
			public short sThumbRX;
			public short sThumbRY;
		}

		private struct XINPUT_STATE
		{
			public uint dwPacketNumber;
			public XINPUT_GAMEPAD Gamepad;
		}

		static GamePad360()
		{
			try
			{
				// some users won't even have xinput installed. in order to avoid spurious exceptions and possible instability, check for the library first
				var llManager = OSTailoredCode.LinkedLibManager;
				var libraryHandle = llManager.LoadOrNull("xinput1_3.dll") ?? llManager.LoadOrNull("xinput1_4.dll");
				if (libraryHandle != null)
				{
					XInputGetStateExProc = (XInputGetStateExProcDelegate) Marshal.GetDelegateForFunctionPointer(
						Win32Imports.GetProcAddressOrdinal(libraryHandle.Value, new IntPtr(100)),
						typeof(XInputGetStateExProcDelegate)
					);
				}
				else
				{
					libraryHandle = llManager.LoadOrNull("xinput9_1_0.dll");
				}
				IsAvailable = libraryHandle != null;

				// don't remove this code. it's important to catch errors on systems with broken xinput installs.
				// (probably, checking for the library was adequate, but let's not get rid of this anyway)
				if (IsAvailable) _ = new Controller(UserIndex.One).IsConnected;
			}
			catch
			{
				// ignored
			}
		}

		public static void Initialize()
		{
			lock (SyncObj)
			{
				Devices.Clear();

				if (!IsAvailable)
					return;

				//now, at this point, SlimDX may be using one xinput, and we may be using another
				//i'm not sure how SlimDX picks its dll to bind to.
				//i'm not sure how troublesome this will be
				//maybe we should get rid of SlimDX for this altogether

				var c1 = new Controller(UserIndex.One);
				var c2 = new Controller(UserIndex.Two);
				var c3 = new Controller(UserIndex.Three);
				var c4 = new Controller(UserIndex.Four);

				if (c1.IsConnected) Devices.Add(new GamePad360(0, c1));
				if (c2.IsConnected) Devices.Add(new GamePad360(1, c2));
				if (c3.IsConnected) Devices.Add(new GamePad360(2, c3));
				if (c4.IsConnected) Devices.Add(new GamePad360(3, c4));
			}
		}

		public static IEnumerable<GamePad360> EnumerateDevices()
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

		private readonly Controller _controller;
		private readonly uint _index0;
		private XINPUT_STATE _state;

		public int PlayerNumber => (int)_index0 + 1;

		GamePad360(uint index0, Controller c)
		{
			this._index0 = index0;
			_controller = c;
			InitializeButtons();
			Update();
		}

		public void Update()
		{
			if (_controller.IsConnected == false)
				return;

			if (XInputGetStateExProc != null)
			{
				_state = new XINPUT_STATE();
				XInputGetStateExProc(_index0, out _state);
			}
			else
			{
				var slimState = _controller.GetState();
				_state.dwPacketNumber = slimState.PacketNumber;
				_state.Gamepad.wButtons = (ushort)slimState.Gamepad.Buttons;
				_state.Gamepad.sThumbLX = slimState.Gamepad.LeftThumbX;
				_state.Gamepad.sThumbLY = slimState.Gamepad.LeftThumbY;
				_state.Gamepad.sThumbRX = slimState.Gamepad.RightThumbX;
				_state.Gamepad.sThumbRY = slimState.Gamepad.RightThumbY;
				_state.Gamepad.bLeftTrigger = slimState.Gamepad.LeftTrigger;
				_state.Gamepad.bRightTrigger = slimState.Gamepad.RightTrigger;
			}
		}

		public IEnumerable<(string AxisID, float Value)> GetAxes()
		{
			var g = _state.Gamepad;

			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;

			//since our whole input framework really only understands whole axes, let's make the triggers look like an axis
			float lTrig = g.bLeftTrigger / 255.0f * 2 - 1;
			float rTrig = g.bRightTrigger / 255.0f * 2 - 1;
			lTrig *= 10000;
			rTrig *= 10000;

			yield return ("LeftThumbX", g.sThumbLX / f);
			yield return ("LeftThumbY", g.sThumbLY / f);
			yield return ("RightThumbX", g.sThumbRX / f);
			yield return ("RightThumbY", g.sThumbRY / f);
			yield return ("LeftTrigger", lTrig);
			yield return ("RightTrigger", rTrig);
		}

		public int NumButtons { get; private set; }

		private readonly List<string> _names = new List<string>();
		private readonly List<Func<bool>> _actions = new List<Func<bool>>();

		private void InitializeButtons()
		{
			const int dzp = 20000;
			const int dzn = -20000;
			const int dzt = 40;

			AddItem("A", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.A) != 0);
			AddItem("B", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.B) != 0);
			AddItem("X", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.X) != 0);
			AddItem("Y", () => (_state.Gamepad.wButtons & unchecked((ushort)GamepadButtonFlags.Y)) != 0);
			AddItem("Guide", () => (_state.Gamepad.wButtons & 1024) != 0);

			AddItem("Start", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.Start) != 0);
			AddItem("Back", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.Back) != 0);
			AddItem("LeftThumb", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.LeftThumb) != 0);
			AddItem("RightThumb", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.RightThumb) != 0);
			AddItem("LeftShoulder", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.LeftShoulder) != 0);
			AddItem("RightShoulder", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.RightShoulder) != 0);

			AddItem("DpadUp", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadUp) != 0);
			AddItem("DpadDown", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadDown) != 0);
			AddItem("DpadLeft", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadLeft) != 0);
			AddItem("DpadRight", () => (_state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadRight) != 0);

			AddItem("LStickUp", () => _state.Gamepad.sThumbLY >= dzp);
			AddItem("LStickDown", () => _state.Gamepad.sThumbLY <= dzn);
			AddItem("LStickLeft", () => _state.Gamepad.sThumbLX <= dzn);
			AddItem("LStickRight", () => _state.Gamepad.sThumbLX >= dzp);

			AddItem("RStickUp", () => _state.Gamepad.sThumbRY >= dzp);
			AddItem("RStickDown", () => _state.Gamepad.sThumbRY <= dzn);
			AddItem("RStickLeft", () => _state.Gamepad.sThumbRX <= dzn);
			AddItem("RStickRight", () => _state.Gamepad.sThumbRX >= dzp);

			AddItem("LeftTrigger", () => _state.Gamepad.bLeftTrigger > dzt);
			AddItem("RightTrigger", () => _state.Gamepad.bRightTrigger > dzt);
		}

		private void AddItem(string name, Func<bool> pressed)
		{
			_names.Add(name);
			_actions.Add(pressed);
			NumButtons++;
		}

		public string ButtonName(int index) => _names[index];

		public bool Pressed(int index) => _actions[index]();
	}
}
