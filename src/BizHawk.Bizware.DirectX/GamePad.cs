using System;
using System.Collections.Generic;

using BizHawk.Common;

using Vortice.DirectInput;

namespace BizHawk.Bizware.DirectX
{
	internal sealed class GamePad
	{
		private static readonly object SyncObj = new();
		private static readonly List<GamePad> Devices = new();
		private static IDirectInput8 _directInput;

		public static void Initialize(IntPtr mainFormHandle)
		{
			lock (SyncObj)
			{
				Cleanup();

				_directInput = DInput.DirectInput8Create();

				foreach (var device in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
				{
					Console.WriteLine("joy device: {0} `{1}`", device.InstanceGuid, device.ProductName);

					if (device.ProductName.Contains("XBOX 360") || device.ProductName.Contains("Xbox One") || device.ProductName.Contains("XINPUT"))
						continue; // Don't input XBOX 360 controllers into here; we'll process them via XInput (there are limitations in some trigger axes when xbox pads go over xinput)

					var joystick = _directInput.CreateDevice(device.InstanceGuid);
					joystick.SetCooperativeLevel(mainFormHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
					joystick.SetDataFormat<RawJoystickState>();
					foreach (var deviceObject in joystick.GetObjects())
					{
						if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
						{
							joystick.GetObjectPropertiesByName(deviceObject.Name).Range = new(-1000, 1000);
						}
					}
					joystick.Acquire();

					var p = new GamePad(joystick, Devices.Count);
					Devices.Add(p);
				}
			}
		}

		public static IEnumerable<GamePad> EnumerateDevices()
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

		public static void Cleanup()
		{
			lock (SyncObj)
			{
				foreach (var device in Devices)
				{
					device._joystick.Dispose();
				}

				Devices.Clear();

				if (_directInput != null)
				{
					_directInput.Dispose();
					_directInput = null;
				}
			}
		}

		// ********************************** Instance Members **********************************

		private readonly IDirectInputDevice8 _joystick;
		private JoystickState _state = new();

		private GamePad(IDirectInputDevice8 joystick, int index)
		{
			_joystick = joystick;
			PlayerNumber = index + 1;
			InputNamePrefix = $"J{PlayerNumber} ";
			Update();
			InitializeCallbacks();
		}

		public void Update()
		{
			try
			{
				if (_joystick.Acquire().Failure)
					return;
			}
			catch
			{
				return;
			}

			if (_joystick.Poll()
				.Failure)
			{
				return;
			}

			try
			{
				_joystick.GetCurrentJoystickState(ref _state);
			}
			catch (SharpGen.Runtime.SharpGenException)
			{
				// do something?
			}
		}

		public IEnumerable<(string AxisID, float Value)> GetAxes()
		{
			var pis = typeof(JoystickState).GetProperties();
			foreach (var pi in pis)
			{
				yield return (pi.Name, 10.0f * (int)pi.GetValue(_state, null));
			}
		}

		/// <summary>FOR DEBUGGING ONLY</summary>
		public JoystickState GetInternalState()
		{
			return _state;
		}

		public int PlayerNumber { get; }

		public readonly string InputNamePrefix;

		public string ButtonName(int index)
		{
			return _names[index];
		}

		public bool Pressed(int index)
		{
			return _actions[index]();
		}

		public int NumButtons { get; private set; }

		private readonly List<string> _names = new List<string>();
		private readonly List<Func<bool>> _actions = new List<Func<bool>>();

		private void AddItem(string name, Func<bool> callback)
		{
			_names.Add(name);
			_actions.Add(callback);
			NumButtons++;
		}

		private void InitializeCallbacks()
		{
			const int dzp = 400;
			const int dzn = -400;

			_names.Clear();
			_actions.Clear();
			NumButtons = 0;

			AddItem("AccelerationX+", () => _state.AccelerationX >= dzp);
			AddItem("AccelerationX-", () => _state.AccelerationX <= dzn);
			AddItem("AccelerationY+", () => _state.AccelerationY >= dzp);
			AddItem("AccelerationY-", () => _state.AccelerationY <= dzn);
			AddItem("AccelerationZ+", () => _state.AccelerationZ >= dzp);
			AddItem("AccelerationZ-", () => _state.AccelerationZ <= dzn);
			AddItem("AngularAccelerationX+", () => _state.AngularAccelerationX >= dzp);
			AddItem("AngularAccelerationX-", () => _state.AngularAccelerationX <= dzn);
			AddItem("AngularAccelerationY+", () => _state.AngularAccelerationY >= dzp);
			AddItem("AngularAccelerationY-", () => _state.AngularAccelerationY <= dzn);
			AddItem("AngularAccelerationZ+", () => _state.AngularAccelerationZ >= dzp);
			AddItem("AngularAccelerationZ-", () => _state.AngularAccelerationZ <= dzn);
			AddItem("AngularVelocityX+", () => _state.AngularVelocityX >= dzp);
			AddItem("AngularVelocityX-", () => _state.AngularVelocityX <= dzn);
			AddItem("AngularVelocityY+", () => _state.AngularVelocityY >= dzp);
			AddItem("AngularVelocityY-", () => _state.AngularVelocityY <= dzn);
			AddItem("AngularVelocityZ+", () => _state.AngularVelocityZ >= dzp);
			AddItem("AngularVelocityZ-", () => _state.AngularVelocityZ <= dzn);
			AddItem("ForceX+", () => _state.ForceX >= dzp);
			AddItem("ForceX-", () => _state.ForceX <= dzn);
			AddItem("ForceY+", () => _state.ForceY >= dzp);
			AddItem("ForceY-", () => _state.ForceY <= dzn);
			AddItem("ForceZ+", () => _state.ForceZ >= dzp);
			AddItem("ForceZ-", () => _state.ForceZ <= dzn);
			AddItem("RotationX+", () => _state.RotationX >= dzp);
			AddItem("RotationX-", () => _state.RotationX <= dzn);
			AddItem("RotationY+", () => _state.RotationY >= dzp);
			AddItem("RotationY-", () => _state.RotationY <= dzn);
			AddItem("RotationZ+", () => _state.RotationZ >= dzp);
			AddItem("RotationZ-", () => _state.RotationZ <= dzn);
			AddItem("TorqueX+", () => _state.TorqueX >= dzp);
			AddItem("TorqueX-", () => _state.TorqueX <= dzn);
			AddItem("TorqueY+", () => _state.TorqueY >= dzp);
			AddItem("TorqueY-", () => _state.TorqueY <= dzn);
			AddItem("TorqueZ+", () => _state.TorqueZ >= dzp);
			AddItem("TorqueZ-", () => _state.TorqueZ <= dzn);
			AddItem("VelocityX+", () => _state.VelocityX >= dzp);
			AddItem("VelocityX-", () => _state.VelocityX <= dzn);
			AddItem("VelocityY+", () => _state.VelocityY >= dzp);
			AddItem("VelocityY-", () => _state.VelocityY <= dzn);
			AddItem("VelocityZ+", () => _state.VelocityZ >= dzp);
			AddItem("VelocityZ-", () => _state.VelocityZ <= dzn);
			AddItem("X+", () => _state.X >= dzp);
			AddItem("X-", () => _state.X <= dzn);
			AddItem("Y+", () => _state.Y >= dzp);
			AddItem("Y-", () => _state.Y <= dzn);
			AddItem("Z+", () => _state.Z >= dzp);
			AddItem("Z-", () => _state.Z <= dzn);

			// i don't know what the "Slider"s do, so they're omitted for the moment

			for (var i = 0; i < _state.Buttons.Length; i++)
			{
				var j = i;
				AddItem($"B{i + 1}", () => _state.Buttons[j]);
			}

			for (var i = 0; i < _state.PointOfViewControllers.Length; i++)
			{
				var j = i;
				AddItem($"POV{i + 1}U", () => {
					var t = _state.PointOfViewControllers[j];
					return 0.RangeTo(4500).Contains(t) || 31500.RangeToExclusive(36000).Contains(t);
				});
				AddItem($"POV{i + 1}D", () => 13500.RangeTo(22500).Contains(_state.PointOfViewControllers[j]));
				AddItem($"POV{i + 1}L", () => 22500.RangeTo(31500).Contains(_state.PointOfViewControllers[j]));
				AddItem($"POV{i + 1}R", () => 4500.RangeTo(13500).Contains(_state.PointOfViewControllers[j]));
			}
		}

		// Note that this does not appear to work at this time. I probably need to have more infos.
		public void SetVibration(int left, int right)
		{
			// my first clue that it doesn't work is that LEFT  and RIGHT _AREN'T USED_
			// I should just look for C++ examples instead of trying to look for SlimDX examples

			var parameters = new EffectParameters
			{
				Duration = 0x2710,
				Gain = 0x2710,
				SamplePeriod = 0,
				TriggerButton = 0,
				TriggerRepeatInterval = 0x2710,
				Flags = EffectFlags.None
			};
			parameters.GetAxes(out var temp1, out var temp2);
			parameters.SetAxes(temp1, temp2);
			var effect = _joystick.CreateEffect(EffectGuid.ConstantForce, parameters);
			effect.Start(1);
		}
	}
}