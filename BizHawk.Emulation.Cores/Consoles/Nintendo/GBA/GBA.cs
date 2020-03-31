using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public static class GBA
	{
		private static readonly ControllerDefinition.AxisRange TiltRange = new ControllerDefinition.AxisRange(-32767, 0, 32767);

		public static readonly ControllerDefinition GBAController = new ControllerDefinition
		{
			Name = "GBA Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power"
			},
			AxisControls =
			{
				"Tilt X", "Tilt Y", "Tilt Z",
				"Light Sensor"
			},
			AxisRanges =
			{
				TiltRange, TiltRange, TiltRange,
				new ControllerDefinition.AxisRange(0, 100, 200),
			}
		};
	}
}
