using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Encore : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private IController _controller = NullController.Instance;

		private readonly _3DSMotionEmu _motionEmu = new();

		private static readonly ControllerDefinition _3DSController = new ControllerDefinition("3DS Controller")
			{
				BoolButtons =
				{
					"A", "B", "X", "Y", "Up", "Down", "Left", "Right", "L", "R", "Start", "Select", "Debug", "GPIO14", "ZL", "ZR", "Touch", "Tilt", "Reset"
				}
			}.AddXYPair("Circle Pad {0}", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("C-Stick {0}", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(320), 160, 0.RangeTo(240), 120)
			.AddXYPair("Tilt {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(320), 160, 0.RangeTo(240), 120)
			.MakeImmutable();

		private bool GetButtonCallback(LibEncore.Buttons button) => button switch
		{
			LibEncore.Buttons.A => _controller.IsPressed("A"),
			LibEncore.Buttons.B => _controller.IsPressed("B"),
			LibEncore.Buttons.X => _controller.IsPressed("X"),
			LibEncore.Buttons.Y => _controller.IsPressed("Y"),
			LibEncore.Buttons.Up => _controller.IsPressed("Up"),
			LibEncore.Buttons.Down => _controller.IsPressed("Down"),
			LibEncore.Buttons.Left => _controller.IsPressed("Left"),
			LibEncore.Buttons.Right => _controller.IsPressed("Right"),
			LibEncore.Buttons.L => _controller.IsPressed("L"),
			LibEncore.Buttons.R => _controller.IsPressed("R"),
			LibEncore.Buttons.Start => _controller.IsPressed("Start"),
			LibEncore.Buttons.Select => _controller.IsPressed("Select"),
			LibEncore.Buttons.Debug => _controller.IsPressed("Debug"),
			LibEncore.Buttons.Gpio14 => _controller.IsPressed("GPIO14"),
			LibEncore.Buttons.ZL => _controller.IsPressed("ZL"),
			LibEncore.Buttons.ZR => _controller.IsPressed("ZR"),
			LibEncore.Buttons.Home => false, // not supported (can only be used if Home menu is booted, which is never be the case for us)
			LibEncore.Buttons.Power => false, // not supported (can only be used if Home menu is booted, which is never be the case for us)
			_ => throw new InvalidOperationException(),
		};

		private void GetAxisCallback(LibEncore.AnalogSticks stick, out float x, out float y)
		{
			switch (stick)
			{
				case LibEncore.AnalogSticks.CirclePad:
					x = _controller.AxisValue("Circle Pad X") / 128.0f;
					y = _controller.AxisValue("Circle Pad Y") / 128.0f;
					break;
				case LibEncore.AnalogSticks.CStick:
					x = _controller.AxisValue("C-Stick X") / 128.0f;
					y = _controller.AxisValue("C-Stick Y") / 128.0f;
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		private bool GetTouchCallback(out float x, out float y)
		{
			x = _controller.AxisValue("Touch X") / 320.0f;
			y = _controller.AxisValue("Touch Y") / 240.0f;
			return _controller.IsPressed("Touch");
		}

		private void GetMotionCallback(
			out float accelX,
			out float accelY,
			out float accelZ,
			out float gyroX,
			out float gyroY,
			out float gyroZ)
		{
			accelX = _motionEmu.Gravity.X;
			accelY = _motionEmu.Gravity.Y;
			accelZ = _motionEmu.Gravity.Z;
			gyroX = _motionEmu.AngularRate.X;
			gyroY = _motionEmu.AngularRate.Y;
			gyroZ = _motionEmu.AngularRate.Z;
		}
	}
}
