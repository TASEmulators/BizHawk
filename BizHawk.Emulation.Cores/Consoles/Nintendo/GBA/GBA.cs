using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public static class GBA
	{
		public static readonly ControllerDefinition GBAController = new ControllerDefinition
		{
			Name = "GBA Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power"
			},
			FloatControls =
			{
				"Tilt X", "Tilt Y", "Tilt Z", "Light Sensor"
			},
			FloatRanges =
			{
				new[] { -32767f, 0f, 32767f },
				new[] { -32767f, 0f, 32767f },
				new[] { -32767f, 0f, 32767f },
				new[] { 0f, 100f, 200f },
			}
		};
	}
}
