using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private IController _controller = NullController.Instance;

		private static readonly ControllerDefinition _3DSController = new ControllerDefinition("3DS Controller")
			{
				BoolButtons =
				{
					"A", "B", "X", "Y", "Up", "Down", "Left", "Right", "L", "R", "Start", "Select", "Debug", "GPIO14", "ZL", "ZR", "Touch", "Tilt", "Reset"
				}
			}.AddXYPair("Circle Pad {0}", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("C-Stick {0}", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(319), 160, 0.RangeTo(239), 120)
			.AddXYPair("Tilt {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(319), 160, 0.RangeTo(239), 120)
			.MakeImmutable();

		private bool GetButtonCallback(LibPPSSPP.Buttons button) => button switch
		{
			LibPPSSPP.Buttons.A => _controller.IsPressed("A"),
			LibPPSSPP.Buttons.B => _controller.IsPressed("B"),
			LibPPSSPP.Buttons.X => _controller.IsPressed("X"),
			LibPPSSPP.Buttons.Y => _controller.IsPressed("Y"),
			LibPPSSPP.Buttons.Up => _controller.IsPressed("Up"),
			LibPPSSPP.Buttons.Down => _controller.IsPressed("Down"),
			LibPPSSPP.Buttons.Left => _controller.IsPressed("Left"),
			LibPPSSPP.Buttons.Right => _controller.IsPressed("Right"),
			LibPPSSPP.Buttons.L => _controller.IsPressed("L"),
			LibPPSSPP.Buttons.R => _controller.IsPressed("R"),
			LibPPSSPP.Buttons.Start => _controller.IsPressed("Start"),
			LibPPSSPP.Buttons.Select => _controller.IsPressed("Select"),
			LibPPSSPP.Buttons.Debug => _controller.IsPressed("Debug"),
			LibPPSSPP.Buttons.Gpio14 => _controller.IsPressed("GPIO14"),
			LibPPSSPP.Buttons.ZL => _controller.IsPressed("ZL"),
			LibPPSSPP.Buttons.ZR => _controller.IsPressed("ZR"),
			LibPPSSPP.Buttons.Home => false, // not supported (can only be used if Home menu is booted, which is never be the case for us)
			LibPPSSPP.Buttons.Power => false, // not supported (can only be used if Home menu is booted, which is never be the case for us)
			_ => throw new InvalidOperationException(),
		};

		private void GetAxisCallback(LibPPSSPP.AnalogSticks stick, out float x, out float y)
		{
			switch (stick)
			{
				case LibPPSSPP.AnalogSticks.CirclePad:
					x = _controller.AxisValue("Circle Pad X") / 128.0f;
					y = _controller.AxisValue("Circle Pad Y") / 128.0f;
					break;
				case LibPPSSPP.AnalogSticks.CStick:
					x = _controller.AxisValue("C-Stick X") / 128.0f;
					y = _controller.AxisValue("C-Stick Y") / 128.0f;
					break;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
