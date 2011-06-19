using System;
using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.MultiClient
{
	public class GamePad
	{
		// ********************************** Static interface **********************************

		private static DirectInput dinput;
		public static List<GamePad> Devices;

		public static void Initialize()
		{
			if (dinput == null)
				dinput = new DirectInput();

			Devices = new List<GamePad>();

			foreach (DeviceInstance device in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
			{
				var joystick = new Joystick(dinput, device.InstanceGuid);
				joystick.SetCooperativeLevel(Global.MainForm.Handle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
				foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
				{
					if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
						joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-1000, 1000);
				}
				joystick.Acquire();

				GamePad p = new GamePad(device.InstanceName, device.InstanceGuid, joystick);
				Devices.Add(p);
			}
		}

		public static void UpdateAll()
		{
			foreach (var device in Devices)
				device.Update();
		}

		// ********************************** Instance Members **********************************

		private readonly string name;
		private readonly Guid guid;
		private readonly Joystick joystick;
		private JoystickState state = new JoystickState();
		private bool[] buttons;
		private int[] pov;

		private GamePad(string name, Guid guid, Joystick joystick)
		{
			this.name = name;
			this.guid = guid;
			this.joystick = joystick;
			Update();
		}

		public void Update()
		{
			if (joystick.Acquire().IsFailure)
				return;
			if (joystick.Poll().IsFailure)
				return;

			state = joystick.GetCurrentState();
			if (Result.Last.IsFailure)
				return;

			buttons = state.GetButtons();
			pov = state.GetPointOfViewControllers();
		}

		public string Name { get { return name; } }
		public Guid Guid { get { return guid; } }

		public float X { get { return state.X / 1000f; } }
		public float Y { get { return state.Y / 1000f; } }
		public float Z { get { return state.Z / 1000f; } }

		public bool[] Buttons { get { return buttons; } }

		public bool Up
		{
			get
			{
				if (state.Y < -250 || state.RotationY < -250)
					return true;
				foreach (int p in pov)
					if (p.In(0, 4500, 31500))
						return true;
				return false;
			}
		}

		public bool Down
		{
			get
			{
				if (state.Y > 250 || state.RotationY > 250)
					return true;
				foreach (int p in pov)
					if (p.In(13500, 18000, 22500))
						return true;
				return false;
			}
		}

		public bool Left
		{
			get
			{
				if (state.X < -250 || state.RotationX < -250)
					return true;
				foreach (int p in pov)
					if (p.In(22500, 27000, 31500))
						return true;
				return false;
			}
		}

		public bool Right
		{
			get
			{
				if (state.X > 250 || state.RotationX > 250)
					return true;
				foreach (int p in pov)
					if (p.In(4500, 9000, 13500))
						return true;
				return false;
			}
		}

		/// <summary>
		/// Note that this does not appear to work at this time. I probably need to have more infos.
		/// </summary>
		public void SetVibration(int left, int right)
		{
			int[] temp1, temp2;
			// my first clue that it doesnt work is that LEFT  and RIGHT _ARENT USED_
			// I should just look for C++ examples instead of trying to look for SlimDX examples

			var parameters = new EffectParameters();
			parameters.Duration = 0x2710;
			parameters.Gain = 0x2710;
			parameters.SamplePeriod = 0;
			parameters.TriggerButton = 0;
			parameters.TriggerRepeatInterval = 0x2710;
			parameters.Flags = EffectFlags.None;
			parameters.GetAxes(out temp1, out temp2);
			parameters.SetAxes(temp1, temp2);
			var effect = new Effect(joystick, EffectGuid.ConstantForce);
			effect.SetParameters(parameters);
			effect.Start(1);
		}
	}
}