using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan
	{
		public static readonly ControllerDefinition WonderSwanController = new ControllerDefinition
		{
			Name = "WonderSwan Controller",
			BoolButtons =
			{
				"P1 X1",
				"P1 X2",
				"P1 X3",
				"P1 X4",
				"P1 Y1",
				"P1 Y2",
				"P1 Y3",
				"P1 Y4",
				"P1 Start",
				"P1 B",
				"P1 A",

				"P2 X1",
				"P2 X2",
				"P2 X3",
				"P2 X4",
				"P2 Y1",
				"P2 Y2",
				"P2 Y3",
				"P2 Y4",
				"P2 Start",
				"P2 B",
				"P2 A",

				"Power",
				"Rotate"
			},
			CategoryLabels =
			{
				{ "P1 X1", "Normal" },
				{ "P1 X2", "Normal" },
				{ "P1 X3", "Normal" },
				{ "P1 X4", "Normal" },
				{ "P1 Y1", "Normal" },
				{ "P1 Y2", "Normal" },
				{ "P1 Y3", "Normal" },
				{ "P1 Y4", "Normal" },
				{ "P1 Start", "Normal" },
				{ "P1 B", "Normal" },
				{ "P1 A", "Normal" },

				{ "P2 X1", "Rotated" },
				{ "P2 X2", "Rotated" },
				{ "P2 X3", "Rotated" },
				{ "P2 X4", "Rotated" },
				{ "P2 Y1", "Rotated" },
				{ "P2 Y2", "Rotated" },
				{ "P2 Y3", "Rotated" },
				{ "P2 Y4", "Rotated" },
				{ "P2 Start", "Rotated" },
				{ "P2 B", "Rotated" },
				{ "P2 A", "Rotated" },
			}
		};
		public ControllerDefinition ControllerDefinition => WonderSwanController;

		private BizSwan.Buttons GetButtons(IController controller)
		{
			BizSwan.Buttons ret = 0;
			if (controller.IsPressed("P1 X1")) ret |= BizSwan.Buttons.X1;
			if (controller.IsPressed("P1 X2")) ret |= BizSwan.Buttons.X2;
			if (controller.IsPressed("P1 X3")) ret |= BizSwan.Buttons.X3;
			if (controller.IsPressed("P1 X4")) ret |= BizSwan.Buttons.X4;
			if (controller.IsPressed("P1 Y1")) ret |= BizSwan.Buttons.Y1;
			if (controller.IsPressed("P1 Y2")) ret |= BizSwan.Buttons.Y2;
			if (controller.IsPressed("P1 Y3")) ret |= BizSwan.Buttons.Y3;
			if (controller.IsPressed("P1 Y4")) ret |= BizSwan.Buttons.Y4;
			if (controller.IsPressed("P1 Start")) ret |= BizSwan.Buttons.Start;
			if (controller.IsPressed("P1 B")) ret |= BizSwan.Buttons.B;
			if (controller.IsPressed("P1 A")) ret |= BizSwan.Buttons.A;

			if (controller.IsPressed("P2 X1")) ret |= BizSwan.Buttons.R_X1;
			if (controller.IsPressed("P2 X2")) ret |= BizSwan.Buttons.R_X2;
			if (controller.IsPressed("P2 X3")) ret |= BizSwan.Buttons.R_X3;
			if (controller.IsPressed("P2 X4")) ret |= BizSwan.Buttons.R_X4;
			if (controller.IsPressed("P2 Y1")) ret |= BizSwan.Buttons.R_Y1;
			if (controller.IsPressed("P2 Y2")) ret |= BizSwan.Buttons.R_Y2;
			if (controller.IsPressed("P2 Y3")) ret |= BizSwan.Buttons.R_Y3;
			if (controller.IsPressed("P2 Y4")) ret |= BizSwan.Buttons.R_Y4;
			if (controller.IsPressed("P2 Start")) ret |= BizSwan.Buttons.R_Start;
			if (controller.IsPressed("P2 B")) ret |= BizSwan.Buttons.R_B;
			if (controller.IsPressed("P2 A")) ret |= BizSwan.Buttons.R_A;

			if (controller.IsPressed("Rotate")) ret |= BizSwan.Buttons.Rotate;

			return ret;
		}

	}
}
