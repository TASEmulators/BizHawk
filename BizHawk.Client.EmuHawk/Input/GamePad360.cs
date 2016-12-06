using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SlimDX.XInput;

#pragma warning disable 169
#pragma warning disable 414

namespace BizHawk.Client.EmuHawk
{
	public class GamePad360
	{
		// ********************************** Static interface **********************************

		public static List<GamePad360> Devices = new List<GamePad360>();

		static bool IsAvailable;

		[DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
		static extern IntPtr GetProcAddressOrdinal(IntPtr hModule, IntPtr procName);

		delegate uint XInputGetStateExProcDelegate(uint dwUserIndex, out XINPUT_STATE state);

		static bool HasGetInputStateEx;
		static IntPtr LibraryHandle;
		static XInputGetStateExProcDelegate XInputGetStateExProc;

		struct XINPUT_GAMEPAD
		{
			public ushort wButtons;
			public byte bLeftTrigger;
			public byte bRightTrigger;
			public short sThumbLX;
			public short sThumbLY;
			public short sThumbRX;
			public short sThumbRY;
		}

		struct XINPUT_STATE
		{
			public uint dwPacketNumber;
			public XINPUT_GAMEPAD Gamepad;
		}

		public static void Initialize()
		{
			IsAvailable = false;
			try
			{
				//some users wont even have xinput installed. in order to avoid spurious exceptions and possible instability, check for the library first
				HasGetInputStateEx = true;
				LibraryHandle = Win32.LoadLibrary("xinput1_3.dll");
				if(LibraryHandle == IntPtr.Zero)
					LibraryHandle = Win32.LoadLibrary("xinput1_4.dll");
				if(LibraryHandle == IntPtr.Zero)
				{
					LibraryHandle = Win32.LoadLibrary("xinput9_1_0.dll");
					HasGetInputStateEx = false;
				}

				if (LibraryHandle != IntPtr.Zero)
				{
					if (HasGetInputStateEx)
					{
						IntPtr proc = GetProcAddressOrdinal(LibraryHandle, new IntPtr(100));
						XInputGetStateExProc = (XInputGetStateExProcDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(XInputGetStateExProcDelegate));
					}

					//don't remove this code. it's important to catch errors on systems with broken xinput installs.
					//(probably, checking for the library was adequate, but lets not get rid of this anyway)
					var test = new SlimDX.XInput.Controller(UserIndex.One).IsConnected;
					IsAvailable = true;
				}

			}
			catch { }

			if (!IsAvailable) return;

			//now, at this point, slimdx may be using one xinput, and we may be using another
			//i'm not sure how slimdx picks its dll to bind to.
			//i'm not sure how troublesome this will be
			//maybe we should get rid of slimdx for this altogether

			var c1 = new Controller(UserIndex.One);
			var c2 = new Controller(UserIndex.Two);
			var c3 = new Controller(UserIndex.Three);
			var c4 = new Controller(UserIndex.Four);

			if (c1.IsConnected) Devices.Add(new GamePad360(0,c1));
			if (c2.IsConnected) Devices.Add(new GamePad360(1,c2));
			if (c3.IsConnected) Devices.Add(new GamePad360(2,c3));
			if (c4.IsConnected) Devices.Add(new GamePad360(3,c4));
		}

		public static void UpdateAll()
		{
			if(IsAvailable)
				foreach (var device in Devices.ToList())
					device.Update();
		}

		// ********************************** Instance Members **********************************

		readonly Controller controller;
        uint index0;
		XINPUT_STATE state;

        public int PlayerNumber { get { return (int)index0 + 1; } }

		GamePad360(uint index0, Controller c)
		{
            this.index0 = index0;
			controller = c;
			InitializeButtons();
			Update();
		}

		public void Update()
		{
			if (controller.IsConnected == false)
				return;

			if (XInputGetStateExProc != null)
			{
				state = new XINPUT_STATE();
				XInputGetStateExProc(index0, out state);
			}
			else
			{
				var slimstate = controller.GetState();
				state.dwPacketNumber = slimstate.PacketNumber;
				state.Gamepad.wButtons = (ushort)slimstate.Gamepad.Buttons;
				state.Gamepad.sThumbLX = slimstate.Gamepad.LeftThumbX;
				state.Gamepad.sThumbLY = slimstate.Gamepad.LeftThumbY;
				state.Gamepad.sThumbRX = slimstate.Gamepad.RightThumbX;
				state.Gamepad.sThumbRY = slimstate.Gamepad.RightThumbY;
				state.Gamepad.bLeftTrigger = slimstate.Gamepad.LeftTrigger;
				state.Gamepad.bRightTrigger = slimstate.Gamepad.RightTrigger;
			}
		}

		public IEnumerable<Tuple<string, float>> GetFloats()
		{
			var g = state.Gamepad;
			const float f = 3.2768f;
			yield return new Tuple<string, float>("LeftThumbX", g.sThumbLX / f);
			yield return new Tuple<string, float>("LeftThumbY", g.sThumbLY / f);
			yield return new Tuple<string, float>("RightThumbX", g.sThumbRX / f);
			yield return new Tuple<string, float>("RightThumbY", g.sThumbRY / f);
			yield break;
		}

		public int NumButtons { get; private set; }

		private readonly List<string> names = new List<string>();
		private readonly List<Func<bool>> actions = new List<Func<bool>>();

		void InitializeButtons()
		{
			const int dzp = 9000;
			const int dzn = -9000;
			const int dzt = 40;

			AddItem("A", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.A) != 0);
			AddItem("B", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.B) != 0);
			AddItem("X", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.X) != 0);
			AddItem("Y", () => (state.Gamepad.wButtons & unchecked((ushort)GamepadButtonFlags.Y)) != 0);
			AddItem("Guide", () => (state.Gamepad.wButtons & 1024) != 0);

			AddItem("Start", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.Start) != 0);
			AddItem("Back", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.Back) != 0);
			AddItem("LeftThumb", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.LeftThumb) != 0);
			AddItem("RightThumb", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.RightThumb) != 0);
			AddItem("LeftShoulder", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.LeftShoulder) != 0);
			AddItem("RightShoulder", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.RightShoulder) != 0);

			AddItem("DpadUp", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadUp) != 0);
			AddItem("DpadDown", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadDown) != 0);
			AddItem("DpadLeft", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadLeft) != 0);
			AddItem("DpadRight", () => (state.Gamepad.wButtons & (ushort)GamepadButtonFlags.DPadRight) != 0);

			AddItem("LStickUp", () => state.Gamepad.sThumbLY >= dzp);
			AddItem("LStickDown", () => state.Gamepad.sThumbLY <= dzn);
			AddItem("LStickLeft", () => state.Gamepad.sThumbLX <= dzn);
			AddItem("LStickRight", () => state.Gamepad.sThumbLX >= dzp);

			AddItem("RStickUp", () => state.Gamepad.sThumbRY >= dzp);
			AddItem("RStickDown", () => state.Gamepad.sThumbRY <= dzn);
			AddItem("RStickLeft", () => state.Gamepad.sThumbRX <= dzn);
			AddItem("RStickRight", () => state.Gamepad.sThumbRX >= dzp);

			AddItem("LeftTrigger", () => state.Gamepad.bLeftTrigger > dzt);
			AddItem("RightTrigger", () => state.Gamepad.bRightTrigger > dzt);
		}

		void AddItem(string name, Func<bool> pressed)
		{
			names.Add(name);
			actions.Add(pressed);
			NumButtons++;
		}

		public string ButtonName(int index)
		{
			return names[index];
		}

		public bool Pressed(int index)
		{
			return actions[index]();
		}
	}
}
